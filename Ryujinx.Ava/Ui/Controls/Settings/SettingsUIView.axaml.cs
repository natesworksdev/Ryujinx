using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Ui.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.Ui.Controls.Settings;

public partial class SettingsUIView : UserControl
{
    private SettingsViewModel _viewModel;

    public SettingsUIView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _viewModel = DataContext as SettingsViewModel;
    }
    
    private async void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        string path = PathBox.Text;

        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !_viewModel.GameDirectories.Contains(path))
        {
            _viewModel.GameDirectories.Add(path);
            _viewModel.DirectoryChanged = true;
        }
        else
        {
            /*var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    if (!_viewModel.GameDirectories.Contains(uri.LocalPath))
                    {
                        _viewModel.GameDirectories.Add(uri.LocalPath);
                        _viewModel.DirectoryChanged = true;
                    }
                }
            }*/
        }
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        int oldIndex = GameList.SelectedIndex;

        foreach (string path in new List<string>(GameList.SelectedItems.Cast<string>()))
        {
            _viewModel.GameDirectories.Remove(path);
            _viewModel.DirectoryChanged = true;
        }

        if (GameList.ItemCount > 0)
        {
            GameList.SelectedIndex = oldIndex < GameList.ItemCount ? oldIndex : 0;
        }
    }
}