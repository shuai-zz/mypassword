using System;

namespace MyPasswordDesktop.Rpc
{
    /// <summary>
    /// A VaultException is converted to a JSON error response by the HTTP daemon.
    /// </summary>
    public class VaultException : Exception
    {
        public readonly ErrorCode ErrorCode;

        public VaultException(ErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
