using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.I18n;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>Read-only detail view for the selected item (all 3 item types).</summary>
    public sealed partial class ItemDetailViewModel : ViewModelBase, IDisposable
    {
        public ObservableCollection<DetailRow> Rows { get; } = new();

        public string LastEdit { get; }
        public bool IsDeleted { get; }
        public bool IsActive => !IsDeleted;

        private readonly Action _onEdit;
        private readonly Action _onDelete;
        private readonly Action _onRestore;
        private readonly DispatcherTimer _totpTimer;
        private TotpData _totp;
        private DetailRow _totpRow;

        public ItemDetailViewModel(AbstractItemData item, Action onEdit, Action onDelete, Action onRestore)
        {
            _onEdit = onEdit;
            _onDelete = onDelete;
            _onRestore = onRestore;
            IsDeleted = item.deleted;
            LastEdit = item.updated_at > 0
                ? I18n.I18n.T("detail.last_edit", StringUtils.FormatDateTime(item.updated_at))
                : "";

            switch (item)
            {
                case LoginItemData login: BuildLogin(login); break;
                case NoteItemData note: BuildNote(note); break;
                case IdentityItemData id: BuildIdentity(id); break;
            }

            if (_totp != null)
            {
                _totpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _totpTimer.Tick += (_, _) => RefreshTotp();
                _totpTimer.Start();
            }
        }

        private void BuildLogin(LoginItemData login)
        {
            LoginFieldsData d = login.data ?? new LoginFieldsData();
            Rows.Add(new DetailRow(I18n.I18n.T("field.title"), DetailRow.KindText, StringUtils.Normalize(d.title)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.username"), DetailRow.KindText, StringUtils.Normalize(d.username)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.password"), DetailRow.KindPassword, d.password ?? ""));
            if (d.totp != null)
            {
                _totp = d.totp;
                _totpRow = new DetailRow(I18n.I18n.T("field.totp"), DetailRow.KindTotp, TotpUtils.GetTotp(d.totp));
                Rows.Add(_totpRow);
            }
            if (d.passkey != null)
            {
                Rows.Add(new DetailRow(I18n.I18n.T("field.passkey"), DetailRow.KindText, FormatPasskey(d.passkey)));
            }
            Rows.Add(new DetailRow(I18n.I18n.T("field.websites"), d.websites));
            Rows.Add(new DetailRow(I18n.I18n.T("field.memo"), DetailRow.KindText, StringUtils.Normalize(d.memo)));
        }

        private void BuildNote(NoteItemData note)
        {
            NoteFieldsData d = note.data ?? new NoteFieldsData();
            Rows.Add(new DetailRow(I18n.I18n.T("field.title"), DetailRow.KindText, StringUtils.Normalize(d.title)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.content"), DetailRow.KindText, StringUtils.Normalize(d.content)));
        }

        private void BuildIdentity(IdentityItemData id)
        {
            IdentityFieldsData d = id.data ?? new IdentityFieldsData();
            Rows.Add(new DetailRow(I18n.I18n.T("field.name"), DetailRow.KindText, StringUtils.Normalize(d.name)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.email"), DetailRow.KindText, StringUtils.Normalize(d.email)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.passport"), DetailRow.KindText, StringUtils.Normalize(d.passport_number)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.id_number"), DetailRow.KindText, StringUtils.Normalize(d.identity_number)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.tax_number"), DetailRow.KindText, StringUtils.Normalize(d.tax_number)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.telephones"), d.telephones));
            Rows.Add(new DetailRow(I18n.I18n.T("field.mobiles"), d.mobiles));
            Rows.Add(new DetailRow(I18n.I18n.T("field.address"), DetailRow.KindText, StringUtils.Normalize(d.address)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.zip_code"), DetailRow.KindText, StringUtils.Normalize(d.zip_code)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.memo"), DetailRow.KindText, StringUtils.Normalize(d.memo)));
        }

        private void RefreshTotp()
        {
            if (_totp != null && _totpRow != null)
            {
                _totpRow.Value = TotpUtils.GetTotp(_totp);
            }
        }

        private static string FormatPasskey(PasskeyData p)
        {
            string user = StringUtils.Normalize(p.username);
            string display = StringUtils.Normalize(p.displayName);
            if (user.Length == 0 && display.Length == 0) return "";
            if (user.Length == 0) return display;
            if (display.Length == 0 || user == display) return user;
            return user + " / " + display;
        }

        [RelayCommand]
        private void Edit() => _onEdit?.Invoke();

        [RelayCommand]
        private void Delete() => _onDelete?.Invoke();

        [RelayCommand]
        private void Restore() => _onRestore?.Invoke();

        public void Dispose() => _totpTimer?.Stop();
    }
}
