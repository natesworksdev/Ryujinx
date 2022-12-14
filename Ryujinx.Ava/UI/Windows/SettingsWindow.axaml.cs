using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using System;

namespace Ryujinx.Ava.Ui.Windows
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
                NavPanel.Content = navitem.Tag.ToString() switch
                {
                    "UiPage"       => UiPage,
                    "InputPage"    => InputPage,
                    "HotkeysPage"  => HotkeysPage,
                    "SystemPage"   => SystemPage,
                    "CpuPage"      => CpuPage,
                    "GraphicsPage" => GraphicsPage,
                    "AudioPage"    => AudioPage,
                    "NetworkPage"  => NetworkPage,
                    "LoggingPage"  => LoggingPage,
                    _              => throw new NotImplementedException()
                };
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // ControllerSettings.Dispose();

            base.OnClosed(e);
        }
    }
}