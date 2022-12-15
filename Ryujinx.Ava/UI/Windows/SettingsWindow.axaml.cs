using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
<<<<<<< HEAD:Ryujinx.Ava/UI/Windows/SettingsWindow.axaml.cs
using Ryujinx.Ava.Ui.ViewModels;
=======
using Ryujinx.Ava.UI.ViewModels;
>>>>>>> 66aac324 (Fix Namespace Case):Ryujinx.Ava/Ui/Windows/SettingsWindow.axaml.cs
using Ryujinx.HLE.FileSystem;
using System;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class SettingsWindow : StyleableWindow
    {
        internal SettingsViewModel ViewModel { get; set; }

        public SettingsWindow(VirtualFileSystem virtualFileSystem, ContentManager contentManager)
        {
            Title = $"Ryujinx {Program.Version} - {LocaleManager.Instance["Settings"]}";

            ViewModel   = new SettingsViewModel(virtualFileSystem, contentManager, this);
            DataContext = ViewModel;

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
    }
}