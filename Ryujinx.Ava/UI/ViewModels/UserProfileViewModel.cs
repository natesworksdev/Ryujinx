using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Collections.ObjectModel;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class UserProfileViewModel : BaseModel, IDisposable
    {
        private UserProfile _selectedProfile;
        private UserProfile _highlightedProfile;

        public UserProfileViewModel()
        {
            Profiles = new ObservableCollection<UserProfile>();
            LostProfiles = new ObservableCollection<UserProfile>();
        }

        public ObservableCollection<UserProfile> Profiles { get; set; }

        public ObservableCollection<UserProfile> LostProfiles { get; set; }

        public UserProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHighlightedProfileDeletable));
                OnPropertyChanged(nameof(IsHighlightedProfileEditable));
            }
        }

        public bool IsHighlightedProfileEditable => _highlightedProfile != null;

        public bool IsHighlightedProfileDeletable => _highlightedProfile != null && _highlightedProfile.UserId != AccountManager.DefaultUserId;

        public UserProfile HighlightedProfile
        {
            get => _highlightedProfile;
            set
            {
                _highlightedProfile = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHighlightedProfileDeletable));
                OnPropertyChanged(nameof(IsHighlightedProfileEditable));
            }
        }

        public void Dispose() { }
    }
}