using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
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

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.IsActive = false;
            base.OnClosed(e);
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
    }
}