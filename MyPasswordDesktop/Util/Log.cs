using System;
using System.IO;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Minimal logger replacing slf4j/logback. Writes to the console and to
    /// <c>~/.mypassword/mypassword.log</c>.
    /// </summary>
    public static class Log
    {
        private static readonly object Gate = new();
        private static string _logFile;

        private static string LogFile
        {
            get
            {
                if (_logFile == null)
                {
                    try { _logFile = FileUtils.GetLogFile(); }
                    catch { _logFile = null; }
                }
                return _logFile;
            }
        }

        public static void Info(string message) => Write("INFO", message, null);

        public static void Warn(string message, Exception ex = null) => Write("WARN", message, ex);

        public static void Error(string message, Exception ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception ex)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{Environment.CurrentManagedThreadId}] {level,-5} - {message}";
            if (ex != null)
            {
                line += Environment.NewLine + ex;
            }
            lock (Gate)
            {
                Console.WriteLine(line);
                try
                {
                    var file = LogFile;
                    if (file != null)
                    {
                        File.AppendAllText(file, line + Environment.NewLine);
                    }
                }
                catch
                {
                    // never let logging crash the app
                }
            }
        }
    }
}
