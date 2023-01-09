using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.UI.Windows;
using System;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Helpers
{
    public static class ContentDialogOverlay
    {
        public static async Task<ContentDialogResult> ShowAsync(ContentDialog contentDialog)
        {
            ContentDialogResult result;

            ContentDialogOverlayWindow contentDialogOverlayWindow = null;

            Window parent = GetMainWindow();

            if (DialogNeedsOverlay())
            {
                contentDialogOverlayWindow = new()
                {
                    Height = parent.Bounds.Height,
                    Width = parent.Bounds.Width,
                    Position = parent.PointToScreen(new Point()),
                    ShowInTaskbar = false
                };

                parent.PositionChanged += OverlayOnPositionChanged;

                void OverlayOnPositionChanged(object sender, PixelPointEventArgs e)
                {
                    contentDialogOverlayWindow.Position = parent.PointToScreen(new Point());
                }

                contentDialogOverlayWindow.ContentDialog = contentDialog;

                bool opened = false;

                contentDialogOverlayWindow.Opened += OverlayOnActivated;

                async void OverlayOnActivated(object sender, EventArgs e)
                {
                    if (opened)
                    {
                        return;
                    }

                    opened = true;

                    contentDialogOverlayWindow.Position = parent.PointToScreen(new Point());

                    result = await ShowDialog();
                }

                result = await contentDialogOverlayWindow.ShowDialog<ContentDialogResult>(parent);
            }
            else
            {
                result = await ShowDialog();
            }

            async Task<ContentDialogResult> ShowDialog()
            {
                if (contentDialogOverlayWindow is not null)
                {
                    result = await contentDialog.ShowAsync(contentDialogOverlayWindow);

                    contentDialogOverlayWindow!.Close();
                }
                else
                {
                    result = await contentDialog.ShowAsync();
                }

                return result;
            }

            if (contentDialogOverlayWindow is not null)
            {
                contentDialogOverlayWindow.Content = null;
                contentDialogOverlayWindow.Close();
            }

            return result;
        }

        private static bool DialogNeedsOverlay()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime al)
            {
                foreach (Window item in al.Windows)
                {
                    if (item.IsActive && item is MainWindow window && window.ViewModel.IsGameRunning)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Window GetMainWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime al)
            {
                foreach (Window item in al.Windows)
                {
                    if (item.IsActive && item is MainWindow window)
                    {
                        return window;
                    }
                }
            }

            return null;
        }
    }
}