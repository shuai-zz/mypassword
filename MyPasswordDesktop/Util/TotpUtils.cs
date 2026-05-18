using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using MyPasswordDesktop.Core.Data;

namespace MyPasswordDesktop.Util
{
    public static class TotpUtils
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>Parse an <c>otpauth://totp/...</c> URI into a <see cref="TotpData"/>.</summary>
        public static TotpData ParseUri(string uri)
        {
            var parsed = new Uri(uri);
            if (!string.Equals(parsed.Scheme, "otpauth", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Not an otpauth URI: " + uri);
            }
            Dictionary<string, string> query = ParseQuery(parsed.Query);

            if (!query.TryGetValue("secret", out string secret) || string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException("Missing secret in otpauth URI");
            }

            // Label: otpauth://totp/Issuer:username  or  otpauth://totp/username
            string label = parsed.AbsolutePath;
            label = label.StartsWith('/') ? label.Substring(1) : label;
            label = Uri.UnescapeDataString(label);
            string labelIssuer = "";
            string labelUser = label;
            int colon = label.IndexOf(':');
            if (colon >= 0)
            {
                labelIssuer = label.Substring(0, colon).Trim();
                labelUser = label.Substring(colon + 1).Trim();
            }

            var data = new TotpData
            {
                secret = secret.ToUpperInvariant().Replace(" ", ""),
                issuer = query.GetValueOrDefault("issuer", labelIssuer),
                username = labelUser,
                algorithm = query.GetValueOrDefault("algorithm", "SHA1").ToUpperInvariant(),
            };
            data.digits = int.TryParse(query.GetValueOrDefault("digits", "6"), out int d) ? d : 6;
            data.period = int.TryParse(query.GetValueOrDefault("period", "30"), out int p) ? p : 30;
            return data;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query))
            {
                return map;
            }
            if (query.StartsWith('?'))
            {
                query = query.Substring(1);
            }
            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0)
                {
                    string key = pair.Substring(0, eq).ToLowerInvariant();
                    string value = Uri.UnescapeDataString(pair.Substring(eq + 1));
                    map[key] = value;
                }
            }
            return map;
        }

        /// <summary>Generate the current TOTP code for the given configuration.</summary>
        public static string GetTotp(TotpData totp)
        {
            long timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / totp.period;
            byte[] key = Base32Decode(totp.secret);
            var counter = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counter, timeStep);

            byte[] hash = totp.algorithm switch
            {
                "SHA256" => HMACSHA256.HashData(key, counter),
                "SHA512" => HMACSHA512.HashData(key, counter),
                _ => HMACSHA1.HashData(key, counter),
            };

            int offset = hash[^1] & 0x0F;
            int code = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);
            int otp = code % (int)Math.Pow(10, totp.digits);
            return otp.ToString().PadLeft(totp.digits, '0');
        }

        private static byte[] Base32Decode(string input)
        {
            string s = input.ToUpperInvariant().Replace("=", "").Replace(" ", "");
            int outLen = s.Length * 5 / 8;
            var output = new byte[outLen];
            int buffer = 0, bitsLeft = 0, idx = 0;
            foreach (char c in s)
            {
                int val = Base32Chars.IndexOf(c);
                if (val < 0)
                {
                    throw new ArgumentException("Invalid base32 character: " + c);
                }
                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    output[idx++] = (byte)(buffer >> bitsLeft);
                }
            }
            return output;
        }
    }
}
