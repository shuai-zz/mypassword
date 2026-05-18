namespace MyPasswordDesktop.Core.Data
{
    /// <summary>Vault setting keys stored in the <c>VaultSetting</c> table.</summary>
    public static class SettingKey
    {
        /// <summary>Auto lock when device is idle, in minutes. 0 = never.</summary>
        public const string AUTO_LOCK = "auto_lock";

        /// <summary>Clear copied password from clipboard, in minutes. 0 = never.</summary>
        public const string CLEAR_CLIPBOARD = "clear_clipboard";

        /// <summary>Keep tray icon rather than exit on window close. 0/1, default 1.</summary>
        public const string KEEP_TRAY_ICON = "keep_tray_icon";

        /// <summary>Delete items in trash after N days. 0 = never.</summary>
        public const string DELETE_AFTER = "delete_after";

        /// <summary>Hot key to activate the app.</summary>
        public const string HOT_KEY_ACTIVATE = "hot_key_activate";

        public const string HOT_KEY_LOCK = "hot_key_lock";

        /// <summary>UI language. "" = system default.</summary>
        public const string LANGUAGE = "language";
    }
}
