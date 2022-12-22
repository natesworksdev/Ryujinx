using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Views.User;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Profile = Ryujinx.HLE.HOS.Services.Account.Acc.UserProfile;

namespace Ryujinx.Ava.UI.Models
{
    public class UserProfile : BaseModel
    {
        private readonly Profile _profile;
        private readonly NavigationDialogHost _owner;
        private byte[] _image;
        private string _name;
        private UserId _userId;
        private bool _isPointerOver;
        public uint MaxProfileNameLength => 0x20;

        public byte[] Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        public UserId UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public bool IsPointerOver
        {
            get => _isPointerOver;
            set
            {
                _isPointerOver = value;
                OnPropertyChanged();
            }
        }

    public UserProfile(Profile profile, NavigationDialogHost owner)
        {
            _profile = profile;
            _owner = owner;

            Image = profile.Image;
            Name = profile.Name;
            UserId = profile.UserId;
        }

        public bool IsOpened => _profile.AccountState == AccountState.Open;

        public void UpdateState()
        {
            OnPropertyChanged(nameof(IsOpened));
            OnPropertyChanged(nameof(Name));
        }

        public void Recover(UserProfile userProfile)
        {
            _owner.Navigate(typeof(UserEditor), (_owner, userProfile, true));
        }
    }
}