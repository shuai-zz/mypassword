using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.I18n;
using MyPasswordDesktop.Models;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Services;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>
    /// The unlocked main screen — toolbar, category navigation, item list and
    /// the right-hand detail/edit pane. Replaces the SWT MVC <c>MainController</c>
    /// + <c>AppState</c>.
    /// </summary>
    public sealed partial class MainContentViewModel : ViewModelBase
    {
        private readonly Action _onLock;
        private readonly Action _onShowSettings;

        private readonly List<AbstractItemData> _allItems = new();
        private readonly List<AbstractItemData> _deletedItems = new();
        private AbstractItemData _selectedItem;

        public ObservableCollection<CategoryItem> Categories { get; } = new();
        public ObservableCollection<ItemRowViewModel> Items { get; } = new();

        [ObservableProperty]
        private CategoryItem _selectedCategory;

        [ObservableProperty]
        private ItemRowViewModel _selectedItemRow;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        private object _rightContent;

        /// <summary>True while the right pane shows the edit form. The toolbar
        /// search box, Add-New button, category list, and item list bind their
        /// <c>IsEnabled</c> against this so they are locked during editing.</summary>
        public bool IsEditing => RightContent is ItemEditViewModel;

        private bool _suppressSelection;

        public MainContentViewModel(Action onLock, Action onShowSettings)
        {
            _onLock = onLock;
            _onShowSettings = onShowSettings;

            Categories.Add(new CategoryItem(Category.ALL, I18n.I18n.T("category.all")));
            Categories.Add(new CategoryItem(Category.FAVORITES, I18n.I18n.T("category.favorites")));
            Categories.Add(new CategoryItem(Category.LOGINS, I18n.I18n.T("category.logins")));
            Categories.Add(new CategoryItem(Category.NOTES, I18n.I18n.T("category.notes")));
            Categories.Add(new CategoryItem(Category.IDENTITIES, I18n.I18n.T("category.identities")));
            Categories.Add(new CategoryItem(Category.TRASH, I18n.I18n.T("category.trash")));
            _selectedCategory = Categories[0];

            // extension created/updated an item (fired from an HTTP thread)
            VaultManager.Current.SetOnItemsChanged(() => Dispatcher.UIThread.Post(() =>
            {
                LoadItems();
                if (RightContent is ItemDetailViewModel)
                {
                    ShowDetail();
                }
            }));

            RightContent = new EmptyViewModel();
            LoadItems();
        }

        // ── reactive hooks ──────────────────────────────────────────────────

        partial void OnSelectedCategoryChanged(CategoryItem value)
        {
            _selectedItem = null;
            RefreshList();
            RightContent = new EmptyViewModel();
        }

        partial void OnSearchTextChanged(string value) => RefreshList();

        partial void OnSelectedItemRowChanged(ItemRowViewModel value)
        {
            if (_suppressSelection)
            {
                return;
            }
            _selectedItem = value?.Item;
            ShowDetail();
        }

        // ── data loading ────────────────────────────────────────────────────

        private void LoadItems()
        {
            byte[] key = Session.Current.GetKey();
            if (key == null)
            {
                return;
            }
            _allItems.Clear();
            _deletedItems.Clear();
            foreach (AbstractItemData item in VaultManager.Current.GetItems(key))
            {
                if (item.deleted)
                {
                    _deletedItems.Add(item);
                }
                else
                {
                    _allItems.Add(item);
                }
            }
            _allItems.Sort();
            _deletedItems.Sort();
            if (_selectedItem != null)
            {
                _selectedItem = FindItem(_selectedItem.id);
            }
            RefreshList();
        }

        private AbstractItemData FindItem(long id)
            => _allItems.FirstOrDefault(i => i.id == id) ?? _deletedItems.FirstOrDefault(i => i.id == id);

        private void RefreshList()
        {
            IEnumerable<AbstractItemData> combined = _allItems.Concat(_deletedItems);
            string q = (SearchText ?? "").Trim().ToLowerInvariant();
            if (q.Length > 0)
            {
                combined = combined.Where(i => Contains(i.Title, q) || Contains(i.Subtitle, q));
            }
            Category cat = SelectedCategory?.Category ?? Category.ALL;
            IEnumerable<AbstractItemData> filtered = cat switch
            {
                Category.ALL => combined.Where(i => !i.deleted),
                Category.FAVORITES => combined.Where(i => !i.deleted && i.favorite),
                Category.LOGINS => combined.Where(i => !i.deleted && i.item_type == ItemType.LOGIN),
                Category.NOTES => combined.Where(i => !i.deleted && i.item_type == ItemType.NOTE),
                Category.IDENTITIES => combined.Where(i => !i.deleted && i.item_type == ItemType.IDENTITY),
                Category.TRASH => combined.Where(i => i.deleted),
                _ => combined,
            };

            _suppressSelection = true;
            Items.Clear();
            foreach (AbstractItemData item in filtered)
            {
                Items.Add(new ItemRowViewModel(item));
            }
            if (_selectedItem != null)
            {
                SelectedItemRow = Items.FirstOrDefault(r => r.Id == _selectedItem.id);
            }
            else
            {
                SelectedItemRow = null;
            }
            _suppressSelection = false;
        }

        private static bool Contains(string text, string q)
            => text != null && text.ToLowerInvariant().Contains(q);

        // ── right pane ──────────────────────────────────────────────────────

        private void ShowDetail()
        {
            (RightContent as IDisposable)?.Dispose();
            if (_selectedItem == null)
            {
                RightContent = new EmptyViewModel();
                return;
            }
            RightContent = new ItemDetailViewModel(_selectedItem, OnEditCurrent, OnDeleteCurrent, OnRestoreCurrent);
        }

        private void OnEditCurrent()
        {
            if (_selectedItem == null)
            {
                return;
            }
            RightContent = new ItemEditViewModel(_selectedItem, _selectedItem.item_type, OnSave, OnCancel);
        }

        private async void OnDeleteCurrent()
        {
            if (_selectedItem == null)
            {
                return;
            }
            bool ok = await Dialogs.ConfirmAsync(
                I18n.I18n.T("confirm.title"), I18n.I18n.T("confirm.delete", _selectedItem.Title));
            if (!ok)
            {
                return;
            }
            byte[] key = Session.Current.GetKey();
            if (key == null)
            {
                return;
            }
            VaultManager.Current.DeleteItem(key, _selectedItem.id);
            _selectedItem = null;
            LoadItems();
            RightContent = new EmptyViewModel();
        }

        private void OnRestoreCurrent()
        {
            if (_selectedItem == null)
            {
                return;
            }
            byte[] key = Session.Current.GetKey();
            if (key == null)
            {
                return;
            }
            VaultManager.Current.RestoreItem(key, _selectedItem.id);
            _selectedItem = null;
            LoadItems();
            RightContent = new EmptyViewModel();
        }

        private async void OnSave(AbstractItemData data)
        {
            byte[] key = Session.Current.GetKey();
            if (key == null)
            {
                return;
            }
            AbstractItemData saved;
            try
            {
                saved = data.id == 0
                    ? VaultManager.Current.CreateItem(key, data)
                    : VaultManager.Current.UpdateItem(key, data);
            }
            catch (VaultException e)
            {
                await Dialogs.InfoAsync(I18n.I18n.T("confirm.title"), e.Message);
                return;
            }
            _selectedItem = null;
            LoadItems();
            _selectedItem = FindItem(saved.id);
            RefreshList();
            ShowDetail();
        }

        private void OnCancel() => ShowDetail();

        // ── toolbar commands ────────────────────────────────────────────────

        private void AddNew(int type)
        {
            _suppressSelection = true;
            SelectedItemRow = null;
            _suppressSelection = false;
            _selectedItem = null;
            RightContent = new ItemEditViewModel(null, type, OnSave, OnCancel);
        }

        [RelayCommand]
        private void AddLogin() => AddNew(ItemType.LOGIN);

        [RelayCommand]
        private void AddNote() => AddNew(ItemType.NOTE);

        [RelayCommand]
        private void AddIdentity() => AddNew(ItemType.IDENTITY);

        [RelayCommand]
        private void Lock() => _onLock?.Invoke();

        [RelayCommand]
        private void Settings() => _onShowSettings?.Invoke();
    }
}
