using System.Collections.Generic;
using System.Text;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktopTest
{
    /// <summary>Ported from <c>desktop/.../util/EncryptUtilsTest.java</c>.</summary>
    public class EncryptUtilsTests
    {
        [Test]
        public void TestGenerateSecureRandomBytes()
        {
            const int count = 1000;
            var set = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                byte[] bs = EncryptUtils.GenerateSecureRandomBytes(12);
                set.Add(Base64Utils.B64(bs));
            }
            Assert.That(set, Has.Count.EqualTo(count));
        }

        [Test]
        public void TestPbeKey()
        {
            char[] password = "HelloMyPassword".ToCharArray();
            byte[] salt = ToBytes("32byte-salt-01234567890123456789", 32);
            byte[] key = EncryptUtils.DerivePbeKey(password, salt, 1000);
            Assert.That(key, Has.Length.EqualTo(32));
            Assert.That(Base64Utils.B64(key), Is.EqualTo("tIN93XrCsToiIhqiV6egL57-mB4Eg6aiMsShYFYRmeA"));
        }

        [Test]
        public void TestEncrypt()
        {
            byte[] data = ToBytes("Hello MyPassword!");
            // AES key (raw 32 bytes):
            byte[] key = ToBytes("32byte-skey-01234567890123456789", 32);
            // AES IV (12 bytes):
            byte[] iv = ToBytes("12byte-iv-xx", 12);

            // encrypt:
            byte[] cdata = EncryptUtils.Encrypt(data, key, iv);
            Assert.That(Base64Utils.B64(cdata), Is.EqualTo("2WxvtMZ1RYliSGQ71IklDjmWkqvil25zk7Wp_-tv6M7O"));

            // decrypt ok:
            byte[] decrypted = EncryptUtils.Decrypt(cdata, key, iv);
            Assert.That(decrypted, Is.EqualTo(data));

            // decrypt with a wrong key fails:
            byte[] badKey = ToBytes("32byte-skey-0123456789012345678x", 32);
            Assert.Throws<EncryptException>(() => EncryptUtils.Decrypt(cdata, badKey, iv));
        }

        private static byte[] ToBytes(string s) => ToBytes(s, s.Length);

        private static byte[] ToBytes(string s, int expectedLength)
        {
            byte[] bs = Encoding.UTF8.GetBytes(s);
            Assert.That(bs, Has.Length.EqualTo(expectedLength));
            return bs;
        }
    }
}
