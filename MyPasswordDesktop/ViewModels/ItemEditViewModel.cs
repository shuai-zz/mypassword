using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.I18n;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>Editable form for creating or editing an item (all 3 types).</summary>
    public sealed partial class ItemEditViewModel : ViewModelBase
    {
        public ObservableCollection<EditRow> Rows { get; } = new();

        private readonly Dictionary<string, EditRow> _byTag = new();
        private readonly int _itemType;
        private readonly AbstractItemData _editingItem;
        private readonly Action<AbstractItemData> _onSave;
        private readonly Action _onCancel;

        private PasskeyData _currentPasskey;
        private TotpData _currentTotp;

        public ItemEditViewModel(AbstractItemData item, int itemType,
            Action<AbstractItemData> onSave, Action onCancel)
        {
            _editingItem = item;
            _itemType = item?.item_type ?? itemType;
            _onSave = onSave;
            _onCancel = onCancel;

            switch (_itemType)
            {
                case ItemType.LOGIN: BuildLogin(item as LoginItemData); break;
                case ItemType.NOTE: BuildNote(item as NoteItemData); break;
                case ItemType.IDENTITY: BuildIdentity(item as IdentityItemData); break;
            }
        }

        private EditRow Add(string tag, string labelKey, string kind, string value = "")
        {
            var row = new EditRow(tag, I18n.I18n.T(labelKey), kind, value);
            _byTag[tag] = row;
            Rows.Add(row);
            return row;
        }

        private void BuildLogin(LoginItemData login)
        {
            LoginFieldsData d = login?.data;
            Add("title", "field.title", EditRow.KindLine, StringUtils.Normalize(d?.title));
            Add("username", "field.username", EditRow.KindLine, StringUtils.Normalize(d?.username));
            Add("password", "field.password", EditRow.KindPassword, StringUtils.Normalize(d?.password));
            EditRow websites = Add("websites", "field.websites", EditRow.KindMulti);
            websites.SetMulti(d?.websites);
            Add("memo", "field.memo", EditRow.KindArea, StringUtils.Normalize(d?.memo));

            _currentTotp = d?.totp;
            if (_currentTotp != null)
            {
                EditRow totpRow = Add("totp", "field.totp", EditRow.KindRemovable, FormatTotp(_currentTotp));
                totpRow.OnRemoved = () => _currentTotp = null;
            }
            _currentPasskey = d?.passkey;
            if (_currentPasskey != null)
            {
                EditRow pkRow = Add("passkey", "field.passkey", EditRow.KindRemovable, FormatPasskey(_currentPasskey));
                pkRow.OnRemoved = () => _currentPasskey = null;
            }
        }

        private void BuildNote(NoteItemData note)
        {
            NoteFieldsData d = note?.data;
            Add("title", "field.title", EditRow.KindLine, StringUtils.Normalize(d?.title));
            Add("content", "field.content", EditRow.KindArea, StringUtils.Normalize(d?.content));
        }

        private void BuildIdentity(IdentityItemData id)
        {
            IdentityFieldsData d = id?.data;
            Add("name", "field.name", EditRow.KindLine, StringUtils.Normalize(d?.name));
            Add("email", "field.email", EditRow.KindLine, StringUtils.Normalize(d?.email));
            Add("passport", "field.passport", EditRow.KindLine, StringUtils.Normalize(d?.passport_number));
            Add("id_number", "field.id_number", EditRow.KindLine, StringUtils.Normalize(d?.identity_number));
            Add("tax_number", "field.tax_number", EditRow.KindLine, StringUtils.Normalize(d?.tax_number));
            Add("mobiles", "field.mobiles", EditRow.KindMulti).SetMulti(d?.mobiles);
            Add("telephones", "field.telephones", EditRow.KindMulti).SetMulti(d?.telephones);
            Add("address", "field.address", EditRow.KindArea, StringUtils.Normalize(d?.address));
            Add("zip_code", "field.zip_code", EditRow.KindLine, StringUtils.Normalize(d?.zip_code));
            Add("memo", "field.memo", EditRow.KindArea, StringUtils.Normalize(d?.memo));
        }

        private string Val(string tag) => (_byTag[tag].Value ?? "").Trim();

        public AbstractItemData CollectData()
        {
            switch (_itemType)
            {
                case ItemType.LOGIN:
                {
                    var data = _editingItem as LoginItemData ?? new LoginItemData();
                    data.item_type = ItemType.LOGIN;
                    data.data = new LoginFieldsData
                    {
                        title = Val("title"),
                        username = Val("username"),
                        password = _byTag["password"].Value ?? "",
                        websites = _byTag["websites"].CollectMulti(),
                        memo = _byTag["memo"].Value ?? "",
                        passkey = _currentPasskey,
                        totp = _currentTotp,
                    };
                    return data;
                }
                case ItemType.NOTE:
                {
                    var data = _editingItem as NoteItemData ?? new NoteItemData();
                    data.item_type = ItemType.NOTE;
                    data.data = new NoteFieldsData
                    {
                        title = Val("title"),
                        content = _byTag["content"].Value ?? "",
                    };
                    return data;
                }
                default:
                {
                    var data = _editingItem as IdentityItemData ?? new IdentityItemData();
                    data.item_type = ItemType.IDENTITY;
                    data.data = new IdentityFieldsData
                    {
                        name = Val("name"),
                        email = Val("email"),
                        passport_number = Val("passport"),
                        identity_number = Val("id_number"),
                        tax_number = Val("tax_number"),
                        mobiles = _byTag["mobiles"].CollectMulti(),
                        telephones = _byTag["telephones"].CollectMulti(),
                        address = Val("address"),
                        zip_code = Val("zip_code"),
                        memo = Val("memo"),
                    };
                    return data;
                }
            }
        }

        private static string FormatTotp(TotpData t)
        {
            string issuer = StringUtils.Normalize(t.issuer);
            string user = StringUtils.Normalize(t.username);
            if (issuer.Length == 0 && user.Length == 0) return "TOTP";
            if (issuer.Length == 0) return user;
            if (user.Length == 0 || issuer == user) return issuer;
            return issuer + " / " + user;
        }

        private static string FormatPasskey(PasskeyData p)
        {
            string user = StringUtils.Normalize(p.username);
            string display = StringUtils.Normalize(p.displayName);
            if (user.Length == 0 && display.Length == 0) return p.relyingPartyId ?? "";
            if (user.Length == 0) return display;
            if (display.Length == 0 || user == display) return user;
            return user + " / " + display;
        }

        [RelayCommand]
        private void Save() => _onSave?.Invoke(CollectData());

        [RelayCommand]
        private void Cancel() => _onCancel?.Invoke();
    }
}
