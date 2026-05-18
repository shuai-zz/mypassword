using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Application logging — Serilog (console + daily-rolling file) behind the
    /// <c>Microsoft.Extensions.Logging</c> abstraction. Configured to mirror the
    /// Java logback setup (<c>desktop/src/main/resources/logback.xml</c>):
    /// <list type="bullet">
    ///   <item>console + file appenders;</item>
    ///   <item>file <c>~/.mypassword/mypassword.log</c>, rolled daily, ~30 days kept;</item>
    ///   <item>pattern <c>yyyy-MM-dd HH:mm:ss [thread] LEVEL logger - message</c>;</item>
    ///   <item>root level INFO.</item>
    /// </list>
    /// </summary>
    public static class Log
    {
        // logback: %d{yyyy-MM-dd HH:mm:ss} [%thread] %-5level %logger{64} - %msg%n
        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss} [{ThreadId}] {LevelText} {SourceContext} - {Message:lj}{NewLine}{Exception}";

        private static readonly ILoggerFactory Factory = CreateFactory();
        private static readonly ConcurrentDictionary<string, ILogger> Loggers = new();

        private static ILoggerFactory CreateFactory()
        {
            var config = new LoggerConfiguration()
                .MinimumLevel.Information()                        // <root level="INFO">
                .Enrich.With(new LogbackEnricher())                // [thread] + logback level text
                .WriteTo.Console(outputTemplate: OutputTemplate);  // ConsoleAppender

            try
            {
                // RollingFileAppender + TimeBasedRollingPolicy. Serilog dates the
                // active file (mypassword<yyyyMMdd>.log); rotation/retention match.
                string logFile = Path.Combine(FileUtils.GetAppDataDir(), "mypassword.log");
                config = config.WriteTo.File(
                    path: logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,                    // logback maxHistory=30
                    outputTemplate: OutputTemplate);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("log file sink unavailable: " + e.Message);
            }

            Logger serilog = config.CreateLogger();
            Serilog.Log.Logger = serilog;
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Serilog.Log.CloseAndFlush();
            return new SerilogLoggerFactory(serilog, dispose: true);
        }

        /// <summary>
        /// Resolve a logger whose category is the caller's source file name —
        /// the closest equivalent of Java's per-class <c>LoggerFactory.getLogger(getClass())</c>.
        /// </summary>
        private static ILogger LoggerFor(string file)
        {
            string category = string.IsNullOrEmpty(file)
                ? "MyPassword"
                : Path.GetFileNameWithoutExtension(file);
            return Loggers.GetOrAdd(category, Factory.CreateLogger);
        }

        public static void Info(string message, [CallerFilePath] string file = null)
            => LoggerFor(file).LogInformation("{LogText}", message);

        public static void Warn(string message, Exception ex = null, [CallerFilePath] string file = null)
            => LoggerFor(file).LogWarning(ex, "{LogText}", message);

        public static void Error(string message, Exception ex = null, [CallerFilePath] string file = null)
            => LoggerFor(file).LogError(ex, "{LogText}", message);
    }

    /// <summary>
    /// Adds the <c>ThreadId</c> and logback-style <c>LevelText</c> properties
    /// referenced by the output template (replaces logback's <c>%thread</c> and
    /// <c>%-5level</c>).
    /// </summary>
    internal sealed class LogbackEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty("ThreadId", Environment.CurrentManagedThreadId));

            // logback level names, left-justified to 5 chars like %-5level
            string level = logEvent.Level switch
            {
                LogEventLevel.Verbose => "TRACE",
                LogEventLevel.Debug => "DEBUG",
                LogEventLevel.Information => "INFO ",
                LogEventLevel.Warning => "WARN ",
                LogEventLevel.Error => "ERROR",
                LogEventLevel.Fatal => "ERROR",
                _ => "INFO ",
            };
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("LevelText", level));
        }
    }
}
