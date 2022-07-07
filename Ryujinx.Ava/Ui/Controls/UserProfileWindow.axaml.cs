using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    public partial class UserProfileWindow : UserControl
    {
        public AccountManager AccountManager { get; }
        public ContentManager ContentManager { get; }
        public UserProfileViewModel ViewModel { get; set; }
        
        public UserProfileWindow()
        {
            InitializeComponent();
        }
        
        public UserProfileWindow(AccountManager accountManager, ContentManager contentManager,
            VirtualFileSystem virtualFileSystem)
        {
            AccountManager = accountManager;
            ContentManager = contentManager;
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

        public void GoBack()
        {
            if (ContentFrame.BackStack.Count > 0)
            {
                ContentFrame.GoBack();
            }
            
            ViewModel.LoadProfiles();
        }
        

        public static async Task Show(AccountManager ownerAccountManager, ContentManager ownerContentManager, VirtualFileSystem ownerVirtualFileSystem)
        {
            var content = new UserProfileWindow(ownerAccountManager, ownerContentManager, ownerVirtualFileSystem);
            ContentDialog contentDialog = new ContentDialog
            {
                Title = LocaleManager.Instance["UserProfileWindowTitle"],
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance["UserProfilesClose"],
                Content = content,
                Padding = new Thickness(2, 2)
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

            ContentFrame.Navigate(typeof(UserSelector), (this, ViewModel));
        }
    }
}