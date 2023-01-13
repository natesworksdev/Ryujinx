using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ui.Common.Configuration;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class ConsoleWindow : StyleableWindow
    {
        private static ConsoleWindowViewModel ConsoleWindowViewModel { get; set; }

        public ConsoleWindow()
        {
            ConsoleWindowViewModel = new ConsoleWindowViewModel(this);

            DataContext = ConsoleWindowViewModel;

            InitializeComponent();

            Title = $"Ryujinx Console {Program.Version}";

            ConsoleScrollViewer.ScrollChanged += ConsoleScrollViewerOnScrollChanged;
            AutoScrollCheckBox.Checked += delegate { MaybeScrollToBottom(); };
        }

        private void ConsoleScrollViewerOnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentDelta != Vector.Zero)
            {
                MaybeScrollToBottom();
            }
        }

        private void MaybeScrollToBottom()
        {
            if (AutoScrollCheckBox.IsChecked == true)
            {
                Dispatcher.UIThread.Post(delegate
                {
                    ConsoleScrollViewer.ScrollToEnd();
                });
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