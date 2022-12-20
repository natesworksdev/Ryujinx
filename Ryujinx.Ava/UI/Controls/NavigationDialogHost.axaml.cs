using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Views.User;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.Controls
{
    public partial class NavigationDialogHost : UserControl
    {
        public AccountManager AccountManager { get; }
        public ContentManager ContentManager { get; }
        public VirtualFileSystem VirtualFileSystem { get; }
        public HorizonClient HorizonClient { get; }
        public UserProfileViewModel ViewModel { get; set; }

        public NavigationDialogHost()
        {
            InitializeComponent();
        }

        public NavigationDialogHost(AccountManager accountManager, ContentManager contentManager,
            VirtualFileSystem virtualFileSystem, HorizonClient horizonClient)
        {
            AccountManager = accountManager;
            ContentManager = contentManager;
            VirtualFileSystem = virtualFileSystem;
            HorizonClient = horizonClient;
            ViewModel = new UserProfileViewModel();
            LoadProfiles();


            if (contentManager.GetCurrentFirmwareVersion() != null)
            {
                Task.Run(() =>
                {
                    UserFirmwareAvatarSelectorViewModel.PreloadAvatars(contentManager, virtualFileSystem);
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

            LoadProfiles();
        }

        public void Navigate(Type sourcePageType, object parameter)
        {
            ContentFrame.Navigate(sourcePageType, parameter);
        }

        public static async Task Show(AccountManager ownerAccountManager, ContentManager ownerContentManager,
            VirtualFileSystem ownerVirtualFileSystem, HorizonClient ownerHorizonClient)
        {
            var content = new NavigationDialogHost(ownerAccountManager, ownerContentManager, ownerVirtualFileSystem, ownerHorizonClient);
            ContentDialog contentDialog = new ContentDialog
            {
                Title = LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle],
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance[LocaleKeys.UserProfilesClose],
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
        
        public void LoadProfiles()
        {
            ViewModel.Profiles.Clear();
            ViewModel.LostProfiles.Clear();

            var profiles = AccountManager.GetAllUsers().OrderByDescending(x => x.AccountState == AccountState.Open);

            foreach (var profile in profiles)
            {
                ViewModel.Profiles.Add(new UserProfile(profile, this));
            }

            ViewModel.SelectedProfile = ViewModel.Profiles.FirstOrDefault(x => x.UserId == AccountManager.LastOpenedUser.UserId);

            if (ViewModel.SelectedProfile == null)
            {
                ViewModel.SelectedProfile = ViewModel.Profiles.First();

                if (ViewModel.SelectedProfile != null)
                {
                    AccountManager.OpenUser(ViewModel.SelectedProfile.UserId);
                }
            }

            var saveDataFilter = SaveDataFilter.Make(programId: default, saveType: SaveDataType.Account,
                default, saveDataId: default, index: default);

            using var saveDataIterator = new UniqueRef<SaveDataIterator>();

            HorizonClient.Fs.OpenSaveDataIterator(ref saveDataIterator.Ref(), SaveDataSpaceId.User, in saveDataFilter).ThrowIfFailure();

            Span<SaveDataInfo> saveDataInfo = stackalloc SaveDataInfo[10];

            HashSet<HLE.HOS.Services.Account.Acc.UserId> lostAccounts = new();

            while (true)
            {
                saveDataIterator.Get.ReadSaveDataInfo(out long readCount, saveDataInfo).ThrowIfFailure();

                if (readCount == 0)
                {
                    break;
                }

                for (int i = 0; i < readCount; i++)
                {
                    var save = saveDataInfo[i];
                    var id = new HLE.HOS.Services.Account.Acc.UserId((long)save.UserId.Id.Low, (long)save.UserId.Id.High);
                    if (ViewModel.Profiles.FirstOrDefault( x=> x.UserId == id) == null)
                    {
                        lostAccounts.Add(id);
                    }
                }
            }

            foreach(var account in lostAccounts)
            {
                ViewModel.LostProfiles.Add(new UserProfile(new HLE.HOS.Services.Account.Acc.UserProfile(account, "", null), this));
            }
        }

        public async void DeleteUser()
        {
            if (ViewModel.HighlightedProfile != null)
            {
                var lastUserId = AccountManager.LastOpenedUser.UserId;

                if (ViewModel.HighlightedProfile.UserId == lastUserId)
                {
                    // If we are deleting the currently open profile, then we must open something else before deleting.
                    var profile = ViewModel.Profiles.FirstOrDefault(x => x.UserId != lastUserId);

                    if (profile == null)
                    {
                        async void Action()
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogUserProfileDeletionWarningMessage"]);
                        }

                        Dispatcher.UIThread.Post(Action);

                        return;
                    }

                    AccountManager.OpenUser(profile.UserId);
                }

                var result =
                    await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance["DialogUserProfileDeletionConfirmMessage"], "",
                        LocaleManager.Instance["InputDialogYes"], LocaleManager.Instance["InputDialogNo"], "");

                if (result == UserResult.Yes)
                {
                    AccountManager.DeleteUser(ViewModel.HighlightedProfile.UserId);
                }
            }

            LoadProfiles();
        }
        
        public void AddUser()
        {
            Navigate(typeof(UserEditor), (this, (UserProfile)null, true));
        }
        
        public void EditUser()
        {
            Navigate(typeof(UserEditor), (this, ViewModel.HighlightedProfile ?? ViewModel.SelectedProfile, false));
        }

        public void RecoverLostAccounts()
        {
            Navigate(typeof(UserRecoverer), (this, this));
        }
        
        public async void ManageSaves()
        {
            UserProfile userProfile = ViewModel.HighlightedProfile ?? ViewModel.SelectedProfile;

            SaveManager manager = new(userProfile, HorizonClient, VirtualFileSystem);
            
            ContentDialog contentDialog = new()
            {
                Title = string.Format(LocaleManager.Instance["SaveManagerHeading"], userProfile.Name),
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance["UserProfilesClose"],
                Content = manager,
                Padding = new Thickness(0)
            };

            await contentDialog.ShowAsync();
        }
    }
}