using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Ui.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.Ui.Controls.Settings;

public partial class SettingsUIView : UserControl
{
    private SettingsWindow _parent;

    public SettingsUIView(SettingsWindow settingsWindow)
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private async void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        string path = PathBox.Text;

        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !_parent.ViewModel.GameDirectories.Contains(path))
        {
            _parent.ViewModel.GameDirectories.Add(path);
            _parent.ViewModel.DirectoryChanged = true;
        }
        else
        {
            var result = await _parent.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    if (!_parent.ViewModel.GameDirectories.Contains(uri.LocalPath))
                    {
                        _parent.ViewModel.GameDirectories.Add(uri.LocalPath);
                        _parent.ViewModel.DirectoryChanged = true;
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
            _parent.ViewModel.GameDirectories.Remove(path);
            _parent.ViewModel.DirectoryChanged = true;
        }

        if (GameList.ItemCount > 0)
        {
            GameList.SelectedIndex = oldIndex < GameList.ItemCount ? oldIndex : 0;
        }
    }
}