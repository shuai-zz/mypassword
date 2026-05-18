using System.Security.Cryptography;
using System.Text;

namespace MyPasswordDesktop.Util
{
    public static class HashUtils
    {
        public static byte[] Sha256(string s) => Sha256(Encoding.UTF8.GetBytes(s));

        public static byte[] Sha256(byte[] bs) => SHA256.HashData(bs);

        public static byte[] HmacSha256(string s, byte[] key)
            => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(s));
    }
}
