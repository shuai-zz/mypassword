using System;
using System.Diagnostics;

namespace MyPasswordDesktop.Util
{
    /// <summary>Small OS-shell helpers (open URLs in the default browser).</summary>
    public static class ShellUtils
    {
        public static void OpenUrl(string url)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", url);
                }
                else
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception e)
            {
                Log.Warn("failed to open url: " + url, e);
            }
        }
    }
}
