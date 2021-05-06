using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.FileSystem.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.Ui.Windows
{
    public class SettingsWindow : StyleableWindow
    {
        private TabControl              _tabs;
        private ListBox                 _gameList;
        private TextBox                 _pathBox;
        private AutoCompleteBox          _timeZoneBox;
        private ControllerSettingsWindow _controllerSettings;

        public SettingsViewModel ViewModel { get; set; }

        public SettingsWindow(VirtualFileSystem virtualFileSystem, ContentManager contentManager)
        {
            Title = $"Ryujinx {Program.Version} - {LocaleManager.Instance["Settings"]}";

            ViewModel   = new SettingsViewModel(virtualFileSystem, contentManager, this);
            DataContext = ViewModel;

            InitializeComponent();
            AttachDebugDevTools();

            FuncMultiValueConverter<string, string> converter = new(parts => string.Format("{0}  {1}   {2}", parts.ToArray()));
            MultiBinding tzMultiBinding = new() { Converter = converter };
            tzMultiBinding.Bindings.Add(new Binding("UtcDifference"));
            tzMultiBinding.Bindings.Add(new Binding("Location"));
            tzMultiBinding.Bindings.Add(new Binding("Abbreviation"));

            _timeZoneBox.ValueMemberBinding = tzMultiBinding;
        }

        public SettingsWindow()
        {
            ViewModel   = new SettingsViewModel();
            DataContext = ViewModel;

            InitializeComponent();
            AttachDebugDevTools();
        }

        [Conditional("DEBUG")]
        private void AttachDebugDevTools()
        {
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _tabs        = this.FindControl<TabControl>("Tabs");
            _pathBox     = this.FindControl<TextBox>("PathBox");
            _gameList    = this.FindControl<ListBox>("GameList");
            _timeZoneBox = this.FindControl<AutoCompleteBox>("TimeZoneBox");
            _controllerSettings = this.FindControl<ControllerSettingsWindow>("ControllerSettings");

            _tabs.SelectionChanged += Tabs_SelectionChanged;
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Remove hardcoded size maybe?

            if (_tabs.SelectedIndex == 4)
            {
                if (Width == 800)
                {
                    Width    = 1100;
                    Position = new PixelPoint(Position.X - 150, Position.Y);
                }

                if (Height == 650)
                {
                    Height   = 780;
                    Position = new PixelPoint(Position.X, Position.Y - 65);
                }
            }
            else
            {
                if (Width == 1100)
                {
                    Width    = 800;
                    Position = new PixelPoint(Position.X + 150, Position.Y);
                }

                if (Height == 780)
                {
                    Height   = 650;
                    Position = new PixelPoint(Position.X, Position.Y + 65);
                }
            }
        }

        private async void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            string path = _pathBox.Text;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !ViewModel.GameDirectories.Contains(path))
            {
                ViewModel.GameDirectories.Add(path);
            }
            else
            {
                path = await new OpenFolderDialog().ShowAsync(this);

                if (!string.IsNullOrWhiteSpace(path))
                {
                    ViewModel.GameDirectories.Add(path);
                }
            }
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            List<string> selected = new(_gameList.SelectedItems.Cast<string>());

            foreach (string path in selected)
            {
                ViewModel.GameDirectories.Remove(path);
            }
        }

        private void TimeZoneBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is TimeZone timeZone)
                {
                    e.Handled = true;

                    ViewModel.ValidateAndSetTimeZone(timeZone.Location);
                }
            }
        }

        private void TimeZoneBox_OnTextChanged(object sender, EventArgs e)
        {
            if (sender is AutoCompleteBox box)
            {
                if (box.SelectedItem != null && box.SelectedItem is TimeZone timeZone)
                {
                    ViewModel.ValidateAndSetTimeZone(timeZone.Location);
                }
            }
        }

        private void SaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            Close();
        }

        private void CloseButton_Clicked(object sender, RoutedEventArgs e)
        {
            ViewModel.RevertIfNotSaved();
            Close();
        }

        private void ApplyButton_Clicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            ViewModel.SaveSettings();

            _controllerSettings?.SaveCurrentProfile();

            if (Owner is MainWindow window)
            {
                window.ViewModel.LoadApplications();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _controllerSettings.Dispose();
            base.OnClosed(e);
        }
    }
}