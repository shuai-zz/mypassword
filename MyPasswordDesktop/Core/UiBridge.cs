using System;
using MyPasswordDesktop.Core.Entities;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// Decouples the core/daemon layer from the Avalonia UI. The UI assigns
    /// these delegates at startup; the daemon invokes them (they marshal to the
    /// UI thread internally).
    /// </summary>
    public static class UiBridge
    {
        /// <summary>Bring the main window to the foreground.</summary>
        public static Action ActivateApp;

        /// <summary>Copy a value to the clipboard and schedule it to be cleared.</summary>
        public static Action<string> CopyPassword;

        /// <summary>Show the extension pairing approve/reject prompt.</summary>
        public static Action<ExtensionConfig> ShowPairRequest;
    }
}
