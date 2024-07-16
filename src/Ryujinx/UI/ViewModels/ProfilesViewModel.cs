using Avalonia.Collections;
using System;
using System.Collections.Generic;

using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class ProfilesViewModel : BaseModel
    {

        private UserProfile _selectedProfile;

        public event Action CloseWindow;
        public event Action ApplyProfile;

        public ProfilesViewModel()
        {
            Profiles = new AvaloniaList<UserProfile>();
            Profiles.Clear();
        }

        public ProfilesViewModel(IEnumerable<UserProfile> profiles)
        {
            Profiles = new AvaloniaList<UserProfile>();
            Profiles.Clear();
            Profiles.AddRange(profiles);
        }

        public AvaloniaList<UserProfile> Profiles
        {
            get; set;
        }

        public UserProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                OnPropertyChanged();
            }
        }

        public void Close()
        {
            CloseWindow?.Invoke();
        }
    }
}
