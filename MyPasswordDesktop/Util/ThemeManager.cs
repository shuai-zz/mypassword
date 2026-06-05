using Avalonia;
using Avalonia.Styling;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Applies the UI theme variant. The stored value (see
    /// <c>SettingKey.THEME</c>) is "" for "follow the OS", "light", or "dark".
    /// Setting <see cref="Application.RequestedThemeVariant"/> to
    /// <see cref="ThemeVariant.Default"/> tells Avalonia to track the system.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>Map a stored theme setting to a variant and apply it live.</summary>
        public static void Apply(string theme)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }
            app.RequestedThemeVariant = Resolve(theme);
        }

        private static ThemeVariant Resolve(string theme) => theme switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
