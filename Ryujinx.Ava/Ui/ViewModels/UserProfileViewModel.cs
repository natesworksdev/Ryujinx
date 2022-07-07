using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Media.Animation;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Ui.ViewModels
{
    public class UserProfileViewModel : BaseModel, IDisposable
    {
        private const uint MaxProfileNameLength = 0x20;

        private readonly UserProfileWindow _owner;

        private UserProfile _selectedProfile;
        private UserProfile _highlightedProfile;
        private string _tempUserName;

        public UserProfileViewModel()
        {
            Profiles = new ObservableCollection<UserProfile>();
        }

        public UserProfileViewModel(UserProfileWindow owner) : this()
        {
            _owner = owner;

            LoadProfiles();
        }

        public ObservableCollection<UserProfile> Profiles { get; set; }

        public UserProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;

                OnPropertyChanged(nameof(SelectedProfile));
                OnPropertyChanged(nameof(IsHighlightedProfileDeletable));
                OnPropertyChanged(nameof(IsHighlightedProfileEditable));
            }
        }

        public bool IsHighlightedProfileEditable =>
            _highlightedProfile != null;

        public bool IsHighlightedProfileDeletable =>
            _highlightedProfile != null && _highlightedProfile.UserId != AccountManager.DefaultUserId;

        public UserProfile HighlightedProfile
        {
            get => _highlightedProfile;
            set
            {
                _highlightedProfile = value;

                OnPropertyChanged(nameof(HighlightedProfile));
                OnPropertyChanged(nameof(IsHighlightedProfileDeletable));
                OnPropertyChanged(nameof(IsHighlightedProfileEditable));
            }
        }

        public void Dispose()
        {
        }

        public void LoadProfiles()
        {
            Profiles.Clear();

            var profiles = _owner.AccountManager.GetAllUsers()
                .OrderByDescending(x => x.AccountState == AccountState.Open);

            foreach (var profile in profiles)
            {
                Profiles.Add(new UserProfile(profile));
            }

            SelectedProfile = Profiles.FirstOrDefault(x => x.UserId == _owner.AccountManager.LastOpenedUser.UserId);

            if (SelectedProfile == null)
            {
                SelectedProfile = Profiles.First();

                if (SelectedProfile != null)
                {
                    _owner.AccountManager.OpenUser(_selectedProfile.UserId);
                }
            }
        }

        public void AddUser()
        {
            UserProfile userProfile = null;
            _owner.ContentFrame.Navigate(typeof(UserEditor), (this._owner, userProfile, true), new SlideNavigationTransitionInfo());
        }

        public void EditUser()
        {
            _owner.ContentFrame.Navigate(typeof(UserEditor), (this._owner, _highlightedProfile, false), new SlideNavigationTransitionInfo());
        }

        public async void DeleteUser()
        {
            if (_highlightedProfile != null)
            {
                var lastUserId = _owner.AccountManager.LastOpenedUser.UserId;

                if (_highlightedProfile.UserId == lastUserId)
                {
                    // If we are deleting the currently open profile, then we must open something else before deleting.
                    var profile = Profiles.FirstOrDefault(x => x.UserId != lastUserId);

                    if (profile == null)
                    {
                        ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogUserProfileDeletionWarningMessage"]);
                        return;
                    }

                    _owner.AccountManager.OpenUser(profile.UserId);
                }

                var result =
                    await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance["DialogUserProfileDeletionConfirmMessage"], "",
                        LocaleManager.Instance["InputDialogYes"], LocaleManager.Instance["InputDialogNo"], "");

                if (result == UserResult.Yes)
                {
                    _owner.AccountManager.DeleteUser(_highlightedProfile.UserId);
                }
            }

            LoadProfiles();
        }
    }
}