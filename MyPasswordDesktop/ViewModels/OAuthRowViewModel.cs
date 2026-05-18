using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>One OAuth recovery provider row (used on the unlock screen).</summary>
    public sealed partial class OAuthRowViewModel : ViewModelBase
    {
        public string Provider { get; }
        public string DisplayProvider { get; }
        public string UserInfo { get; }

        public OAuthRowViewModel(RecoveryConfig rc)
        {
            Provider = rc.oauth_provider;
            DisplayProvider = char.ToUpper(Provider[0]) + Provider.Substring(1);
            string name = string.IsNullOrEmpty(rc.oauth_name) ? "" : rc.oauth_name;
            string email = string.IsNullOrEmpty(rc.oauth_email) ? "" : rc.oauth_email;
            if (name.Length > 0 && email.Length > 0)
            {
                UserInfo = $"{name} <{email}>";
            }
            else
            {
                UserInfo = email.Length > 0 ? email : name;
            }
        }

        [RelayCommand]
        private void Login()
        {
            ShellUtils.OpenUrl($"http://127.0.0.1:{HttpDaemon.Port}/oauth/{Provider}/start?recover=true");
        }
    }
}
