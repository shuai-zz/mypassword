using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MyPasswordDesktop.Views;

namespace MyPasswordDesktop.Services
{
    /// <summary>Modal confirm/info dialog helpers.</summary>
    public static class Dialogs
    {
        private static Window Owner
            => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        public static async Task<bool> ConfirmAsync(string title, string message)
        {
            Window owner = Owner;
            var dlg = new MessageDialog(title, message, confirm: true);
            return owner != null ? await dlg.ShowDialog<bool>(owner) : false;
        }

        public static async Task InfoAsync(string title, string message)
        {
            Window owner = Owner;
            var dlg = new MessageDialog(title, message, confirm: false);
            if (owner != null)
            {
                await dlg.ShowDialog<bool>(owner);
            }
        }
    }
}
