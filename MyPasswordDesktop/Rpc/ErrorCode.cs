namespace MyPasswordDesktop.Rpc
{
    /// <summary>
    /// Error codes serialized on the wire as their enum name (e.g.
    /// <c>"VAULT_LOCKED"</c>) — matches Jackson's default enum handling.
    /// </summary>
    public enum ErrorCode
    {
        VAULT_LOCKED,
        UNKNOWN_EXTENSION,
        NO_PASSWORD,
        DATA_NOT_FOUND,
        BAD_FIELD,
        BAD_PASSWORD,
        BAD_REQUEST,
    }
}
