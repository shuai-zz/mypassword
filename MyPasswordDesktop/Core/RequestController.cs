using System;
using System.Collections.Generic;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Core.Web.Pkce;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Rpc.Request;
using MyPasswordDesktop.Rpc.Response;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// Handles HTTP requests from the Chrome extension. Replaces the Java
    /// reflection-annotation dispatcher with an explicit, AOT-safe route table.
    /// </summary>
    public sealed class RequestController
    {
        private readonly Dictionary<string, OAuthAuthenticator> _authenticators = new();

        public RequestController()
        {
            foreach (RecoveryConfig rc in VaultManager.Current.GetRecoveryConfigs())
            {
                string provider = rc.oauth_provider;
                OAuthAuthenticator auth = provider switch
                {
                    "google" => new GoogleAuthenticator(),
                    "microsoft" => new MicrosoftAuthenticator(),
                    _ => null,
                };
                if (auth == null)
                {
                    throw new ArgumentException("Unknown OAuth provider: " + provider);
                }
                Log.Info($"add provider {provider}: {auth.GetType().Name}");
                _authenticators[provider] = auth;
            }
        }

        // ── routing ─────────────────────────────────────────────────────────

        public object Handle(string method, string path, string query, string body)
        {
            Dictionary<string, string> q = ParseQuery(query);
            string[] seg = path.Split('/'); // "/items/3/get" -> ["", "items", "3", "get"]

            if (method == "POST")
            {
                switch (path)
                {
                    case "/info": return Info();
                    case "/pair": return RequestPair(Body<PairRequest>(body));
                    case "/totps/add": return TotpAdd(Body<TotpAddRequest>(body));
                    case "/totps/get": return TotpGet(Body<TotpGetRequest>(body));
                    case "/passkeys/add": return PasskeyAdd(Body<PasskeyAddRequest>(body));
                    case "/passkeys/login": return PasskeyLogin(Body<PasskeyLoginRequest>(body));
                    case "/items/create": return ItemCreate(Body<ItemRequest>(body));
                    case "/vault/lock": return VaultLock();
                    case "/vault/unlock": return VaultUnlock(Body<VaultPasswordRequest>(body));
                    case "/activate": return Activate();
                }
                if (seg.Length == 4 && seg[1] == "items" && long.TryParse(seg[2], out long postId))
                {
                    if (seg[3] == "copy") return ItemCopyPassword(postId);
                    if (seg[3] == "update") return ItemUpdate(postId, Body<ItemRequest>(body));
                }
            }
            else if (method == "GET")
            {
                if (path == "/items/list")
                {
                    return List(int.TryParse(q.GetValueOrDefault("type"), out int t) ? t : 0);
                }
                if (seg.Length == 4 && seg[1] == "items" && seg[3] == "get"
                    && long.TryParse(seg[2], out long getId))
                {
                    return ItemGet(getId);
                }
                if (seg.Length == 4 && seg[1] == "oauth")
                {
                    string provider = seg[2];
                    if (seg[3] == "start")
                    {
                        bool recover = bool.TryParse(q.GetValueOrDefault("recover"), out bool r) && r;
                        return OauthStart(provider, recover);
                    }
                    if (seg[3] == "callback")
                    {
                        return OauthCallback(provider, q.GetValueOrDefault("code"));
                    }
                }
            }
            return HttpDaemon.NotProcessed;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var map = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query))
            {
                return map;
            }
            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int n = pair.IndexOf('=');
                if (n >= 1)
                {
                    map[pair.Substring(0, n)] = Uri.UnescapeDataString(pair.Substring(n + 1));
                }
            }
            return map;
        }

        private static T Body<T>(string body)
            => string.IsNullOrEmpty(body) ? default : JsonUtils.FromJson<T>(body);

        private static byte[] GetKey()
        {
            byte[] key = Session.Current.GetKey();
            if (key == null)
            {
                throw new VaultException(ErrorCode.VAULT_LOCKED, "Vault is locked.");
            }
            return key;
        }

        // ── endpoints ───────────────────────────────────────────────────────

        public InfoResponse Info()
        {
            VaultManager vm = VaultManager.Current;
            return new InfoResponse
            {
                data = new InfoResponse.InfoData
                {
                    initialized = vm.IsInitialized(),
                    locked = Session.Current.IsLocked(),
                    database = FileUtils.GetDbFile(),
                    appVersion = vm.GetAppVersion(),
                    dataVersion = vm.GetDataVersion(),
                    caller = Extension.Current,
                },
            };
        }

        public PairResponse RequestPair(PairRequest pr)
        {
            GetKey(); // ensure vault unlocked
            ExtensionConfig ec = VaultManager.Current.SaveExtensionRequest(pr.name, pr.device);
            UiBridge.ShowPairRequest?.Invoke(ec);
            return new PairResponse
            {
                data = new PairResponse.PairResponseData { id = ec.id, seed = ec.seed },
            };
        }

        public object OauthStart(string provider, bool isRecover)
        {
            if (!_authenticators.TryGetValue(provider, out OAuthAuthenticator auth))
            {
                return "<html><body>OAuth provider not found.</body></html>";
            }
            return "redirect:" + auth.StartOAuth(isRecover);
        }

        public string OauthCallback(string provider, string code)
        {
            VaultManager vm = VaultManager.Current;
            if (!_authenticators.TryGetValue(provider, out OAuthAuthenticator auth))
            {
                return HtmlPage("OAuth provider not found.");
            }
            Log.Info("exchange code: " + code);
            OAuthUser user = auth.ExchangeOAuthId(code);
            if (user == null)
            {
                return HtmlPage("OAuth login failed.");
            }
            string displayProvider = char.ToUpper(provider[0]) + provider.Substring(1);
            if (auth.IsRecoverMode())
            {
                Log.Info("unlock vault by oauth...");
                byte[] dek = vm.UnlockVaultByOAuth(user);
                if (dek == null)
                {
                    return HtmlPage("Unlock vault by OAuth failed. Make sure you logged in with correct user account.");
                }
                Session.Current.SetKey(UnlockType.OAUTH, dek);
                vm.FireVaultUnlocked();
                return HtmlPage("<p>You have successfully logged in " + displayProvider
                    + " and unlocked your vault.</p><p>Please reset your master password in Settings - Password.</p>");
            }
            Log.Info("add oauth recovery...");
            byte[] sessionDek = Session.Current.GetKey();
            if (sessionDek == null)
            {
                return HtmlPage("Vault is locked. Please unlock your vault first.");
            }
            vm.SaveOAuthRecovery(provider, user.name, user.email, user.oauthId, sessionDek);
            string displayUser = !string.IsNullOrEmpty(user.name) ? user.name : "";
            string displayEmail = !string.IsNullOrEmpty(user.email) ? " &lt;" + user.email + "&gt;" : "";
            return HtmlPage("<p>You have successfully logged in " + displayProvider + " account " + displayUser
                + displayEmail + ".</p><p>You can use your " + displayProvider
                + " account to unlock your vault for emergency.</p>");
        }

        public ItemResponse TotpAdd(TotpAddRequest req)
        {
            byte[] key = GetKey();
            AbstractItemData item = VaultManager.Current.GetItem(key, req.itemId);
            if (item is not LoginItemData login)
            {
                throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Login item not found: " + req.itemId);
            }
            if (login.data == null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Login item has no data: " + req.itemId);
            }
            if (login.data.totp != null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Login item already has TOTP: " + req.itemId);
            }
            login.data.totp = TotpUtils.ParseUri(req.uri);
            VaultManager.Current.UpdateItem(key, login);
            VaultManager.Current.FireItemsChanged();
            Log.Info($"added TOTP for item {req.itemId} (issuer={login.data.totp.issuer})");
            return new ItemResponse { item = StripSecrets(login, false) };
        }

        public StringResponse TotpGet(TotpGetRequest req)
        {
            byte[] key = GetKey();
            AbstractItemData item = VaultManager.Current.GetItem(key, req.itemId);
            if (item is not LoginItemData login)
            {
                throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Login item not found: " + req.itemId);
            }
            if (login.data?.totp == null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Login item has no data: " + req.itemId);
            }
            return new StringResponse { data = TotpUtils.GetTotp(login.data.totp) };
        }

        public PasskeyAddResponse PasskeyAdd(PasskeyAddRequest req)
        {
            byte[] key = GetKey();
            AbstractItemData item = VaultManager.Current.GetItem(key, req.itemId);
            if (item is not LoginItemData login)
            {
                throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Login item not found: " + req.itemId);
            }
            if (login.data == null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Login item has no data: " + req.itemId);
            }
            if (login.data.passkey != null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Login item already has a passkey: " + req.itemId);
            }

            PasskeyBuilder.Result built = PasskeyBuilder.Build(req);
            login.data.passkey = built.Data;
            VaultManager.Current.UpdateItem(key, login);
            VaultManager.Current.FireItemsChanged();
            Log.Info($"added passkey for item {req.itemId} (rp={built.Data.relyingPartyId})");

            string credIdB64 = Base64Utils.B64(built.CredentialId);
            return new PasskeyAddResponse
            {
                id = credIdB64,
                rawId = credIdB64,
                type = "public-key",
                authenticatorAttachment = "platform",
                response = new PasskeyAddResponse.PasskeyResponse
                {
                    clientDataJSON = Base64Utils.B64(built.ClientDataJson),
                    authenticatorData = Base64Utils.B64(built.AuthenticatorData),
                    publicKey = Base64Utils.B64(built.PublicKeySpki),
                    publicKeyAlgorithm = built.PublicKeyAlgorithm,
                    attestationObject = Base64Utils.B64(built.AttestationObject),
                    transports = ["internal"],
                },
            };
        }

        public PasskeyLoginResponse PasskeyLogin(PasskeyLoginRequest req)
        {
            byte[] key = GetKey();
            AbstractItemData item = VaultManager.Current.GetItem(key, req.itemId);
            if (item is not LoginItemData login)
            {
                throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Login item not found: " + req.itemId);
            }
            if (login.data?.passkey == null)
            {
                throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Login item has no passkey: " + req.itemId);
            }

            PasskeySigner.Result signed = PasskeySigner.Sign(login, req);
            Log.Info($"signed passkey assertion for item {req.itemId} (rp={login.data.passkey.relyingPartyId})");

            string credIdB64 = Base64Utils.B64(signed.CredentialId);
            var resp = new PasskeyLoginResponse
            {
                id = credIdB64,
                rawId = credIdB64,
                type = "public-key",
                authenticatorAttachment = "platform",
                response = new PasskeyLoginResponse.AssertionResponse
                {
                    clientDataJSON = Base64Utils.B64(signed.ClientDataJson),
                    authenticatorData = Base64Utils.B64(signed.AuthenticatorData),
                    signature = Base64Utils.B64(signed.SignatureDer),
                },
            };
            if (signed.UserHandle != null)
            {
                resp.response.userHandle = Base64Utils.B64(signed.UserHandle);
            }
            return resp;
        }

        public ItemsResponse List(int type)
        {
            byte[] key = GetKey();
            List<AbstractItemData> items = type == 0
                ? VaultManager.Current.GetItems(key)
                : VaultManager.Current.GetItems(key, type);
            foreach (AbstractItemData item in items)
            {
                if (item is LoginItemData login)
                {
                    StripSecrets(login, false);
                }
            }
            return new ItemsResponse { items = items };
        }

        public ItemResponse ItemGet(long id)
        {
            byte[] key = GetKey();
            AbstractItemData item = VaultManager.Current.GetItem(key, id);
            if (item is LoginItemData login)
            {
                StripSecrets(login, true);
            }
            return new ItemResponse { item = item };
        }

        public BaseResponse ItemCopyPassword(long id)
        {
            byte[] key = GetKey();
            AbstractItemData item = VaultManager.Current.GetItem(key, id);
            if (item is LoginItemData login)
            {
                var fd = (LoginFieldsData)login.Fields;
                if (!string.IsNullOrEmpty(fd.password))
                {
                    UiBridge.CopyPassword?.Invoke(fd.password);
                    return new BaseResponse();
                }
            }
            throw new VaultException(ErrorCode.NO_PASSWORD, "No password found.");
        }

        public StringResponse GeneratePassword(GeneratePasswordRequest req)
        {
            if (req.len < 4 || req.len > 100)
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Invalid len.");
            }
            return new StringResponse { data = PasswordUtils.GeneratePassword(req.len, req.style) };
        }

        public ItemResponse ItemCreate(ItemRequest request)
        {
            byte[] key = GetKey();
            var response = new ItemResponse { item = VaultManager.Current.CreateItem(key, request.item) };
            VaultManager.Current.FireItemsChanged();
            return response;
        }

        public ItemResponse ItemUpdate(long id, ItemRequest request)
        {
            byte[] key = GetKey();
            request.item.id = id;
            var response = new ItemResponse { item = VaultManager.Current.UpdateItem(key, request.item) };
            VaultManager.Current.FireItemsChanged();
            return response;
        }

        public InfoResponse VaultLock()
        {
            if (!VaultManager.Current.IsInitialized())
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Vault is not initialized.");
            }
            Session.Current.Lock();
            return Info();
        }

        public BaseResponse VaultUnlock(VaultPasswordRequest request)
        {
            if (!VaultManager.Current.IsInitialized())
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Vault is not initialized.");
            }
            if (request.password == null || request.password.Length < Constants.PasswordMinLength)
            {
                throw new VaultException(ErrorCode.BAD_PASSWORD, "Bad password.");
            }
            byte[] dek = VaultManager.Current.UnlockVault(request.password.ToCharArray());
            if (dek == null)
            {
                throw new VaultException(ErrorCode.BAD_PASSWORD, "Bad password.");
            }
            Session.Current.SetKey(UnlockType.PASSWORD, dek);
            VaultManager.Current.FireVaultUnlocked();
            return Info();
        }

        public BaseResponse Activate()
        {
            UiBridge.ActivateApp?.Invoke();
            return new BaseResponse();
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static LoginItemData StripSecrets(LoginItemData login, bool keepPassword)
        {
            if (login.data == null)
            {
                return login;
            }
            if (!keepPassword)
            {
                string pwd = login.data.password;
                login.data.password = !string.IsNullOrEmpty(pwd) ? "" : null;
            }
            if (login.data.passkey != null)
            {
                login.data.passkey.b64PrivKey = null;
            }
            if (login.data.totp != null)
            {
                login.data.totp.secret = null;
            }
            return login;
        }

        private static string HtmlPage(string body)
            => "<html><body>" + body + "<p>You can now close this page.</p></body></html>";
    }
}
