using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MyPasswordDesktop.Rpc;

namespace MyPasswordDesktop.Util
{
    public static class StringUtils
    {
        public static string Normalize(string str) => str == null ? "" : str.Trim();

        public static string CheckNotEmpty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Invalid " + name);
            }
            return value.Trim();
        }

        public static string CheckPattern(string name, string pattern, string value)
        {
            if (value == null || !Regex.IsMatch(value, pattern))
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Invalid " + name);
            }
            return value;
        }

        private static CultureInfo _dateCulture = CultureInfo.CurrentCulture;

        /// <summary>Set the locale used by <see cref="FormatDateTime"/>.</summary>
        public static void InitDateTimeLocale(string locale)
        {
            if (!string.IsNullOrEmpty(locale))
            {
                try { _dateCulture = CultureInfo.GetCultureInfo(locale); }
                catch (CultureNotFoundException) { /* keep default */ }
            }
        }

        /// <summary>Format an epoch-millis timestamp using the configured locale.</summary>
        public static string FormatDateTime(long ts)
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime;
            return dt.ToString("d MMM yyyy, h:mm tt", _dateCulture);
        }
    }
}
