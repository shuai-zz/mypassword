using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.I18n
{
    /// <summary>
    /// Localization facade. Replaces the Java <c>I18nUtils</c>/ResourceBundle
    /// mechanism with Avalonia <c>.axaml</c> resource dictionaries
    /// (<c>Resources/Strings.{lang}.axaml</c>) merged into the application
    /// resources, so XAML can bind via <c>{DynamicResource key}</c>.
    /// <para>
    /// The dictionaries carry <c>x:Class</c> and are instantiated directly
    /// (<c>new StringsEn()</c>), which runs their <em>compiled</em> XAML — this
    /// is trim- and Native-AOT-safe, unlike loading them by URI at runtime.
    /// </para>
    /// </summary>
    public static class I18n
    {
        private static ResourceDictionary _current;

        /// <summary>
        /// Load the language dictionary. <paramref name="language"/> is the
        /// <c>SettingKey.LANGUAGE</c> value — "" for system default, else "en"/"zh".
        /// </summary>
        public static void Init(string language)
        {
            string lang = ResolveLang(language);
            Log.Info("init locale: " + lang);

            ResourceDictionary dict = lang switch
            {
                "zh" => new MyPasswordDesktop.Resources.StringsZh(),
                _ => new MyPasswordDesktop.Resources.StringsEn(),
            };

            var appResources = Application.Current?.Resources;
            if (appResources == null)
            {
                return;
            }
            if (_current != null)
            {
                appResources.MergedDictionaries.Remove(_current);
            }
            appResources.MergedDictionaries.Add(dict);
            _current = dict;
        }

        private static string ResolveLang(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
            }
            return language == "zh" ? "zh" : "en";
        }

        /// <summary>Look up a localized string by key.</summary>
        public static string T(string key)
        {
            if (Application.Current != null
                && Application.Current.TryGetResource(key, null, out object value)
                && value is string s)
            {
                return s;
            }
            return key;
        }

        /// <summary>Look up and format a localized string (MessageFormat-style {0}).</summary>
        public static string T(string key, params object[] args)
            => string.Format(T(key), args);
    }
}
