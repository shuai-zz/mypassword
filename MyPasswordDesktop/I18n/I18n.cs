using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.I18n
{
    /// <summary>
    /// Localization facade. Replaces the Java <c>I18nUtils</c>/ResourceBundle
    /// mechanism with Avalonia <c>.axaml</c> resource dictionaries
    /// (<c>Resources/Strings.{lang}.axaml</c>) merged into the application
    /// resources, so XAML can bind via <c>{DynamicResource key}</c>.
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
            var uri = new Uri($"avares://MyPassword/Resources/Strings.{lang}.axaml");
            var dict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);

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
