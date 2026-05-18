using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MyPasswordDesktop.I18n;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Views
{
    /// <summary>
    /// Shown when the vault file is missing — lets the user open an existing
    /// vault or pick a folder for a new one, writing the pointer file.
    /// </summary>
    public partial class VaultLocatorWindow : Window
    {
        private const string VaultFileName = "mypassword.db";

        private TextBlock _errorText;

        public VaultLocatorWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _errorText = this.FindControl<TextBlock>("ErrorText");
            this.FindControl<Button>("OpenButton").Click += async (_, _) => await OnOpenAsync();
            this.FindControl<Button>("CreateButton").Click += async (_, _) => await OnCreateAsync();
        }

        private async Task OnOpenAsync()
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = I18n.I18n.T("locate.open.title"),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18n.I18n.T("locate.file.filter")) { Patterns = ["*.db"] },
                    FilePickerFileTypes.All,
                ],
            });
            if (files.Count == 0)
            {
                return;
            }
            string path = files[0].TryGetLocalPath();
            if (path == null || !File.Exists(path))
            {
                _errorText.Text = I18n.I18n.T("locate.error.not_a_file");
                return;
            }
            try
            {
                FileUtils.SetVaultLocation(path);
                Close(true);
            }
            catch (IOException ex)
            {
                _errorText.Text = I18n.I18n.T("locate.error.pointer", ex.Message);
            }
        }

        private async Task OnCreateAsync()
        {
            IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = I18n.I18n.T("locate.create.title") });
            if (folders.Count == 0)
            {
                return;
            }
            string folder = folders[0].TryGetLocalPath();
            if (folder == null)
            {
                _errorText.Text = I18n.I18n.T("locate.error.not_a_file");
                return;
            }
            string target = Path.Combine(folder, VaultFileName);
            if (File.Exists(target))
            {
                _errorText.Text = I18n.I18n.T("locate.error.already_exists");
                return;
            }
            try
            {
                FileUtils.SetVaultLocation(target);
                Close(true);
            }
            catch (IOException ex)
            {
                _errorText.Text = I18n.I18n.T("locate.error.pointer", ex.Message);
            }
        }
    }
}
