using System;
using System.Collections.Generic;
using System.Linq;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Core.Web.Pkce;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// Owns vault init / unlock / CRUD. The DEK is passed around as the raw
    /// 32-byte AES key.
    /// </summary>
    public sealed class VaultManager
    {
        private DbManager _dbManager;
        private VaultConfig _vaultConfig;
        private int _appVersion;

        private Action _onOAuthChanged;
        private Action _onVaultUnlocked;
        private Action _onItemsChanged;

        public static VaultManager Current { get; private set; }

        public VaultManager(DbManager dbManager)
        {
            _dbManager = dbManager;
            _vaultConfig = dbManager.QueryVaultConfig();
            Current = this;
        }

        public void BackupDb() => _dbManager.BackupDb();

        public bool IsInitialized() => _vaultConfig != null;

        // ── settings ────────────────────────────────────────────────────────

        public string GetSetting(string key, string defaultValue)
        {
            // the vault may already be closed during app shutdown
            if (_dbManager == null)
            {
                return defaultValue;
            }
            VaultSetting vs = _dbManager.QueryFirst<VaultSetting>("where setting_key = ?", key);
            return vs?.setting_value ?? defaultValue;
        }

        public long GetSetting(string key, long defaultValue)
            => long.TryParse(GetSetting(key, defaultValue.ToString()), out long v) ? v : defaultValue;

        public int GetSetting(string key, int defaultValue)
            => int.TryParse(GetSetting(key, defaultValue.ToString()), out int v) ? v : defaultValue;

        public void SetSetting(string key, string value)
        {
            Log.Info($"set setting {key} = {value}");
            VaultSetting vs = _dbManager.QueryFirst<VaultSetting>("where setting_key = ?", key);
            if (vs == null)
            {
                vs = new VaultSetting { setting_key = key, setting_value = value };
                _dbManager.Insert(vs);
            }
            else
            {
                vs.setting_value = value;
                _dbManager.Update(vs, "setting_value");
            }
        }

        public void SetSetting(string key, long value) => SetSetting(key, value.ToString());

        public void SetSetting(string key, int value) => SetSetting(key, value.ToString());

        public int GetAppVersion()
        {
            if (_appVersion == 0)
            {
                _appVersion = _dbManager.QueryAppVersion();
            }
            return _appVersion;
        }

        public int GetDataVersion() => _dbManager.QueryDataVersion();

        // ── callbacks ───────────────────────────────────────────────────────

        public void SetOnOAuthChanged(Action callback) => _onOAuthChanged = callback;

        public void SetOnVaultUnlocked(Action callback) => _onVaultUnlocked = callback;

        public void FireVaultUnlocked() => _onVaultUnlocked?.Invoke();

        public void SetOnItemsChanged(Action callback) => _onItemsChanged = callback;

        public void FireItemsChanged() => _onItemsChanged?.Invoke();

        // ── extensions ──────────────────────────────────────────────────────

        public ExtensionConfig GetExtension(long id)
            => _dbManager.QueryFirst<ExtensionConfig>("WHERE id = ?", id);

        public List<ExtensionConfig> GetExtensions()
            => _dbManager.QueryForList<ExtensionConfig>("");

        public ExtensionConfig SaveExtensionRequest(string name, string device)
        {
            var ec = new ExtensionConfig
            {
                id = IdUtils.NextId(),
                approve = false,
                name = StringUtils.CheckNotEmpty("name", name),
                device = StringUtils.CheckNotEmpty("device", device),
                seed = PasswordUtils.GeneratePassword(16, PasswordUtils.StyleAlphabetNumber),
            };
            _dbManager.Insert(ec);
            return ec;
        }

        public void ApproveExtension(long id, bool approve)
        {
            ExtensionConfig ec = _dbManager.QueryFirst<ExtensionConfig>("WHERE id = ?", id)
                ?? throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Extension request not found.");
            if (approve)
            {
                ec.approve = true;
                _dbManager.Update(ec, "approve");
            }
            else
            {
                _dbManager.Delete(ec);
            }
        }

        public List<RecoveryConfig> GetRecoveryConfigs()
            => _dbManager.QueryForList<RecoveryConfig>("");

        public RecoveryConfig GetRecoveryConfig(string provider)
            => _dbManager.QueryFirst<RecoveryConfig>("WHERE oauth_provider = ?", provider);

        // ── items ───────────────────────────────────────────────────────────

        private Item GetItem(long id) => _dbManager.QueryFirst<Item>("WHERE id = ?", id);

        public AbstractItemData GetItem(byte[] key, long id)
        {
            Item item = GetItem(id)
                ?? throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Item not found: " + id);
            return ConvertUtils.ToItemData(key, item);
        }

        public List<AbstractItemData> GetItems(byte[] key)
            => GetRawItems().Select(it => ConvertUtils.ToItemData(key, it)).ToList();

        public List<AbstractItemData> GetItems(byte[] key, int type)
            => GetRawItems(type).Select(it => ConvertUtils.ToItemData(key, it)).ToList();

        public List<Item> GetRawItems() => _dbManager.QueryForList<Item>("");

        public List<Item> GetRawItems(int type) => _dbManager.QueryForList<Item>("WHERE item_type = ?", type);

        public AbstractItemData CreateItem(byte[] key, AbstractItemData data)
        {
            if (data.Fields == null)
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Missing fields.");
            }
            string errField = data.Fields.Check();
            if (errField != null)
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Invalid field: " + errField);
            }
            var item = new Item
            {
                id = IdUtils.NextId(),
                item_type = data.item_type,
                deleted = false,
                favorite = false,
                updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
            ConvertUtils.Encrypt(key, item, data.Fields);
            _dbManager.Insert(item);
            _dbManager.IncDataVersion();
            return ConvertUtils.ToItemData(key, item);
        }

        public AbstractItemData DeleteItem(byte[] key, long id)
        {
            Item item = GetItem(id)
                ?? throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Item not found: " + id);
            if (!item.deleted)
            {
                item.deleted = true;
                item.updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _dbManager.Update(item, "deleted", "updated_at");
                _dbManager.IncDataVersion();
            }
            return ConvertUtils.ToItemData(key, item);
        }

        public AbstractItemData RestoreItem(byte[] key, long id)
        {
            Item item = GetItem(id)
                ?? throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Item not found: " + id);
            if (item.deleted)
            {
                item.deleted = false;
                item.updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _dbManager.Update(item, "deleted", "updated_at");
                _dbManager.IncDataVersion();
            }
            return ConvertUtils.ToItemData(key, item);
        }

        public AbstractItemData UpdateItem(byte[] key, AbstractItemData data)
        {
            string errField = data.Fields.Check();
            if (errField != null)
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Invalid field: " + errField);
            }
            Item item = GetItem(data.id)
                ?? throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Item not found: " + data.id);
            if (item.item_type != data.item_type)
            {
                throw new VaultException(ErrorCode.BAD_FIELD, "Item type not match: " + data.item_type);
            }
            ConvertUtils.Encrypt(key, item, data.Fields);
            item.updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _dbManager.Tx(() =>
            {
                _dbManager.Execute(
                    "INSERT INTO ItemHistory (hid, rid, b64_encrypted_data, b64_encrypted_data_iv, updated_at) SELECT "
                    + IdUtils.NextId()
                    + ", id, b64_encrypted_data, b64_encrypted_data_iv, updated_at FROM Item where id = ?",
                    item.id);
                _dbManager.Update(item, "b64_encrypted_data", "b64_encrypted_data_iv", "updated_at");
                _dbManager.IncDataVersion();
            });
            return ConvertUtils.ToItemData(key, item);
        }

        // ── vault key management ────────────────────────────────────────────

        public byte[] InitVault(string password)
        {
            if (IsInitialized())
            {
                throw new InvalidOperationException("Vault already initialized.");
            }
            byte[] dek = EncryptUtils.GenerateKey();
            EncryptDEK(password, dek);
            return dek;
        }

        private void EncryptDEK(string password, byte[] dek)
        {
            const int pbeIterations = 1_000_000;
            byte[] pbeSalt = EncryptUtils.GenerateSalt();
            byte[] pbeKey = EncryptUtils.DerivePbeKey(password.ToCharArray(), pbeSalt, pbeIterations);
            byte[] encryptedDekIv = EncryptUtils.GenerateIV();
            byte[] encryptedDek = EncryptUtils.Encrypt(dek, pbeKey, encryptedDekIv);
            var vc = new VaultConfig
            {
                id = 1,
                pbe_iterations = pbeIterations,
                b64_pbe_salt = Base64Utils.B64(pbeSalt),
                b64_encrypted_dek = Base64Utils.B64(encryptedDek),
                b64_encrypted_dek_iv = Base64Utils.B64(encryptedDekIv),
            };
            _dbManager.Tx(() =>
            {
                _dbManager.Execute("DELETE FROM VaultConfig WHERE id = 1");
                _dbManager.Insert(vc);
            });
            _vaultConfig = vc;
        }

        public void SaveOAuthRecovery(string provider, string name, string email, string oauthId, byte[] dek)
        {
            RecoveryConfig rc = _dbManager.QueryFirst<RecoveryConfig>("where oauth_provider = ?", provider)
                ?? throw new VaultException(ErrorCode.BAD_REQUEST, "OAuth provider not found: " + provider);
            byte[] hmacKey = EncryptUtils.GenerateKey();
            byte[] uidHash = HashUtils.Sha256(oauthId);
            const int pbeIterations = 1_000_000;
            byte[] pbeSalt = EncryptUtils.GenerateSalt();
            string password = Convert.ToHexStringLower(HashUtils.HmacSha256(oauthId, hmacKey));
            byte[] pbeKey = EncryptUtils.DerivePbeKey(password.ToCharArray(), pbeSalt, pbeIterations);
            byte[] encryptedDekIv = EncryptUtils.GenerateIV();
            byte[] encryptedDek = EncryptUtils.Encrypt(dek, pbeKey, encryptedDekIv);

            rc.oauth_name = name ?? "";
            rc.oauth_email = email ?? "";
            rc.b64_uid_hash = Base64Utils.B64(uidHash);
            rc.b64_uid_hash_hmac = Base64Utils.B64(hmacKey);
            rc.pbe_iterations = pbeIterations;
            rc.b64_pbe_salt = Base64Utils.B64(pbeSalt);
            rc.b64_encrypted_dek = Base64Utils.B64(encryptedDek);
            rc.b64_encrypted_dek_iv = Base64Utils.B64(encryptedDekIv);
            rc.updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _dbManager.Update(rc, "oauth_name", "oauth_email", "b64_uid_hash", "b64_uid_hash_hmac", "pbe_iterations",
                "b64_pbe_salt", "b64_encrypted_dek", "b64_encrypted_dek_iv", "updated_at");
            _onOAuthChanged?.Invoke();
        }

        public void DisconnectOAuth(string provider)
        {
            RecoveryConfig rc = _dbManager.QueryFirst<RecoveryConfig>("where oauth_provider = ?", provider);
            if (rc != null)
            {
                rc.oauth_name = "";
                rc.oauth_email = "";
                rc.b64_uid_hash = "";
                rc.b64_uid_hash_hmac = "";
                rc.b64_encrypted_dek = "";
                rc.b64_encrypted_dek_iv = "";
                rc.updated_at = 0;
                _dbManager.Update(rc, "oauth_name", "oauth_email", "b64_uid_hash", "b64_uid_hash_hmac",
                    "b64_encrypted_dek", "b64_encrypted_dek_iv", "updated_at");
            }
        }

        /// <summary>Returns the DEK, or <c>null</c> on bad OAuth identity.</summary>
        public byte[] UnlockVaultByOAuth(OAuthUser oauthUser)
        {
            if (!IsInitialized())
            {
                throw new InvalidOperationException("Vault not initialized.");
            }
            RecoveryConfig rc = _dbManager.QueryFirst<RecoveryConfig>("where oauth_provider = ?", oauthUser.provider);
            if (rc == null || string.IsNullOrEmpty(rc.b64_uid_hash) || string.IsNullOrEmpty(rc.b64_uid_hash_hmac))
            {
                return null;
            }
            string oauthId = oauthUser.oauthId;
            byte[] uidHash = HashUtils.Sha256(oauthId);
            if (!Base64Utils.B64(uidHash).Equals(rc.b64_uid_hash))
            {
                Log.Warn("oauth id not match.");
                return null;
            }
            byte[] hmacKey = Base64Utils.B64(rc.b64_uid_hash_hmac);
            byte[] pbeSalt = Base64Utils.B64(rc.b64_pbe_salt);
            string password = Convert.ToHexStringLower(HashUtils.HmacSha256(oauthId, hmacKey));
            byte[] pbeKey = EncryptUtils.DerivePbeKey(password.ToCharArray(), pbeSalt, rc.pbe_iterations);
            byte[] encryptedDekIv = Base64Utils.B64(rc.b64_encrypted_dek_iv);
            byte[] encryptedDek = Base64Utils.B64(rc.b64_encrypted_dek);
            try
            {
                return EncryptUtils.Decrypt(encryptedDek, pbeKey, encryptedDekIv);
            }
            catch (EncryptException)
            {
                return null;
            }
        }

        /// <summary>Returns the DEK, or <c>null</c> on bad password.</summary>
        public byte[] UnlockVault(char[] password)
        {
            if (!IsInitialized())
            {
                throw new InvalidOperationException("Vault not initialized.");
            }
            byte[] pbeKey = EncryptUtils.DerivePbeKey(password, Base64Utils.B64(_vaultConfig.b64_pbe_salt),
                _vaultConfig.pbe_iterations);
            byte[] encryptedDek = Base64Utils.B64(_vaultConfig.b64_encrypted_dek);
            byte[] encryptedDekIv = Base64Utils.B64(_vaultConfig.b64_encrypted_dek_iv);
            try
            {
                return EncryptUtils.Decrypt(encryptedDek, pbeKey, encryptedDekIv);
            }
            catch (EncryptException)
            {
                return null;
            }
        }

        public bool ChangeMasterPassword(string oldPassword, string newPassword)
        {
            if (!IsInitialized())
            {
                throw new InvalidOperationException("Vault not initialized.");
            }
            byte[] dek = UnlockVault(oldPassword.ToCharArray());
            if (dek == null)
            {
                return false;
            }
            EncryptDEK(newPassword, dek);
            return true;
        }

        public void ResetMasterPassword(string newPassword, byte[] dek)
        {
            if (!IsInitialized())
            {
                throw new InvalidOperationException("Vault not initialized.");
            }
            EncryptDEK(newPassword, dek);
        }

        /// <summary>Close the vault. Idempotent — safe to call more than once.</summary>
        public void Close()
        {
            _dbManager?.Close();
            _dbManager = null;
        }
    }
}
