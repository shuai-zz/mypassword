namespace MyPasswordDesktop.Core.Data
{
    public sealed class PasskeyData
    {
        public string relyingPartyId { get; set; }   // e.g. "github.com"
        public string relyingPartyName { get; set; } // e.g. "GitHub"

        public string b64UserId { get; set; }   // base64 of the user handle
        public string username { get; set; }    // WebAuthn user.name
        public string displayName { get; set; } // WebAuthn user.displayName

        public int alg { get; set; } // -7 = ES256

        public string b64CredentialId { get; set; } // base64url — lookup key for sign-in
        public string b64PubKey { get; set; }       // base64url raw uncompressed P-256 point
        public string b64PrivKey { get; set; }      // base64url PKCS#8-encoded EC private key

        public long createdAt { get; set; }
    }

    public sealed class TotpData
    {
        public string secret { get; set; }    // base32-encoded
        public string issuer { get; set; }
        public string username { get; set; }
        public string algorithm { get; set; } // SHA1, SHA256, SHA512
        public int digits { get; set; }        // 6 or 8
        public int period { get; set; }        // seconds, usually 30
    }
}
