using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.I18n;

namespace MyPasswordDesktop.Views
{
    /// <summary>First-run dialog: set the master password and initialize the vault.</summary>
    public partial class InitVaultWindow : Window
    {
        private const int MinLen = 8;
        private const int MaxLen = 50;

        private TextBox _password;
        private TextBox _confirm;
        private TextBlock _error;
        private Button _create;

        public InitVaultWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _password = this.FindControl<TextBox>("PasswordBox");
            _confirm = this.FindControl<TextBox>("ConfirmBox");
            _error = this.FindControl<TextBlock>("ErrorText");
            _create = this.FindControl<Button>("CreateButton");

            _password.PlaceholderText = I18n.I18n.T("init.password.placeholder", MinLen, MaxLen);
            _password.TextChanged += (_, _) => _create.IsEnabled = (_password.Text?.Length ?? 0) >= MinLen;
            _create.Click += (_, _) => Submit();
            this.FindControl<Button>("CancelButton").Click += (_, _) => Close(false);
        }

        private void Submit()
        {
            string pw = _password.Text ?? "";
            string cfm = _confirm.Text ?? "";
            if (pw.Length < MinLen)
            {
                _error.Text = I18n.I18n.T("init.error.too_short", MinLen);
                return;
            }
            if (pw != cfm)
            {
                _error.Text = I18n.I18n.T("init.error.mismatch");
                _confirm.Text = "";
                return;
            }
            byte[] dek = VaultManager.Current.InitVault(pw);
            InsertSampleData(dek);
            Close(true);
        }

        private static void InsertSampleData(byte[] key)
        {
            VaultManager vm = VaultManager.Current;
            vm.CreateItem(key, NewLogin("MyPassword", "example@puppylab.org", "my-password-for-test-1",
                ["https://mypassword.puppylab.org/login.html"], true));
            vm.CreateItem(key, NewLogin("MyPassword", "test@puppylab.org", "my-password-for-test-2",
                ["https://mypassword.puppylab.org/login.html"], false));
            vm.CreateItem(key, NewNote("Wi-Fi Password", "SSID: Home-5G\nPassword: 12345678"));
            vm.CreateItem(key, NewNote("Software License",
                "MyPassword is a free, open source desktop password manager.\n"
                + "Source: https://github.com/michaelliao/mypassword\nLicense: GPLv3"));
            vm.CreateItem(key, NewIdentity("Simpson", "ChunkyLover53@aol.com", "E-1234567890", "ID-1234567890",
                ["+1 123456789"], "742 Evergreen Terrace, Springfield", "58008",
                "an overweight, lazy, and often ignorant, yet deeply devoted man."));
        }

        private static LoginItemData NewLogin(string title, string username, string password,
            List<string> websites, bool fav)
            => new()
            {
                favorite = fav,
                data = new LoginFieldsData
                {
                    title = title, username = username, password = password, websites = websites,
                },
            };

        private static NoteItemData NewNote(string title, string content)
            => new() { data = new NoteFieldsData { title = title, content = content } };

        private static IdentityItemData NewIdentity(string name, string email, string passport, string idNumber,
            List<string> mobiles, string address, string zipCode, string memo)
            => new()
            {
                data = new IdentityFieldsData
                {
                    name = name, email = email, passport_number = passport, identity_number = idNumber,
                    mobiles = mobiles, address = address, zip_code = zipCode, memo = memo,
                },
            };
    }
}
