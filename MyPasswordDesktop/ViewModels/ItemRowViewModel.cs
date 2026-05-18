using Avalonia.Media;
using MyPasswordDesktop.Core.Data;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>One row in the item list — display wrapper around an item.</summary>
    public sealed class ItemRowViewModel : ViewModelBase
    {
        public AbstractItemData Item { get; }

        public ItemRowViewModel(AbstractItemData item) => Item = item;

        public long Id => Item.id;

        public string Title => Item.Title;

        public string Subtitle => Item.Subtitle;

        public string IconLetter => Item.item_type switch
        {
            ItemType.LOGIN => "L",
            ItemType.NOTE => "N",
            ItemType.IDENTITY => "I",
            _ => "?",
        };

        public IBrush IconBrush => Item.item_type switch
        {
            ItemType.LOGIN => new SolidColorBrush(Color.FromRgb(70, 130, 180)),
            ItemType.NOTE => new SolidColorBrush(Color.FromRgb(230, 150, 50)),
            ItemType.IDENTITY => new SolidColorBrush(Color.FromRgb(72, 175, 110)),
            _ => new SolidColorBrush(Color.FromRgb(70, 130, 180)),
        };
    }
}
