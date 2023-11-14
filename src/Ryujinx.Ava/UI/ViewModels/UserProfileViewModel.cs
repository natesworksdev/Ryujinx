using Microsoft.IdentityModel.Tokens;
using Ryujinx.Ava.UI.Models;
using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class UserProfileViewModel : BaseModel, IDisposable
    {
        public UserProfileViewModel()
        {
            Profiles = [];
            LostProfiles = [];
            IsEmpty = LostProfiles.IsNullOrEmpty();
        }

        public ObservableCollection<BaseModel> Profiles { get; set; }

        public ObservableCollection<UserProfile> LostProfiles { get; set; }

        public bool IsEmpty { get; set; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
