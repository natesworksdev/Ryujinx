using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Collections.ObjectModel;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class UserProfileViewModel : BaseModel, IDisposable
    {
        private UserProfile _selectedProfile;

        public UserProfileViewModel()
        {
            Profiles = new ObservableCollection<BaseModel>();
            LostProfiles = new ObservableCollection<UserProfile>();
        }

        public ObservableCollection<BaseModel> Profiles { get; set; }

        public ObservableCollection<UserProfile> LostProfiles { get; set; }

        public UserProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;

                OnPropertyChanged();
            }
        }

        public void Dispose() { }
    }
}