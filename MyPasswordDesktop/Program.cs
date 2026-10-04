using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called.
        [STAThread]
        public static void Main(string[] args)
        {
            // ── crash logging ────────────────────────────────────────────────
            // Capture unhandled exceptions on every thread and write them to the
            // log file before the process dies. Without this, a failure during
            // Avalonia/XAML startup (notably under Native AOT) exits silently.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Log.Error("FATAL: unhandled exception", e.ExceptionObject as Exception);
                Log.Flush();
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log.Error("FATAL: unobserved task exception", e.Exception);
                e.SetObserved();
            };

            // ── bind the daemon port up-front ────────────────────────────────
            // Mirrors the Java MainWindow: claim 127.0.0.1:27432 before showing
            // any UI. If the port is taken, another MyPassword instance is
            // already running — ask it to activate its window, then exit.
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{HttpDaemon.Port}/");
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to bind daemon port — another instance may be running", ex);
                ActivateExistingInstance();
                return;
            }

            HttpDaemon.SharedListener = listener;
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Error("FATAL: application terminated by an unhandled exception", ex);
                Log.Flush();
            }
            finally
            {
                try { listener.Close(); } catch { /* ignore */ }
            }
        }

        private static void ActivateExistingInstance()
        {
            try
            {
                using var client = new HttpClient();
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                client.PostAsync($"http://127.0.0.1:{HttpDaemon.Port}/activate", content)
                      .GetAwaiter().GetResult();
            }
            catch
            {
                // existing instance may not be reachable — ignore
            }
        }

        // Avalonia configuration, don't remove; also used by the visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
            if (OperatingSystem.IsMacOS())
                builder.With(new MacOSPlatformOptions { ShowInDock = true });
            return builder;
        }
    }
}
