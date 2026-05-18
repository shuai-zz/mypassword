using System;
using System.Security.Cryptography;
using System.Text;
using MyPasswordDesktop.Core;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// AES-256-GCM + PBKDF2-HMAC-SHA256 helpers. Wire/format compatible with the
    /// Java <c>EncryptUtils</c>: PBKDF2 hashes the UTF-8 bytes of the password,
    /// AES-GCM ciphertext is stored as <c>cipher || 16-byte tag</c>.
    /// </summary>
    public static class EncryptUtils
    {
        public const int AesKeySize = 256;   // bits
        public const int AesTagSize = 16;    // bytes (128 bits)
        public const int AesIvSize  = 12;    // bytes (96 bits)
        public const int PbeKeySize = 32;    // bytes (256 bits)

        /// <summary>Derive a 256-bit PBKDF2-HMAC-SHA256 key.</summary>
        public static byte[] DerivePbeKey(char[] password, byte[] salt, int iterations)
        {
            try
            {
                return Rfc2898DeriveBytes.Pbkdf2(
                    new string(password), salt, iterations, HashAlgorithmName.SHA256, PbeKeySize);
            }
            catch (Exception e)
            {
                throw new EncryptException(e);
            }
        }

        public static byte[] GenerateIV() => GenerateSecureRandomBytes(AesIvSize);

        public static byte[] GenerateSalt() => GenerateSecureRandomBytes(32);

        public static byte[] GenerateKey() => GenerateSecureRandomBytes(32);

        public static byte[] GenerateSecureRandomBytes(int size) => RandomNumberGenerator.GetBytes(size);

        /// <summary>Encrypt with AES-GCM. Output is <c>ciphertext || tag</c>.</summary>
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            try
            {
                var cipher = new byte[data.Length];
                var tag = new byte[AesTagSize];
                using var gcm = new AesGcm(key, AesTagSize);
                gcm.Encrypt(iv, data, cipher, tag);
                var output = new byte[cipher.Length + tag.Length];
                Buffer.BlockCopy(cipher, 0, output, 0, cipher.Length);
                Buffer.BlockCopy(tag, 0, output, cipher.Length, tag.Length);
                return output;
            }
            catch (Exception e)
            {
                throw new EncryptException(e);
            }
        }

        /// <summary>Decrypt an AES-GCM <c>ciphertext || tag</c> blob.</summary>
        public static byte[] Decrypt(byte[] cdata, byte[] key, byte[] iv)
        {
            try
            {
                int cipherLen = cdata.Length - AesTagSize;
                if (cipherLen < 0)
                {
                    throw new EncryptException("Ciphertext too short.");
                }
                var cipher = new byte[cipherLen];
                var tag = new byte[AesTagSize];
                Buffer.BlockCopy(cdata, 0, cipher, 0, cipherLen);
                Buffer.BlockCopy(cdata, cipherLen, tag, 0, AesTagSize);
                var plain = new byte[cipherLen];
                using var gcm = new AesGcm(key, AesTagSize);
                gcm.Decrypt(iv, cipher, tag, plain);
                return plain;
            }
            catch (EncryptException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new EncryptException(e);
            }
        }

        public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);
    }
}
