using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    public static class ErrorUtils
    {
        public static BaseResponse Error(ErrorCode code, string message)
            => new() { error = code, errorMessage = message };

        public static string ErrorJson(ErrorCode code, string message)
            => JsonUtils.ToJson(Error(code, message));
    }
}
