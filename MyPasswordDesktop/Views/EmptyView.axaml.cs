using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyPasswordDesktop.Views
{
    public partial class EmptyView : UserControl
    {
        public EmptyView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
