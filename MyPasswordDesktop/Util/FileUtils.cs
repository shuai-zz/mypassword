using System;
using System.IO;
using System.Text;

namespace MyPasswordDesktop.Util
{
    public static class FileUtils
    {
        /// <summary>Name of the pointer file inside <see cref="GetAppDataDir"/>.</summary>
        private const string VaultPointer = "vault.path";

        /// <summary>UTF-8 without a byte-order mark — the Java app reads the
        /// pointer file with <c>Files.readString</c>, which does not strip a BOM.</summary>
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static string GetUserHome()
            => Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        /// <summary>
        /// Return <c>~/.mypassword</c>, creating it if missing. Throws if the path
        /// exists but is not a directory.
        /// </summary>
        public static string GetAppDataDir()
        {
            string dir = Path.Combine(GetUserHome(), ".mypassword");
            if (File.Exists(dir))
            {
                throw new InvalidOperationException(
                    dir + " exists but is not a directory. Remove it and restart MyPassword.");
            }
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        /// <summary>
        /// Resolve the vault database path from the pointer file
        /// <c>~/.mypassword/vault.path</c>. Returns <c>null</c> when no pointer
        /// file exists.
        /// </summary>
        public static string GetDbFile()
        {
            string pointer = Path.Combine(GetAppDataDir(), VaultPointer);
            if (File.Exists(pointer))
            {
                string content = File.ReadAllText(pointer, Encoding.UTF8).Trim();
                if (content.Length > 0)
                {
                    return Path.GetFullPath(content);
                }
            }
            return null;
        }

        /// <summary>
        /// Return <c>~/.mypassword/logs</c>, creating it if missing.
        /// </summary>
        public static string GetLogDir()
        {
            string dir = Path.Combine(GetAppDataDir(), "logs");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static string GetLogFile() => Path.Combine(GetLogDir(), "mypassword.log");

        /// <summary>
        /// Display-only path: replaces the user-home prefix with <c>~</c>
        /// (e.g. <c>C:\Users\me\.mypassword</c> -> <c>~\.mypassword</c>).
        /// </summary>
        public static string CollapseHome(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            string home = GetUserHome();
            if (path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
            {
                return "~" + path.Substring(home.Length);
            }
            return path;
        }

        /// <summary>True when the path resolves to an existing regular file.</summary>
        public static bool IsValidVaultFile(string p) => p != null && File.Exists(p);

        /// <summary>
        /// Write the pointer file so future <see cref="GetDbFile"/> calls resolve
        /// to <paramref name="target"/>. Passing <c>null</c> removes the pointer.
        /// </summary>
        public static void SetVaultLocation(string target)
        {
            string pointer = Path.Combine(GetAppDataDir(), VaultPointer);
            if (target == null)
            {
                if (File.Exists(pointer)) File.Delete(pointer);
                return;
            }
            File.WriteAllText(pointer, Path.GetFullPath(target), Utf8NoBom);
        }
    }
}
