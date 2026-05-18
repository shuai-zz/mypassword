namespace MyPasswordDesktop.Rpc
{
    /// <summary>Marker base type for request DTOs.</summary>
    public class BaseRequest
    {
    }

    /// <summary>
    /// Base type for response DTOs. <c>error</c>/<c>errorMessage</c> are omitted
    /// from the wire when null (global <c>WhenWritingNull</c> policy).
    /// </summary>
    public class BaseResponse
    {
        public ErrorCode? error { get; set; }
        public string errorMessage { get; set; }
    }
}
