using System;
using System.Security.Cryptography;

namespace MyPasswordDesktop.Util
{
    public static class PasswordUtils
    {
        public const int StyleAlphabetNumber       = 0;
        public const int StyleAlphabet             = 1;
        public const int StyleNumber               = 2;
        public const int StyleAlphabetNumberSymbol  = 3;

        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Number   = "0123456789";
        private const string Symbol   = "?!@#$%&*()-_+=[]{}<>:;,.";
        private static readonly string AlphabetNumber       = Alphabet + Number;
        private static readonly string AlphabetNumberSymbol =
            string.Concat(Alphabet + Number, Alphabet + Number, Alphabet + Number, Alphabet + Number) + Symbol;

        public static string GeneratePassword(int len, int style)
        {
            if (len < 4 || len > 100)
            {
                throw new ArgumentException("Invalid length.");
            }
            if (style < StyleAlphabetNumber || style > StyleAlphabetNumberSymbol)
            {
                throw new ArgumentException("Invalid style.");
            }
            return style switch
            {
                StyleAlphabet => Generate(Alphabet, len),
                StyleNumber => Generate(Number, len),
                StyleAlphabetNumber => Generate(AlphabetNumber, len, true, true, false),
                StyleAlphabetNumberSymbol => Generate(AlphabetNumberSymbol, len, true, true, true),
                _ => throw new ArgumentException("Invalid style."),
            };
        }

        private static string Generate(string pool, int len)
        {
            var buffer = new char[len];
            Generate(buffer, pool, false, false, false);
            return new string(buffer);
        }

        private static string Generate(string pool, int len, bool containsAlphabet, bool containsNumber,
            bool containsSymbol)
        {
            var buffer = new char[len];
            while (!Generate(buffer, pool, containsAlphabet, containsNumber, containsSymbol))
            {
                // retry until all required character classes are present
            }
            return new string(buffer);
        }

        private static bool Generate(char[] buffer, string pool, bool containsAlphabet, bool containsNumber,
            bool containsSymbol)
        {
            bool hasAlphabet = false, hasNumber = false, hasSymbol = false;
            int range = pool.Length;
            for (int i = 0; i < buffer.Length; i++)
            {
                char ch = pool[RandomNumberGenerator.GetInt32(range)];
                buffer[i] = ch;
                if (Alphabet.IndexOf(ch) >= 0) hasAlphabet = true;
                if (Number.IndexOf(ch) >= 0) hasNumber = true;
                if (Symbol.IndexOf(ch) >= 0) hasSymbol = true;
            }
            if (containsAlphabet && !hasAlphabet) return false;
            if (containsNumber && !hasNumber) return false;
            if (containsSymbol && !hasSymbol) return false;
            return true;
        }
    }
}
