using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace MyPasswordDesktop.Views
{
    public partial class UnlockView : UserControl
    {
        public UnlockView()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (_, _) =>
                Dispatcher.UIThread.Post(() => this.FindControl<TextBox>("PasswordBox")?.Focus());
        }
    }
}
