using MyPasswordDesktop.Models;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>A selectable left-hand navigation category.</summary>
    public sealed class CategoryItem
    {
        public Category Category { get; }
        public string Name { get; }

        public CategoryItem(Category category, string name)
        {
            Category = category;
            Name = name;
        }
    }

    /// <summary>Placeholder shown in the right pane when nothing is selected.</summary>
    public sealed class EmptyViewModel : ViewModelBase
    {
        public string Hint => I18n.I18n.T("empty.hint");
    }
}
