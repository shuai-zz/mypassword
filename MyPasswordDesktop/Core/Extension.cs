using System;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// The Chrome extension bound to the current HTTP request, validated from
    /// the <c>X-Extension-*</c> signed headers. Stored per-request via
    /// <see cref="AsyncLocal{T}"/>.
    /// </summary>
    public sealed class Extension
    {
        private static readonly AsyncLocal<Extension> CurrentExtension = new();

        public long id { get; set; }
        public string name { get; set; }
        public string device { get; set; }

        public Extension() { }

        private Extension(long id, string name, string device)
        {
            this.id = id;
            this.name = name;
            this.device = device;
        }

        /// <summary>Current extension for this request, or <c>null</c> if not bound.</summary>
        [JsonIgnore]
        public static Extension Current => CurrentExtension.Value;

        public static void Remove() => CurrentExtension.Value = null;

        /// <summary>
        /// Validate the signed extension headers and, on success, bind the
        /// extension to the current request. Returns <c>false</c> when the
        /// request is not from a known/approved extension.
        /// </summary>
        public static bool TrySetExtension(NameValueCollection headers)
        {
            try
            {
                string sid = headers["X-Extension-Id"];
                string sts = headers["X-Extension-Timestamp"];
                string sig = headers["X-Extension-Signature"];
                if (sid == null || sts == null || sig == null)
                {
                    return false;
                }
                long id = long.Parse(sid);
                long ts = long.Parse(sts);
                if (Math.Abs(ts - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) > 30_000L)
                {
                    Log.Warn("Extension: invalid timestamp: " + ts);
                    return false;
                }
                ExtensionConfig ec = VaultManager.Current.GetExtension(id);
                if (ec == null || !ec.approve)
                {
                    Log.Warn("Extension: not paired.");
                    return false;
                }
                byte[] hash = HashUtils.HmacSha256(sts, Encoding.UTF8.GetBytes(ec.seed));
                string hexHash = Convert.ToHexStringLower(hash);
                if (!sig.Equals(hexHash))
                {
                    Log.Warn("Extension: invalid signature.");
                    return false;
                }
                Log.Info($"Extension: validated ok: {ec.name} @ {ec.device}");
                CurrentExtension.Value = new Extension(ec.id, ec.name, ec.device);
                return true;
            }
            catch (Exception e)
            {
                Log.Warn("Extension: validate failed: " + e.Message);
            }
            return false;
        }
    }
}
