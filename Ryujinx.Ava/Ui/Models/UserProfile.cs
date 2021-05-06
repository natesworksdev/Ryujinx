using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.HOS.Services.Account.Acc;

namespace Ryujinx.Ava.Ui.Models
{
    public class UserProfile : BaseModel
    {
        private readonly HLE.HOS.Services.Account.Acc.UserProfile _profile;
        private byte[] _image;
        private string _name;
        private UserId _userId;


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
        
        public UserProfile(HLE.HOS.Services.Account.Acc.UserProfile profile)
        {
            _profile = profile;

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
    }
}