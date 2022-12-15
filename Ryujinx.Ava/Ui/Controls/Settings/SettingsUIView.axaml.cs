using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Ryujinx.Ava.Ui.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.Ui.Controls.Settings;

public partial class SettingsUIView : UserControl
{
    public SettingsViewModel ViewModel;
    
    public SettingsUIView()
    {
        InitializeComponent();
    }
    
    private async void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        string path = PathBox.Text;

        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !ViewModel.GameDirectories.Contains(path))
        {
            ViewModel.GameDirectories.Add(path);
            ViewModel.DirectoryChanged = true;
        }
        else
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    AllowMultiple = false
                });

                if (result.Count > 0)
                {
                    if (result[0].TryGetUri(out Uri uri))
                    {
                        if (!ViewModel.GameDirectories.Contains(uri.LocalPath))
                        {
                            ViewModel.GameDirectories.Add(uri.LocalPath);
                            ViewModel.DirectoryChanged = true;
                        }
                    }
                }
            }
        }
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        int oldIndex = GameList.SelectedIndex;

        foreach (string path in new List<string>(GameList.SelectedItems.Cast<string>()))
        {
            ViewModel.GameDirectories.Remove(path);
            ViewModel.DirectoryChanged = true;
        }

        if (GameList.ItemCount > 0)
        {
            GameList.SelectedIndex = oldIndex < GameList.ItemCount ? oldIndex : 0;
        }
    }
}