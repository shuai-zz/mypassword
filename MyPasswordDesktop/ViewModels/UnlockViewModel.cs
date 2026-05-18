using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.I18n;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>The vault-unlock screen.</summary>
    public sealed partial class UnlockViewModel : ViewModelBase
    {
        private const int MinPasswordLen = 8;

        private readonly Action _onUnlocked;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UnlockCommand))]
        private string _password = "";

        [ObservableProperty]
        private string _errorMessage = "";

        public ObservableCollection<OAuthRowViewModel> OAuthRows { get; } = new();

        public bool HasOAuth => OAuthRows.Count > 0;

        public UnlockViewModel(Action onUnlocked)
        {
            _onUnlocked = onUnlocked;
            RefreshOAuth();
        }

        public void RefreshOAuth()
        {
            OAuthRows.Clear();
            foreach (RecoveryConfig rc in VaultManager.Current.GetRecoveryConfigs())
            {
                if (!string.IsNullOrEmpty(rc.b64_uid_hash))
                {
                    OAuthRows.Add(new OAuthRowViewModel(rc));
                }
            }
            OnPropertyChanged(nameof(HasOAuth));
        }

        private bool CanUnlock() => (Password?.Length ?? 0) >= MinPasswordLen;

        [RelayCommand(CanExecute = nameof(CanUnlock))]
        private void Unlock()
        {
            byte[] dek = VaultManager.Current.UnlockVault(Password.ToCharArray());
            if (dek == null)
            {
                ErrorMessage = I18n.I18n.T("unlock.error.wrong_password");
                Password = "";
                return;
            }
            Session.Current.SetKey(UnlockType.PASSWORD, dek);
            ErrorMessage = "";
            Password = "";
            _onUnlocked?.Invoke();
        }
    }
}
