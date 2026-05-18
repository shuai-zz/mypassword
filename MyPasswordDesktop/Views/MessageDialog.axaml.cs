using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyPasswordDesktop.Views
{
    public partial class MessageDialog : Window
    {
        // Parameterless ctor for the XAML previewer / loader.
        public MessageDialog() : this("", "", true) { }

        public MessageDialog(string title, string message, bool confirm)
        {
            AvaloniaXamlLoader.Load(this);
            Title = title;
            this.FindControl<TextBlock>("MessageText").Text = message;

            var ok = this.FindControl<Button>("OkButton");
            var cancel = this.FindControl<Button>("CancelButton");
            ok.Click += (_, _) => Close(true);
            cancel.Click += (_, _) => Close(false);
            cancel.IsVisible = confirm;
        }
    }
}
