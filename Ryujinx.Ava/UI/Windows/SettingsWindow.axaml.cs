using Avalonia.Interactivity;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using System;
using System.ComponentModel;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class SettingsWindow : StyleableWindow
    {
        internal SettingsViewModel ViewModel { get; set; }

        public SettingsWindow(VirtualFileSystem virtualFileSystem, ContentManager contentManager)
        {
            Title = $"Ryujinx {Program.Version} - {LocaleManager.Instance["Settings"]}";

            ViewModel   = new SettingsViewModel(virtualFileSystem, contentManager);
            DataContext = ViewModel;

            ViewModel.CloseWindow += Close;
            ViewModel.SaveSettingsEvent += SaveSettings;

            InitializeComponent();
            Load();
        }

        public SettingsWindow()
        {
            ViewModel   = new SettingsViewModel();
            DataContext = ViewModel;

            InitializeComponent();
            Load();
        }

        public void SaveSettings()
        {
            InputPage.ControllerSettings?.SaveCurrentProfile();

            if (Owner is MainWindow window && ViewModel.DirectoryChanged)
            {
                window.ViewModel.LoadApplications();
            }
        }

        private void Load()
        {
            Pages.Children.Clear();
            NavPanel.SelectionChanged += NavPanelOnSelectionChanged;
            NavPanel.SelectedItem = NavPanel.MenuItems.ElementAt(0);
        }

        private void NavPanelOnSelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem navitem)
            {
                switch(navitem.Tag.ToString())
                {
                    case "UiPage":
                        var uiPage = UiPage;
                        uiPage.ViewModel = ViewModel;
                        NavPanel.Content = uiPage;
                        break;
                    case "InputPage":
                        NavPanel.Content = InputPage;
                        break;
                    case "HotkeysPage":
                        NavPanel.Content = HotkeysPage;
                        break;
                    case "SystemPage":
                        var systemPage = SystemPage;
                        systemPage.ViewModel = ViewModel;
                        NavPanel.Content = systemPage;
                        break;
                    case "CpuPage":
                        NavPanel.Content = CpuPage;
                        break;
                    case "GraphicsPage":
                        NavPanel.Content = GraphicsPage;
                        break;
                    case "AudioPage":
                        NavPanel.Content = AudioPage;
                        break;
                    case "NetworkPage":
                        NavPanel.Content = NetworkPage;
                        break;
                    case "LoggingPage":
                        NavPanel.Content = LoggingPage;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void OkButton(object sender, RoutedEventArgs e)
        {
            ViewModel.OkButton();
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelButton();
        }

        private void ApplyButton(object sender, RoutedEventArgs e)
        {
            ViewModel.ApplyButton();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            HotkeysPage.Dispose();
            InputPage.Dispose();
            base.OnClosing(e);
        }
    }
}