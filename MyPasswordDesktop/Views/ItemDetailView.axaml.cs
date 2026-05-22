using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MyPasswordDesktop.Views
{
    public partial class ItemDetailView : UserControl
    {
        public ItemDetailView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Opens the clicked website in the OS default browser. URLs without a
        // scheme are normalized to https:// (e.g. "xyz.com" → "https://xyz.com").
        private void OpenWebsite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not string raw)
            {
                return;
            }
            string url = (raw ?? "").Trim();
            if (url.Length == 0)
            {
                return;
            }
            if (!url.Contains("://"))
            {
                url = "https://" + url;
            }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch
            {
                // ignore — invalid URL or no browser configured
            }
        }
    }
}
