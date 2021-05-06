using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaColorPicker;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;

namespace Ryujinx.Ava.Ui.Windows
{
    public class AvatarWindow : StyleableWindow
    {
        public AvatarWindow(ContentManager contentManager)
        {
            ContentManager = contentManager;
            ViewModel = new AvatarProfileViewModel();

            DataContext = ViewModel;

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            Title = $"Ryujinx {Program.Version} - Manage Accounts - Avatar";
        }

        public AvatarWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (Program.PreviewerDetached)
            {
                Title = $"Ryujinx {Program.Version} - Manage Accounts - Avatar";
            }
        }
        
        public ContentManager ContentManager { get; }

        public byte[] SelectedImage { get; set; }

        public AvatarProfileViewModel ViewModel { get; set; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChooseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedIndex > -1)
            {
                SelectedImage = ViewModel.SelectedImage;

                Close();
            }
        }

        private async void ColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new()
            {
                Color = Colors.White,
                Title = "Choose Background Color"
            };

            Color? res = await dialog.ShowDialog(this);

            if (res != default)
            {
                Color backgroundColor =
                    new Color(Byte.MaxValue, dialog.Color.R, dialog.Color.G, dialog.Color.B);

                ViewModel.BackgroundColor = backgroundColor;

                ViewModel.ReloadImages();
            }
        }
    }
}