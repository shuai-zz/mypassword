using System;
using System.IO;
using System.Text;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Minimal CBOR (RFC 8949) encoder sufficient for building WebAuthn
    /// attestationObject and COSE_Key structures. Direct port of the Java
    /// <c>CborWriter</c>: major types 0/1 (int), 2 (bytes), 3 (text), 5 (map).
    /// </summary>
    public sealed class CborWriter
    {
        private readonly MemoryStream _out = new();

        public byte[] ToByteArray() => _out.ToArray();

        private void WriteTypeAndLength(int majorType, long value)
        {
            int mt = (majorType & 0x07) << 5;
            if (value < 0)
            {
                throw new ArgumentException("CBOR length/value must be non-negative: " + value);
            }
            if (value < 24)
            {
                _out.WriteByte((byte)(mt | (int)value));
            }
            else if (value < 0x100L)
            {
                _out.WriteByte((byte)(mt | 24));
                _out.WriteByte((byte)value);
            }
            else if (value < 0x10000L)
            {
                _out.WriteByte((byte)(mt | 25));
                _out.WriteByte((byte)((value >> 8) & 0xff));
                _out.WriteByte((byte)(value & 0xff));
            }
            else if (value < 0x100000000L)
            {
                _out.WriteByte((byte)(mt | 26));
                _out.WriteByte((byte)((value >> 24) & 0xff));
                _out.WriteByte((byte)((value >> 16) & 0xff));
                _out.WriteByte((byte)((value >> 8) & 0xff));
                _out.WriteByte((byte)(value & 0xff));
            }
            else
            {
                _out.WriteByte((byte)(mt | 27));
                _out.WriteByte((byte)((value >> 56) & 0xff));
                _out.WriteByte((byte)((value >> 48) & 0xff));
                _out.WriteByte((byte)((value >> 40) & 0xff));
                _out.WriteByte((byte)((value >> 32) & 0xff));
                _out.WriteByte((byte)((value >> 24) & 0xff));
                _out.WriteByte((byte)((value >> 16) & 0xff));
                _out.WriteByte((byte)((value >> 8) & 0xff));
                _out.WriteByte((byte)(value & 0xff));
            }
        }

        public CborWriter WriteInt(long value)
        {
            if (value >= 0)
            {
                WriteTypeAndLength(0, value);
            }
            else
            {
                WriteTypeAndLength(1, -1 - value);
            }
            return this;
        }

        public CborWriter WriteBytes(byte[] value)
        {
            WriteTypeAndLength(2, value.Length);
            _out.Write(value, 0, value.Length);
            return this;
        }

        public CborWriter WriteText(string value)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(value);
            WriteTypeAndLength(3, utf8.Length);
            _out.Write(utf8, 0, utf8.Length);
            return this;
        }

        public CborWriter WriteMapHeader(int size)
        {
            WriteTypeAndLength(5, size);
            return this;
        }
    }
}
