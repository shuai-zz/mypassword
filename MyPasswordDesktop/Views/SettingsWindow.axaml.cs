using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.I18n;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Views
{
    /// <summary>Tab-based settings dialog (General / Security / Password / Extension).</summary>
    public partial class SettingsWindow : Window
    {
        private const int MinPwLen = 8;

        private static readonly string[] Languages = { "", "en", "zh" };
        private static readonly string[] Themes = { "", "light", "dark" };
        private static readonly int[] AutoLockMinutes = { 1, 2, 5, 10, 15, 30, 60, 0 };
        private static readonly int[] ClearClipboardMinutes = { 1, 2, 5, 0 };
        private static readonly int[] DeleteAfterDays = { 7, 14, 30, 90, 180, 0 };

        public SettingsWindow()
        {
            AvaloniaXamlLoader.Load(this);
            BuildGeneral();
            BuildSecurity();
            BuildPassword();
            BuildExtension();

            // OAuth login completes on an HTTP thread — rebuild the rows so the
            // status updates from "<not logged in>" without reopening Settings.
            VaultManager.Current.SetOnOAuthChanged(() =>
                Dispatcher.UIThread.Post(BuildOAuthRows));
            Closed += (_, _) => VaultManager.Current.SetOnOAuthChanged(null);
        }

        private static VaultManager Vault => VaultManager.Current;

        // ── General ──────────────────────────────────────────────────────────

        private void BuildGeneral()
        {
            var tray = this.FindControl<CheckBox>("TrayCheck");
            tray.IsChecked = Vault.GetSetting(SettingKey.KEEP_TRAY_ICON, 1) != 0;
            tray.IsCheckedChanged += (_, _) =>
                Vault.SetSetting(SettingKey.KEEP_TRAY_ICON, tray.IsChecked == true ? 1 : 0);

            var lang = this.FindControl<ComboBox>("LangCombo");
            lang.ItemsSource = new List<string>
            {
                I18n.I18n.T("settings.language.system"),
                I18n.I18n.T("settings.language.name.en"),
                I18n.I18n.T("settings.language.name.zh"),
            };
            string curLang = Vault.GetSetting(SettingKey.LANGUAGE, "");
            lang.SelectedIndex = Math.Max(0, Array.IndexOf(Languages, curLang));
            lang.SelectionChanged += (_, _) =>
                Vault.SetSetting(SettingKey.LANGUAGE, Languages[Math.Max(0, lang.SelectedIndex)]);

            var theme = this.FindControl<ComboBox>("ThemeCombo");
            theme.ItemsSource = new List<string>
            {
                I18n.I18n.T("settings.theme.system"),
                I18n.I18n.T("settings.theme.light"),
                I18n.I18n.T("settings.theme.dark"),
            };
            string curTheme = Vault.GetSetting(SettingKey.THEME, "");
            theme.SelectedIndex = Math.Max(0, Array.IndexOf(Themes, curTheme));
            theme.SelectionChanged += (_, _) =>
            {
                string value = Themes[Math.Max(0, theme.SelectedIndex)];
                Vault.SetSetting(SettingKey.THEME, value);
                ThemeManager.Apply(value);
            };

            var deleteAfter = this.FindControl<ComboBox>("DeleteAfterCombo");
            deleteAfter.ItemsSource = DaysOptions(DeleteAfterDays);
            deleteAfter.SelectedIndex = IndexOf(DeleteAfterDays, Vault.GetSetting(SettingKey.DELETE_AFTER, 90));
            deleteAfter.SelectionChanged += (_, _) =>
                Vault.SetSetting(SettingKey.DELETE_AFTER, DeleteAfterDays[Math.Max(0, deleteAfter.SelectedIndex)]);

            string dbFilePath = FileUtils.GetDbFile() ?? "";
            var dbFileText = this.FindControl<TextBlock>("DbFileText");
            dbFileText.Text = FileUtils.CollapseHome(dbFilePath);
            dbFileText.PointerPressed += (_, _) =>
            {
                if (string.IsNullOrEmpty(dbFilePath)) return;
                string dir = Path.GetDirectoryName(dbFilePath);
                if (!string.IsNullOrEmpty(dir)) ShellUtils.OpenUrl(dir);
            };
            string logDirPath = FileUtils.GetLogDir();
            var logDirText = this.FindControl<TextBlock>("LogDirText");
            logDirText.Text = FileUtils.CollapseHome(logDirPath);
            logDirText.PointerPressed += (_, _) =>
            {
                if (!string.IsNullOrEmpty(logDirPath)) ShellUtils.OpenUrl(logDirPath);
            };

            this.FindControl<TextBlock>("VersionText").Text = GetAppVersion();
        }

        private static string GetAppVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                int plus = informational.IndexOf('+');
                return plus >= 0 ? informational.Substring(0, plus) : informational;
            }
            Version v = asm.GetName().Version;
            return v == null ? "" : $"{v.Major}.{v.Minor}.{v.Build}";
        }

        // ── Security ─────────────────────────────────────────────────────────

        private void BuildSecurity()
        {
            var autoLock = this.FindControl<ComboBox>("AutoLockCombo");
            autoLock.ItemsSource = MinutesOptions(AutoLockMinutes);
            autoLock.SelectedIndex = IndexOf(AutoLockMinutes, Vault.GetSetting(SettingKey.AUTO_LOCK, 10));
            autoLock.SelectionChanged += (_, _) =>
                Vault.SetSetting(SettingKey.AUTO_LOCK, AutoLockMinutes[Math.Max(0, autoLock.SelectedIndex)]);

            var clear = this.FindControl<ComboBox>("ClearClipboardCombo");
            clear.ItemsSource = MinutesOptions(ClearClipboardMinutes);
            clear.SelectedIndex = IndexOf(ClearClipboardMinutes, Vault.GetSetting(SettingKey.CLEAR_CLIPBOARD, 1));
            clear.SelectionChanged += (_, _) =>
                Vault.SetSetting(SettingKey.CLEAR_CLIPBOARD, ClearClipboardMinutes[Math.Max(0, clear.SelectedIndex)]);
        }

        // ── Password ─────────────────────────────────────────────────────────

        private void BuildPassword()
        {
            bool oauthUnlocked = Session.Current.GetUnlockType() == UnlockType.OAUTH;
            var currentRow = this.FindControl<Grid>("CurrentRow");
            currentRow.IsVisible = !oauthUnlocked;

            var currentBox = this.FindControl<TextBox>("CurrentPwBox");
            var newBox = this.FindControl<TextBox>("NewPwBox");
            var confirmBox = this.FindControl<TextBox>("ConfirmPwBox");
            var message = this.FindControl<TextBlock>("PwMessage");
            var changeBtn = this.FindControl<Button>("ChangePwButton");

            changeBtn.Click += (_, _) =>
            {
                string nw = newBox.Text ?? "";
                string cfm = confirmBox.Text ?? "";
                if (nw.Length < MinPwLen)
                {
                    Fail(message, I18n.I18n.T("settings.password.error.too_short", MinPwLen));
                    return;
                }
                if (nw != cfm)
                {
                    Fail(message, I18n.I18n.T("settings.password.error.mismatch"));
                    return;
                }
                if (oauthUnlocked)
                {
                    Vault.ResetMasterPassword(nw, Session.Current.GetKey());
                }
                else
                {
                    if (!Vault.ChangeMasterPassword(currentBox.Text ?? "", nw))
                    {
                        Fail(message, I18n.I18n.T("settings.password.error.wrong"));
                        return;
                    }
                    currentBox.Text = "";
                }
                newBox.Text = "";
                confirmBox.Text = "";
                message.Foreground = Brushes.Green;
                message.Text = I18n.I18n.T("settings.password.success");
            };

            BuildOAuthRows();
        }

        private static void Fail(TextBlock message, string text)
        {
            message.Foreground = Brushes.Red;
            message.Text = text;
        }

        private void BuildOAuthRows()
        {
            var list = this.FindControl<StackPanel>("OAuthList");
            list.Children.Clear();
            foreach (RecoveryConfig rc in Vault.GetRecoveryConfigs())
            {
                bool loggedIn = !string.IsNullOrEmpty(rc.b64_uid_hash);
                string display = char.ToUpper(rc.oauth_provider[0]) + rc.oauth_provider.Substring(1);

                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("110,*,130") };
                grid.Children.Add(Cell(display, 0));
                grid.Children.Add(Cell(loggedIn ? FormatOAuth(rc) : I18n.I18n.T("settings.oauth.not_logged_in"), 1));

                var btn = new Button { HorizontalAlignment = HorizontalAlignment.Right };
                btn.Classes.Add("normal");
                Grid.SetColumn(btn, 2);
                if (loggedIn)
                {
                    btn.Content = I18n.I18n.T("settings.oauth.btn.disconnect");
                    btn.Click += async (_, _) =>
                    {
                        bool ok = await Services.Dialogs.ConfirmAsync(I18n.I18n.T("confirm.title"),
                            I18n.I18n.T("settings.oauth.confirm.disconnect", display));
                        if (ok)
                        {
                            Vault.DisconnectOAuth(rc.oauth_provider);
                            BuildOAuthRows();
                        }
                    };
                }
                else
                {
                    btn.Content = I18n.I18n.T("settings.oauth.btn.login");
                    btn.Click += (_, _) => ShellUtils.OpenUrl(
                        $"http://127.0.0.1:{HttpDaemon.Port}/oauth/{rc.oauth_provider}/start");
                }
                grid.Children.Add(btn);
                list.Children.Add(grid);
            }
        }

        private static string FormatOAuth(RecoveryConfig rc)
        {
            string name = rc.oauth_name ?? "";
            string email = rc.oauth_email ?? "";
            if (name.Length > 0 && email.Length > 0) return $"{name} <{email}>";
            if (email.Length > 0) return email;
            return name.Length > 0 ? name : "Connected";
        }

        // ── Extension ────────────────────────────────────────────────────────

        private void BuildExtension()
        {
            var url = this.FindControl<TextBlock>("ExtensionUrl");
            url.PointerPressed += (_, _) => ShellUtils.OpenUrl(I18n.I18n.T("settings.extension.url"));
            BuildExtensionRows();
        }

        private void BuildExtensionRows()
        {
            var list = this.FindControl<StackPanel>("ExtensionList");
            list.Children.Clear();
            List<ExtensionConfig> extensions = Vault.GetExtensions();
            if (extensions.Count == 0)
            {
                list.Children.Add(new TextBlock
                {
                    Text = I18n.I18n.T("settings.extension.none"),
                    Foreground = Brushes.Gray,
                });
                return;
            }
            foreach (ExtensionConfig ec in extensions)
            {
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("200,*,170") };
                grid.Children.Add(Cell(ec.name, 0));
                grid.Children.Add(Cell(ec.device, 1));

                var actions = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                Grid.SetColumn(actions, 2);
                if (ec.approve)
                {
                    var unpair = new Button { Content = I18n.I18n.T("settings.extension.btn.unpair") };
                    unpair.Classes.Add("normal");
                    unpair.Click += async (_, _) =>
                    {
                        bool ok = await Services.Dialogs.ConfirmAsync(I18n.I18n.T("confirm.title"),
                            I18n.I18n.T("settings.extension.confirm.unpair", ec.name));
                        if (ok)
                        {
                            Vault.ApproveExtension(ec.id, false);
                            BuildExtensionRows();
                        }
                    };
                    actions.Children.Add(unpair);
                }
                else
                {
                    var approve = new Button { Content = I18n.I18n.T("settings.extension.btn.approve") };
                    approve.Classes.Add("normal");
                    approve.Click += (_, _) => { Vault.ApproveExtension(ec.id, true); BuildExtensionRows(); };
                    var reject = new Button { Content = I18n.I18n.T("settings.extension.btn.reject") };
                    reject.Classes.Add("normal");
                    reject.Click += (_, _) => { Vault.ApproveExtension(ec.id, false); BuildExtensionRows(); };
                    actions.Children.Add(approve);
                    actions.Children.Add(reject);
                }
                grid.Children.Add(actions);
                list.Children.Add(grid);
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static Control Cell(string text, int column)
        {
            var tb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tb, column);
            return tb;
        }

        private static List<string> MinutesOptions(int[] values)
        {
            var list = new List<string>();
            foreach (int v in values)
            {
                list.Add(v == 0 ? I18n.I18n.T("settings.never") : I18n.I18n.T("settings.minutes", v));
            }
            return list;
        }

        private static List<string> DaysOptions(int[] values)
        {
            var list = new List<string>();
            foreach (int v in values)
            {
                list.Add(v == 0 ? I18n.I18n.T("settings.never") : I18n.I18n.T("settings.days", v));
            }
            return list;
        }

        private static int IndexOf(int[] values, int value)
        {
            int idx = Array.IndexOf(values, value);
            return idx < 0 ? 0 : idx;
        }
    }
}
