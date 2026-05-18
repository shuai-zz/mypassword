using System.Collections.Generic;
using System.Text.Json.Serialization;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Core.Web.Pkce;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Rpc.Request;
using MyPasswordDesktop.Rpc.Response;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// System.Text.Json source-generation context. Generating the (de)serializers
    /// at compile time keeps JSON handling AOT/trim-safe — no runtime reflection.
    /// </summary>
    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UseStringEnumConverter = true)]
    // ── item data ──
    [JsonSerializable(typeof(AbstractItemData))]
    [JsonSerializable(typeof(LoginItemData))]
    [JsonSerializable(typeof(NoteItemData))]
    [JsonSerializable(typeof(IdentityItemData))]
    [JsonSerializable(typeof(List<AbstractItemData>))]
    [JsonSerializable(typeof(LoginFieldsData))]
    [JsonSerializable(typeof(NoteFieldsData))]
    [JsonSerializable(typeof(IdentityFieldsData))]
    [JsonSerializable(typeof(PasskeyData))]
    [JsonSerializable(typeof(TotpData))]
    // ── rpc ──
    [JsonSerializable(typeof(BaseResponse))]
    [JsonSerializable(typeof(PairRequest))]
    [JsonSerializable(typeof(PairResponse))]
    [JsonSerializable(typeof(StringResponse))]
    [JsonSerializable(typeof(InfoResponse))]
    [JsonSerializable(typeof(ItemResponse))]
    [JsonSerializable(typeof(ItemsResponse))]
    [JsonSerializable(typeof(PasskeyAddResponse))]
    [JsonSerializable(typeof(PasskeyLoginResponse))]
    [JsonSerializable(typeof(GeneratePasswordRequest))]
    [JsonSerializable(typeof(ItemRequest))]
    [JsonSerializable(typeof(OAuth))]
    [JsonSerializable(typeof(PasskeyAddRequest))]
    [JsonSerializable(typeof(PasskeyLoginRequest))]
    [JsonSerializable(typeof(TotpAddRequest))]
    [JsonSerializable(typeof(TotpGetRequest))]
    [JsonSerializable(typeof(VaultPasswordRequest))]
    // ── misc ──
    [JsonSerializable(typeof(Extension))]
    [JsonSerializable(typeof(ClientData))]
    [JsonSerializable(typeof(OAuthConfig))]
    [JsonSerializable(typeof(OAuthResult))]
    [JsonSerializable(typeof(JwtUser))]
    [JsonSerializable(typeof(OAuthUser))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    public partial class MyPasswordJsonContext : JsonSerializerContext
    {
    }
}
