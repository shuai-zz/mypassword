using System;
using System.Buffers.Text;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// URL-safe base64 (RFC 4648 §5) without padding — matches Java's
    /// <c>Base64.getUrlEncoder().withoutPadding()</c> / <c>getUrlDecoder()</c>.
    /// </summary>
    public static class Base64Utils
    {
        /// <summary>URL-safe base64 encode (no padding).</summary>
        public static string B64(byte[] data) => Base64Url.EncodeToString(data);

        /// <summary>URL-safe base64 decode (padding optional).</summary>
        public static byte[] B64(string b64str) => Base64Url.DecodeFromChars(b64str.AsSpan());
    }
}
