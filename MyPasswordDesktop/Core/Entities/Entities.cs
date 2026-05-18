using System;

namespace MyPasswordDesktop.Core.Entities
{
    /// <summary>Marks the primary-key property of an ORM entity (≈ JPA @Id).</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IdAttribute : Attribute
    {
    }

    /// <summary>Extension pairing record.</summary>
    public sealed class ExtensionConfig
    {
        [Id] public long id { get; set; }
        public bool approve { get; set; }
        public string seed { get; set; }   // random seed "a1b2c3"
        public string name { get; set; }   // e.g. "MyPassword Chrome Extension v1.0"
        public string device { get; set; } // e.g. "Windows 11 (Homer's PC)"
    }

    public sealed class Item
    {
        [Id] public long id { get; set; }
        public bool favorite { get; set; }
        public bool deleted { get; set; }
        public int item_type { get; set; }
        public long updated_at { get; set; }
        public string b64_encrypted_data { get; set; }
        public string b64_encrypted_data_iv { get; set; }
    }

    public sealed class ItemHistory
    {
        [Id] public long hid { get; set; } // history id
        public long rid { get; set; }      // reference id
        public long updated_at { get; set; }
        public string b64_encrypted_data { get; set; }
        public string b64_encrypted_data_iv { get; set; }
    }

    public sealed class RecoveryConfig
    {
        [Id] public string oauth_provider { get; set; }
        public string oauth_config_json { get; set; }
        public string oauth_name { get; set; }
        public string oauth_email { get; set; }
        public string b64_uid_hash { get; set; }      // sha256(uid)
        public string b64_uid_hash_hmac { get; set; } // random hmac key
        public int pbe_iterations { get; set; }
        public string b64_pbe_salt { get; set; }
        public string b64_encrypted_dek { get; set; }
        public string b64_encrypted_dek_iv { get; set; }
        public long updated_at { get; set; }
    }

    /// <summary>Derive the PBE key from the master password.</summary>
    public sealed class VaultConfig
    {
        [Id] public int id { get; set; }
        public int pbe_iterations { get; set; }
        public string b64_pbe_salt { get; set; }
        public string b64_encrypted_dek { get; set; }
        public string b64_encrypted_dek_iv { get; set; }
    }

    public sealed class VaultSetting
    {
        [Id] public string setting_key { get; set; }
        public string setting_value { get; set; }
    }

    public sealed class VaultVersion
    {
        public const int ID_DATA_VERSION = 1;
        public const int ID_APP_VERSION = 2;

        [Id] public int id { get; set; }
        public int version { get; set; }
    }
}
