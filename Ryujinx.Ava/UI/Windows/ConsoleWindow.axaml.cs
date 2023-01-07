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

        private void InputElement_OnPointerEnter(object? sender, PointerEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                ListBoxItem lbi = (sender as TextBlock)?.Parent as ListBoxItem;
                ListBox lb = lbi.Parent as ListBox;
                lb.SelectedItems.Add((sender as TextBlock)?.DataContext);
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                ListBoxItem lbi = (sender as TextBlock)?.Parent as ListBoxItem;
                ListBox lb = lbi.Parent as ListBox;
                lb.SelectedItems.Clear();
                lb.SelectedItems.Add((sender as TextBlock)?.DataContext);
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        private void ConsoleListBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
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