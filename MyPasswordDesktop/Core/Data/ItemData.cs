using System;
using System.Text.Json.Serialization;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core.Data
{
    /// <summary>
    /// Polymorphic item payload. The concrete subtype is selected from the
    /// <c>item_type</c> discriminator by <see cref="ItemDataConverter"/>. The
    /// converter dispatches via the source-generated type infos, so the
    /// concrete subtypes themselves serialize without the converter (no
    /// recursion — the generator does not inherit this attribute).
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(ItemDataConverter))]
    public abstract class AbstractItemData : IComparable<AbstractItemData>
    {
        public long id { get; set; }
        public bool favorite { get; set; }
        public bool deleted { get; set; }
        public long updated_at { get; set; }
        public int item_type { get; set; }

        [JsonIgnore]
        public abstract AbstractFields Fields { get; }

        [JsonIgnore]
        public abstract string Title { get; }

        [JsonIgnore]
        public abstract string Subtitle { get; }

        public int CompareTo(AbstractItemData o)
        {
            int c = string.Compare(Title, o.Title, StringComparison.OrdinalIgnoreCase);
            if (c == 0)
            {
                c = string.Compare(Subtitle, o.Subtitle, StringComparison.OrdinalIgnoreCase);
            }
            return c;
        }
    }

    public sealed class LoginItemData : AbstractItemData
    {
        public LoginItemData() => item_type = ItemType.LOGIN;

        public LoginFieldsData data { get; set; }

        public override AbstractFields Fields => data;

        public override string Title => data == null ? "" : data.title;

        public override string Subtitle => StringUtils.Normalize(data?.username);
    }

    public sealed class NoteItemData : AbstractItemData
    {
        public NoteItemData() => item_type = ItemType.NOTE;

        public NoteFieldsData data { get; set; }

        public override AbstractFields Fields => data;

        public override string Title => StringUtils.Normalize(data?.title);

        public override string Subtitle
        {
            get
            {
                string s = data?.content;
                if (s != null)
                {
                    int pos = s.IndexOf('\n');
                    if (pos > 0)
                    {
                        s = s.Substring(0, pos);
                    }
                    else if (s.Length > 20)
                    {
                        s = s.Substring(0, 20);
                    }
                }
                return StringUtils.Normalize(s);
            }
        }
    }

    public sealed class IdentityItemData : AbstractItemData
    {
        public IdentityItemData() => item_type = ItemType.IDENTITY;

        public IdentityFieldsData data { get; set; }

        public override AbstractFields Fields => data;

        public override string Title => StringUtils.Normalize(data?.name);

        public override string Subtitle
        {
            get
            {
                string s = null;
                if (data != null)
                {
                    if (data.mobiles != null && data.mobiles.Count > 0)
                    {
                        s = data.mobiles[0];
                    }
                    if (s == null && data.telephones != null && data.telephones.Count > 0)
                    {
                        s = data.telephones[0];
                    }
                    if (s == null && !string.IsNullOrEmpty(data.email))
                    {
                        s = data.email;
                    }
                }
                return StringUtils.Normalize(s);
            }
        }
    }
}
