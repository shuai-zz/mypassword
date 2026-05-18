using System.Collections.Generic;

namespace MyPasswordDesktop.Core.Data
{
    /// <summary>Base class for the decrypted, type-specific item payload.</summary>
    public abstract class AbstractFields
    {
        /// <summary>Returns the offending field name, or <c>null</c> when valid.</summary>
        public abstract string Check();
    }

    public sealed class LoginFieldsData : AbstractFields
    {
        public string title { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public PasskeyData passkey { get; set; }
        public TotpData totp { get; set; }
        public List<string> websites { get; set; }
        public string ga { get; set; }
        public string memo { get; set; }

        public override string Check()
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "title";
            }
            return null;
        }
    }

    public sealed class NoteFieldsData : AbstractFields
    {
        public string title { get; set; }
        public string content { get; set; }

        public override string Check()
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "title";
            }
            return null;
        }
    }

    public sealed class IdentityFieldsData : AbstractFields
    {
        public string name { get; set; }
        public string email { get; set; }
        public string passport_number { get; set; }
        public string identity_number { get; set; }
        public string tax_number { get; set; }
        public List<string> mobiles { get; set; }
        public List<string> telephones { get; set; }
        public string address { get; set; }
        public string zip_code { get; set; }
        public string memo { get; set; }

        public override string Check()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "name";
            }
            return null;
        }
    }
}
