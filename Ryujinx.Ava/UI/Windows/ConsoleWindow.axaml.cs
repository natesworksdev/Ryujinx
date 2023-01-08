using Avalonia.Interactivity;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ui.Common.Configuration;
using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class ConsoleWindow : StyleableWindow
    {
        private int changeCount = 0;
        private int autoScrollChangeCount = 0;

        private static ConsoleWindowViewModel ConsoleWindowViewModel { get; set; }

        public ConsoleWindow()
        {
            ConsoleWindowViewModel = new ConsoleWindowViewModel(this);

            DataContext = ConsoleWindowViewModel;

            InitializeComponent();

            Title = $"Ryujinx Console {Program.Version}";

            ConsoleWindowViewModel.LogEntries.CollectionChanged += LogEntriesOnCollectionChanged;
            AutoScrollCheckBox.Checked += AutoScrollCheckBoxOnChecked;
            ConsoleItemsControl.LayoutUpdated += ConsoleItemsControlOnLayoutUpdated;
        }

        private void LogEntriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            changeCount++;
        }

        private void AutoScrollCheckBoxOnChecked(object sender, RoutedEventArgs e)
        {
            if (AutoScrollCheckBox.IsChecked == true)
            {
                ConsoleScrollViewer.ScrollToEnd();
            }
        }

        private void ConsoleItemsControlOnLayoutUpdated(object sender, EventArgs e)
        {
            if (AutoScrollCheckBox.IsChecked == true && autoScrollChangeCount != changeCount)
            {
                autoScrollChangeCount = changeCount;
                ConsoleScrollViewer.ScrollToEnd();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
            ConfigurationState.Instance.Ui.ShowConsole.Value = false;

            base.OnClosing(e);
        }
    }
}