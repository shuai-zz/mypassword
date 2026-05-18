namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// WebAuthn <c>clientDataJSON</c> payload. Property declaration order is the
    /// JSON-compatible serialization order required by the spec:
    /// <c>type, challenge, origin, crossOrigin</c>.
    /// </summary>
    public sealed class ClientData
    {
        public string type { get; set; }
        public string challenge { get; set; }
        public string origin { get; set; }
        public bool crossOrigin { get; set; }
    }
}
