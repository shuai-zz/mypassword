using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core.Web.Pkce
{
    /// <summary>
    /// OAuth 2.0 + PKCE authenticator for vault recovery. Subclasses supply the
    /// provider-specific endpoints.
    /// </summary>
    public abstract class OAuthAuthenticator
    {
        protected readonly string Provider;
        protected readonly OAuthConfig Config;

        private bool _isRecover;
        private string _codeVerifier;

        protected OAuthAuthenticator(string provider)
        {
            Provider = provider;
            RecoveryConfig rc = VaultManager.Current.GetRecoveryConfig(provider);
            Config = (OAuthConfig)JsonUtils.FromJson(rc.oauth_config_json, typeof(OAuthConfig));
            Log.Info($"Load oauth provider {provider}: {rc.oauth_config_json}");
        }

        protected abstract string GetAuthUrl();
        protected abstract string GetTokenUrl();
        protected abstract string GetScope();

        public string GetProvider() => Provider;

        public bool IsRecoverMode() => _isRecover;

        protected string GetRedirectUri()
            => $"http://127.0.0.1:{HttpDaemon.Port}/oauth/{Provider}/callback";

        public string StartOAuth(bool isRecover)
        {
            _isRecover = isRecover;
            _codeVerifier = Base64Utils.B64(EncryptUtils.GenerateKey());
            byte[] digest = HashUtils.Sha256(Encoding.UTF8.GetBytes(_codeVerifier));
            string codeChallenge = Base64Utils.B64(digest);
            var query = new Dictionary<string, string>
            {
                ["client_id"] = Config.client_id ?? "",
                ["response_type"] = "code",
                ["prompt"] = "login",
                ["max_age"] = "0",
                ["scope"] = GetScope(),
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["redirect_uri"] = GetRedirectUri(),
            };
            return HttpUtils.AppendQuery(GetAuthUrl(), query);
        }

        public OAuthUser ExchangeOAuthId(string code)
        {
            var query = new Dictionary<string, string>
            {
                ["client_id"] = Config.client_id ?? "",
                ["client_secret"] = Config.client_secret ?? "",
                ["code"] = code,
                ["code_verifier"] = _codeVerifier,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = GetRedirectUri(),
            };
            _codeVerifier = null;
            string result = HttpUtils.PostForm(GetTokenUrl(), query,
                new Dictionary<string, string> { ["Accept"] = "application/json" });
            var oauth = (OAuthResult)JsonUtils.FromJson(result, typeof(OAuthResult));
            return ProcessOAuthResult(oauth);
        }

        private OAuthUser ProcessOAuthResult(OAuthResult oauth)
        {
            if (oauth.id_token == null)
            {
                return null;
            }
            JwtUser jwtUser;
            try
            {
                jwtUser = DecodeJwtUser(oauth.id_token);
            }
            catch (Exception e)
            {
                Log.Error("Parse JWT failed.", e);
                throw new VaultException(ErrorCode.BAD_REQUEST, "Cannot parse JWT.");
            }
            return new OAuthUser
            {
                provider = Provider,
                oauthId = jwtUser.sub,
                email = jwtUser.email,
                name = jwtUser.name,
            };
        }

        /// <summary>
        /// Decode the id_token claims. When a JWKS URL is configured the RS256
        /// signature is verified; otherwise the payload is read unverified.
        /// </summary>
        private JwtUser DecodeJwtUser(string jwt)
        {
            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Malformed JWT");
            }
            string payloadJson = Encoding.UTF8.GetString(Base64Utils.B64(parts[1]));

            if (!string.IsNullOrEmpty(Config.jwks))
            {
                VerifySignature(jwt, parts);
            }
            else
            {
                Log.Warn("Missing jwks and cannot verify JWT signature!");
            }

            using var doc = JsonDocument.Parse(payloadJson);
            JsonElement root = doc.RootElement;
            return new JwtUser
            {
                iss = GetString(root, "iss"),
                sub = GetString(root, "sub"),
                email = GetString(root, "email"),
                name = GetString(root, "name"),
            };
        }

        private void VerifySignature(string jwt, string[] parts)
        {
            string headerJson = Encoding.UTF8.GetString(Base64Utils.B64(parts[0]));
            string kid;
            using (var headerDoc = JsonDocument.Parse(headerJson))
            {
                kid = GetString(headerDoc.RootElement, "kid");
            }

            string jwksJson = HttpUtils.Get(Config.jwks, null, null);
            byte[] signed = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
            byte[] signature = Base64Utils.B64(parts[2]);

            using var jwksDoc = JsonDocument.Parse(jwksJson);
            foreach (JsonElement key in jwksDoc.RootElement.GetProperty("keys").EnumerateArray())
            {
                string keyKid = GetString(key, "kid");
                if (kid != null && keyKid != null && kid != keyKid)
                {
                    continue;
                }
                var rsaParams = new RSAParameters
                {
                    Modulus = Base64Utils.B64(GetString(key, "n")),
                    Exponent = Base64Utils.B64(GetString(key, "e")),
                };
                using var rsa = RSA.Create(rsaParams);
                if (rsa.VerifyData(signed, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    return;
                }
            }
            throw new VaultException(ErrorCode.BAD_REQUEST, "JWT signature verification failed.");
        }

        private static string GetString(JsonElement obj, string name)
            => obj.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;
    }

    public sealed class GoogleAuthenticator : OAuthAuthenticator
    {
        public GoogleAuthenticator() : base("google") { }

        protected override string GetScope() => "openid email profile";

        protected override string GetAuthUrl() => "https://accounts.google.com/o/oauth2/v2/auth";

        protected override string GetTokenUrl() => "https://oauth2.googleapis.com/token";
    }

    public sealed class MicrosoftAuthenticator : OAuthAuthenticator
    {
        public MicrosoftAuthenticator() : base("microsoft") { }

        protected override string GetScope() => "openid email profile";

        protected override string GetAuthUrl()
            => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        protected override string GetTokenUrl()
            => "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    }
}
