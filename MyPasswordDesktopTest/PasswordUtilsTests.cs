using System;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktopTest
{
    /// <summary>Ported from <c>desktop/.../util/PasswordUtilsTest.java</c>.</summary>
    public class PasswordUtilsTests
    {
        // same symbol set as PasswordUtils.SYMBOL in the Java source
        private const string Symbol = "?!@#$%&*()-_+=[]{}<>:;,.";

        private static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static bool IsNumber(char c) => c >= '0' && c <= '9';

        [Test]
        public void TestGeneratePasswordAlphabetNumber()
        {
            for (int n = 0; n < 10; n++)
            {
                string pwd = PasswordUtils.GeneratePassword(20, PasswordUtils.StyleAlphabetNumber);
                Assert.That(pwd, Has.Length.EqualTo(20));
                bool hasAlpha = false, hasNumber = false;
                foreach (char c in pwd)
                {
                    bool isAlpha = IsAlpha(c);
                    bool isNumber = IsNumber(c);
                    Assert.That(isAlpha || isNumber, Is.True, $"unexpected char '{c}'");
                    hasAlpha |= isAlpha;
                    hasNumber |= isNumber;
                }
                Assert.That(hasAlpha, Is.True);
                Assert.That(hasNumber, Is.True);
            }
        }

        [Test]
        public void TestGeneratePasswordAlphabet()
        {
            for (int n = 0; n < 10; n++)
            {
                string pwd = PasswordUtils.GeneratePassword(10, PasswordUtils.StyleAlphabet);
                Assert.That(pwd, Has.Length.EqualTo(10));
                foreach (char c in pwd)
                {
                    Assert.That(IsAlpha(c), Is.True, $"unexpected char '{c}'");
                }
            }
        }

        [Test]
        public void TestGeneratePasswordNumber()
        {
            for (int n = 0; n < 10; n++)
            {
                string pwd = PasswordUtils.GeneratePassword(15, PasswordUtils.StyleNumber);
                Assert.That(pwd, Has.Length.EqualTo(15));
                foreach (char c in pwd)
                {
                    Assert.That(IsNumber(c), Is.True, $"unexpected char '{c}'");
                }
            }
        }

        [Test]
        public void TestGeneratePasswordAlphabetNumberSymbol()
        {
            for (int n = 0; n < 10; n++)
            {
                string pwd = PasswordUtils.GeneratePassword(20, PasswordUtils.StyleAlphabetNumberSymbol);
                Assert.That(pwd, Has.Length.EqualTo(20));
                bool hasAlpha = false, hasNumber = false, hasSymbol = false;
                foreach (char c in pwd)
                {
                    bool isAlpha = IsAlpha(c);
                    bool isNumber = IsNumber(c);
                    bool isSymbol = Symbol.IndexOf(c) >= 0;
                    Assert.That(isAlpha || isNumber || isSymbol, Is.True, $"unexpected char '{c}'");
                    hasAlpha |= isAlpha;
                    hasNumber |= isNumber;
                    hasSymbol |= isSymbol;
                }
                Assert.That(hasAlpha, Is.True);
                Assert.That(hasNumber, Is.True);
                Assert.That(hasSymbol, Is.True);
            }
        }

        [Test]
        public void TestGeneratePasswordShortLength()
        {
            for (int n = 0; n < 10; n++)
            {
                string pwd = PasswordUtils.GeneratePassword(4, PasswordUtils.StyleAlphabetNumberSymbol);
                Assert.That(pwd, Has.Length.EqualTo(4));
            }
        }

        [Test]
        public void TestGeneratePasswordInvalidArgsThrow()
        {
            Assert.Throws<ArgumentException>(() => PasswordUtils.GeneratePassword(3, PasswordUtils.StyleAlphabet));
            Assert.Throws<ArgumentException>(() => PasswordUtils.GeneratePassword(16, 99));
        }
    }
}
