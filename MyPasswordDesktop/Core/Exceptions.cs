using System;

namespace MyPasswordDesktop.Core
{
    /// <summary>Thrown on any cryptographic failure.</summary>
    public class EncryptException : Exception
    {
        public EncryptException() { }
        public EncryptException(string message) : base(message) { }
        public EncryptException(Exception cause) : base(cause?.Message, cause) { }
        public EncryptException(string message, Exception cause) : base(message, cause) { }
    }

    /// <summary>Thrown on any database access failure.</summary>
    public class DataAccessException : Exception
    {
        public DataAccessException() { }
        public DataAccessException(string message) : base(message) { }
        public DataAccessException(Exception cause) : base(cause?.Message, cause) { }
        public DataAccessException(string message, Exception cause) : base(message, cause) { }
    }
}
