using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyPasswordDesktop.Views
{
    public partial class MainContentView : UserControl
    {
        public MainContentView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
