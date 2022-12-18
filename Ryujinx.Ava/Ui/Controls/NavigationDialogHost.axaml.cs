using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LibHac;
using LibHac.FsSystem;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    public partial class NavigationDialogHost : UserControl
    {
        public AccountManager AccountManager { get; }
        public ContentManager ContentManager { get; }
        public VirtualFileSystem VirtualFileSystem { get; }
        public HorizonClient HorizonClient { get; }
        public List<ApplicationData> Applications { get; }
        public UserProfileViewModel ViewModel { get; set; }

        public NavigationDialogHost()
        {
            InitializeComponent();
            
            
        }

        public NavigationDialogHost(AccountManager accountManager, ContentManager contentManager,
            VirtualFileSystem virtualFileSystem, HorizonClient horizonClient, List<ApplicationData> applications)
        {
            AccountManager = accountManager;
            ContentManager = contentManager;
            VirtualFileSystem = virtualFileSystem;
            HorizonClient = horizonClient;
            Applications = applications;
            ViewModel = new UserProfileViewModel(this);


            if (contentManager.GetCurrentFirmwareVersion() != null)
            {
                Task.Run(() =>
                {
                    AvatarProfileViewModel.PreloadAvatars(contentManager, virtualFileSystem);
                });
            }
            InitializeComponent();
        }

        public void GoBack(object parameter = null)
        {
            if (ContentFrame.BackStack.Count > 0)
            {
                ContentFrame.GoBack();
            }

            ViewModel.LoadProfiles();
        }

        public void Navigate(Type sourcePageType, object parameter)
        {
            ContentFrame.Navigate(sourcePageType, parameter);
        }

        public static async Task Show(AccountManager ownerAccountManager, ContentManager ownerContentManager,
            VirtualFileSystem ownerVirtualFileSystem, HorizonClient ownerHorizonClient, List<ApplicationData> applications)
        {
            var content = new NavigationDialogHost(ownerAccountManager, ownerContentManager, ownerVirtualFileSystem, ownerHorizonClient, applications);
            ContentDialog contentDialog = new ContentDialog
            {
                Title = LocaleManager.Instance["UserProfileWindowTitle"],
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance["UserProfilesClose"],
                Content = content,
                Padding = new Thickness(0)
            };

            contentDialog.Closed += (sender, args) =>
            {
                content.ViewModel.Dispose();
            };

            await contentDialog.ShowAsync();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            Navigate(typeof(UserSelector), this);
        }
    }
}