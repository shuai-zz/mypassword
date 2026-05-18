using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyPasswordDesktop.Resources
{
    /// <summary>English string resources — see <c>Strings.en.axaml</c>.</summary>
    public partial class StringsEn : ResourceDictionary
    {
        public StringsEn()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
