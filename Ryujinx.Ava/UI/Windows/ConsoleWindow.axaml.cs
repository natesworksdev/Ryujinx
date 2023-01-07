using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Logging;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ui.Common.Configuration;
using SixLabors.ImageSharp;
using System;
using System.ComponentModel;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class ConsoleWindow : StyleableWindow
    {
        internal static ConsoleWindowViewModel ConsoleWindowViewModel { get; private set; }

        public ConsoleWindow()
        {
            ConsoleWindowViewModel = new ConsoleWindowViewModel(this);

            DataContext = ConsoleWindowViewModel;

            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
            ConfigurationState.Instance.Ui.ShowConsole.Value = false;

            base.OnClosing(e);
        }

        private void ConsoleListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                if (listBox.Selection.Count > 0)
                {
                    string text = ((InMemoryLogTarget.Entry)listBox.SelectedItem).Text;
                    Application.Current.Clipboard.SetTextAsync(text).Wait();
                    listBox.Selection.Clear();
                }
            }
        }
    }
}