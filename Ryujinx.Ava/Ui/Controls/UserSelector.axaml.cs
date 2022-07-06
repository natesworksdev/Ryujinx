using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Ui.Controls
{
    public class UserSelector : UserControl
    {
        public AccountManager AccountManager { get; }
        public ContentManager ContentManager { get; }

        public UserProfileViewModel ViewModel { get; set; }
        
        public UserSelector()
        {
            InitializeComponent();
        }
        
        public UserSelector(AccountManager accountManager, ContentManager contentManager,
            VirtualFileSystem virtualFileSystem)
        {
            AccountManager = accountManager;
            ContentManager = contentManager;
            ViewModel = new UserProfileViewModel(this);

            DataContext = ViewModel;

            InitializeComponent();
            
            if (contentManager.GetCurrentFirmwareVersion() != null)
            {
                Task.Run(() =>
                {
                    AvatarProfileViewModel.PreloadAvatars(contentManager, virtualFileSystem);
                });
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task Show(AccountManager ownerAccountManager, ContentManager ownerContentManager, VirtualFileSystem ownerVirtualFileSystem)
        {
            var content = new UserSelector(ownerAccountManager, ownerContentManager, ownerVirtualFileSystem);
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
        
        private void ProfilesList_DoubleTapped(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                int selectedIndex = listBox.SelectedIndex;

                if (selectedIndex >= 0 && selectedIndex < ViewModel.Profiles.Count)
                {
                    ViewModel.SelectedProfile = ViewModel.Profiles[selectedIndex];

                    AccountManager.OpenUser(ViewModel.SelectedProfile.UserId);

                    ViewModel.LoadProfiles();

                    foreach (UserProfile profile in ViewModel.Profiles)
                    {
                        profile.UpdateState();
                    }
                }
            }
        }

        private void SelectingItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                int selectedIndex = listBox.SelectedIndex;

                if (selectedIndex >= 0 && selectedIndex < ViewModel.Profiles.Count)
                {
                    ViewModel.HighlightedProfile = ViewModel.Profiles[selectedIndex];
                }
            }
        }
    }
}