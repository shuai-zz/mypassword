using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyPasswordDesktop.Core.Data;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Polymorphic JSON converter for <see cref="AbstractItemData"/>. Replaces
    /// Jackson's <c>@JsonTypeInfo</c>/<c>@JsonSubTypes</c>: the concrete subtype
    /// is chosen from the <c>item_type</c> discriminator. Dispatches through the
    /// source-generated <see cref="MyPasswordJsonContext"/> type infos, keeping
    /// it trim- and AOT-safe and position-independent.
    /// </summary>
    public sealed class ItemDataConverter : JsonConverter<AbstractItemData>
    {
        public override AbstractItemData Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;
            int itemType = 0;
            if (root.TryGetProperty("item_type", out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                itemType = prop.GetInt32();
            }
            return itemType switch
            {
                ItemType.LOGIN => root.Deserialize(MyPasswordJsonContext.Default.LoginItemData),
                ItemType.NOTE => root.Deserialize(MyPasswordJsonContext.Default.NoteItemData),
                ItemType.IDENTITY => root.Deserialize(MyPasswordJsonContext.Default.IdentityItemData),
                _ => throw new JsonException("Invalid item_type: " + itemType),
            };
        }

        public override void Write(Utf8JsonWriter writer, AbstractItemData value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case LoginItemData login:
                    JsonSerializer.Serialize(writer, login, MyPasswordJsonContext.Default.LoginItemData);
                    break;
                case NoteItemData note:
                    JsonSerializer.Serialize(writer, note, MyPasswordJsonContext.Default.NoteItemData);
                    break;
                case IdentityItemData identity:
                    JsonSerializer.Serialize(writer, identity, MyPasswordJsonContext.Default.IdentityItemData);
                    break;
                default:
                    throw new JsonException("Unknown item type: " + value?.GetType());
            }
        }
    }
}
