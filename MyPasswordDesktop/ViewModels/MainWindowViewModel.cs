using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.I18n;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>
    /// Root view model. Switches the window between the unlock screen and the
    /// unlocked main content, and routes vault lock/unlock events.
    /// </summary>
    public sealed partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object _content;

        /// <summary>Raised when the user asks to open the settings dialog.</summary>
        public event Action ShowSettingsRequested;

        public string Title => I18n.I18n.T("app.name");

        /// <summary>
        /// Wire vault callbacks. Called once after the vault has been opened.
        /// </summary>
        public void WireCallbacks()
        {
            VaultManager.Current.SetOnVaultUnlocked(() =>
                Dispatcher.UIThread.Post(() =>
                {
                    if (Content is not MainContentViewModel)
                    {
                        ShowMain();
                    }
                }));

            Session.Current.SetOnAutoLocked(() =>
                Dispatcher.UIThread.Post(() =>
                {
                    if (Content is MainContentViewModel)
                    {
                        ShowUnlock();
                    }
                }));
        }

        public void ShowUnlock()
        {
            Content = new UnlockViewModel(ShowMain);
        }

        public void ShowMain()
        {
            Content = new MainContentViewModel(LockVault, () => ShowSettingsRequested?.Invoke());
        }

        private void LockVault()
        {
            Session.Current.Lock();
            ShowUnlock();
        }
    }
}
