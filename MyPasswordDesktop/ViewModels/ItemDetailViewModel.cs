using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
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
                RefreshTotp(); // set the initial code + countdown pie
                _totpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _totpTimer.Tick += (_, _) => RefreshTotp();
                _totpTimer.Start();
            }
        }

        private void BuildLogin(LoginItemData login)
        {
            LoginFieldsData d = login.data ?? new LoginFieldsData();
            Rows.Add(new DetailRow(I18n.I18n.T("field.title"), DetailRow.KindText, StringUtils.Normalize(d.title)));
            Rows.Add(new DetailRow(I18n.I18n.T("field.username"), DetailRow.KindText, StringUtils.Normalize(d.username), canCopy: true));
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
            if (_totp == null || _totpRow == null)
            {
                return;
            }
            _totpRow.Value = TotpUtils.GetTotp(_totp);
            // remaining fraction of the current TOTP period -> countdown pie
            int period = _totp.period > 0 ? _totp.period : 30;
            int elapsed = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % period);
            int remaining = period - elapsed;
            // warn (red) in the final 5 seconds before the code rolls over
            bool warn = remaining <= 5;
            _totpRow.TotpPie = BuildTotpPie(remaining / (double)period);
            _totpRow.TotpPieBrush = warn ? PieWarnBrush : PieNormalBrush;
            _totpRow.TotpWarning = warn;
        }

        private const double PieSize = 16.0;

        private static readonly IBrush PieNormalBrush = new SolidColorBrush(Color.FromRgb(0x5C, 0x8D, 0xEF));
        private static readonly IBrush PieWarnBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0x4F, 0x4F));

        /// <summary>
        /// Build a depleting countdown pie for the given remaining fraction (1 = full
        /// circle just after the code rolled over, 0 = empty). The filled wedge ends
        /// at 12 o'clock; its leading edge sweeps clockwise as time runs out, so the
        /// slice is eaten away clockwise.
        /// </summary>
        private static Geometry BuildTotpPie(double fraction)
        {
            const double r = PieSize / 2.0;
            var center = new Point(r, r);
            if (fraction >= 1.0)
            {
                return new EllipseGeometry(new Rect(0, 0, PieSize, PieSize));
            }
            if (fraction <= 0.0)
            {
                return new StreamGeometry();
            }
            double sweepDeg = fraction * 360.0;
            // leading edge moves clockwise away from 12 o'clock; wedge ends at 12 o'clock
            Point start = PointOnCircle(center, r, -90.0 + (1.0 - fraction) * 360.0);
            Point end = PointOnCircle(center, r, -90.0);
            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(center, isFilled: true);
                ctx.LineTo(start);
                ctx.ArcTo(end, new Size(r, r), 0, sweepDeg > 180.0, SweepDirection.Clockwise);
                ctx.LineTo(center);
                ctx.EndFigure(true);
            }
            return geometry;
        }

        private static Point PointOnCircle(Point center, double radius, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
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
