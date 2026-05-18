using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Rpc.Request
{
    public sealed class GeneratePasswordRequest : BaseRequest
    {
        public int len { get; set; } = 16;
        public int style { get; set; } = PasswordUtils.StyleAlphabetNumber;
    }

    public sealed class ItemRequest : BaseRequest
    {
        public AbstractItemData item { get; set; }
    }

    public sealed class OAuth : BaseRequest
    {
        public string action { get; set; }
        public string provider { get; set; }
        public string oauthId { get; set; }
    }

    public sealed class TotpAddRequest : BaseRequest
    {
        public long itemId { get; set; }
        public string uri { get; set; } // otpauth://xxx
    }

    public sealed class TotpGetRequest : BaseRequest
    {
        public long itemId { get; set; }
    }

    public sealed class VaultPasswordRequest : BaseRequest
    {
        public string password { get; set; }
    }

    public sealed class PasskeyAddRequest : BaseRequest
    {
        public long itemId { get; set; }
        public string origin { get; set; }
        public PasskeyOptions options { get; set; }

        public sealed class PasskeyOptions
        {
            public string attestation { get; set; }
            public string challenge { get; set; }
            public long timeout { get; set; }
            public PasskeyAuthenticatorSelection authenticatorSelection { get; set; }
            public PasskeyPubKeyCredParam[] pubKeyCredParams { get; set; }
            public PasskeyExcludeCredential[] excludeCredentials { get; set; }
            public PasskeyRp rp { get; set; }
            public PasskeyUser user { get; set; }
        }

        public sealed class PasskeyPubKeyCredParam
        {
            public int alg { get; set; }
            public string type { get; set; }
        }

        public sealed class PasskeyExcludeCredential
        {
            public string id { get; set; }
            public string type { get; set; }
            public string[] transports { get; set; }
        }

        public sealed class PasskeyRp
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public sealed class PasskeyUser
        {
            public string id { get; set; }
            public string displayName { get; set; }
            public string name { get; set; }
        }

        public sealed class PasskeyAuthenticatorSelection
        {
            public string residentKey { get; set; }
            public string userVerification { get; set; }
            public string authenticatorAttachment { get; set; }
        }
    }

    public sealed class PasskeyLoginRequest : BaseRequest
    {
        public long itemId { get; set; }
        public string origin { get; set; }
        public GetOptions options { get; set; }

        public sealed class GetOptions
        {
            public string challenge { get; set; }
            public string rpId { get; set; }
            public long timeout { get; set; }
            public string userVerification { get; set; }
            public AllowCredential[] allowCredentials { get; set; }
        }

        public sealed class AllowCredential
        {
            public string id { get; set; }
            public string type { get; set; }
            public string[] transports { get; set; }
        }
    }
}
