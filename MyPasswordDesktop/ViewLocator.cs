#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyPasswordDesktop.ViewModels;
using MyPasswordDesktop.Views;

namespace MyPasswordDesktop
{
    /// <summary>
    /// Maps a view model to its view. Uses an explicit switch (no reflection /
    /// <c>Activator.CreateInstance</c>) so it is trim- and Native-AOT-safe.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param) => param switch
        {
            UnlockViewModel => new UnlockView(),
            MainContentViewModel => new MainContentView(),
            ItemDetailViewModel => new ItemDetailView(),
            ItemEditViewModel => new ItemEditView(),
            EmptyViewModel => new EmptyView(),
            null => null,
            _ => new TextBlock { Text = "Not Found: " + param.GetType().FullName },
        };

        public bool Match(object? data) => data is ViewModelBase;
    }
}
