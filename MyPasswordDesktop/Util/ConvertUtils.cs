using System;
using System.Text;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Core.Entities;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Converts between the encrypted <see cref="Item"/> entity and the decrypted
    /// <see cref="AbstractItemData"/> payload. The DEK is the raw 32-byte key.
    /// </summary>
    public static class ConvertUtils
    {
        public static AbstractItemData ToItemData(byte[] key, Item item)
        {
            byte[] decrypted = DecryptItemData(key, item);
            AbstractItemData itemData = item.item_type switch
            {
                ItemType.LOGIN => new LoginItemData
                {
                    data = (LoginFieldsData)JsonUtils.FromJson(decrypted, typeof(LoginFieldsData)),
                },
                ItemType.NOTE => new NoteItemData
                {
                    data = (NoteFieldsData)JsonUtils.FromJson(decrypted, typeof(NoteFieldsData)),
                },
                ItemType.IDENTITY => new IdentityItemData
                {
                    data = (IdentityFieldsData)JsonUtils.FromJson(decrypted, typeof(IdentityFieldsData)),
                },
                _ => throw new ArgumentException("Invalid item type: " + item.item_type),
            };
            itemData.id = item.id;
            itemData.item_type = item.item_type;
            itemData.deleted = item.deleted;
            itemData.favorite = item.favorite;
            itemData.updated_at = item.updated_at;
            return itemData;
        }

        public static void Encrypt(byte[] key, Item item, AbstractFields fields)
        {
            string jsonData = JsonUtils.ToJson(fields);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            byte[] iv = EncryptUtils.GenerateIV();
            byte[] encrypted = EncryptUtils.Encrypt(data, key, iv);
            item.b64_encrypted_data = Base64Utils.B64(encrypted);
            item.b64_encrypted_data_iv = Base64Utils.B64(iv);
        }

        public static byte[] DecryptItemData(byte[] key, Item item)
        {
            byte[] encrypted = Base64Utils.B64(item.b64_encrypted_data);
            byte[] iv = Base64Utils.B64(item.b64_encrypted_data_iv);
            return EncryptUtils.Decrypt(encrypted, key, iv);
        }
    }
}
