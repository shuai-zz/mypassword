using System.Collections.Generic;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;

namespace MyPasswordDesktop.Rpc.Response
{
    public sealed class InfoResponse : BaseResponse
    {
        public sealed class InfoData
        {
            public string database { get; set; }
            public bool initialized { get; set; }
            public bool locked { get; set; }
            public int appVersion { get; set; }
            public int dataVersion { get; set; }
            public Extension caller { get; set; }
        }

        public InfoData data { get; set; }
    }

    public sealed class ItemResponse : BaseResponse
    {
        public AbstractItemData item { get; set; }
    }

    public sealed class ItemsResponse : BaseResponse
    {
        public List<AbstractItemData> items { get; set; }
    }

    public sealed class PasskeyAddResponse
    {
        public string id { get; set; }                      // base64url credentialId
        public string rawId { get; set; }
        public string type { get; set; }                    // "public-key"
        public string authenticatorAttachment { get; set; } // "platform"
        public PasskeyResponse response { get; set; }
        public Dictionary<string, string> clientExtensionResults { get; set; } = new();

        public sealed class PasskeyResponse
        {
            public string clientDataJSON { get; set; }
            public string authenticatorData { get; set; }
            public string publicKey { get; set; }
            public int publicKeyAlgorithm { get; set; }
            public string attestationObject { get; set; }
            public string[] transports { get; set; }
        }
    }

    /// <summary>
    /// WebAuthn AuthenticationResponseJSON returned to the extension.
    /// </summary>
    public sealed class PasskeyLoginResponse
    {
        public string id { get; set; }
        public string rawId { get; set; }
        public string type { get; set; }
        public string authenticatorAttachment { get; set; }
        public AssertionResponse response { get; set; }
        public Dictionary<string, string> clientExtensionResults { get; set; } = new();

        public sealed class AssertionResponse
        {
            public string clientDataJSON { get; set; }
            public string authenticatorData { get; set; }
            public string signature { get; set; }
            public string userHandle { get; set; }
        }
    }
}
