namespace MyPasswordDesktop.Core.Web.Pkce
{
    /// <summary>Parsed <c>oauth_config_json</c> from the RecoveryConfig row.</summary>
    public sealed class OAuthConfig
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string issuer { get; set; }
        public string jwks { get; set; }
    }

    /// <summary>Token endpoint response.</summary>
    public sealed class OAuthResult
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
        public string scope { get; set; }
        public string id_token { get; set; }
    }

    /// <summary>Claims extracted from a verified id_token.</summary>
    public sealed class JwtUser
    {
        public string iss { get; set; }
        public string sub { get; set; }
        public string email { get; set; }
        public string name { get; set; }
    }

    public sealed class OAuthUser
    {
        public string provider { get; set; }
        public string oauthId { get; set; }
        public string name { get; set; }
        public string email { get; set; }

        public override string ToString()
            => $"OAuthUser [provider={provider}, oauthId={oauthId}, name={name}, email={email}]";
    }
}
