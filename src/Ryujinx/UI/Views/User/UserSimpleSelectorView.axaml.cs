using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Linq;
using Button = Avalonia.Controls.Button;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserSimpleSelectorView : UserControl
    {

        public ProfilesViewModel ViewModel { get; set; }

        private readonly AccountManager _accountManager;

        public UserSimpleSelectorView(AccountManager accountManager)
        {
            _accountManager = accountManager;
            ViewModel = new ProfilesViewModel();
            var profiles = _accountManager
                .GetAllUsers()
                .Select(p => new Models.UserProfile(p, null))
                .OrderBy(p => p.Name);

            ViewModel.Profiles.AddRange(profiles);
            InitializeComponent();
        }



        private void Grid_PointerEntered(object sender, PointerEventArgs e)
        {
            if (sender is Grid grid)
            {
                if (grid.DataContext is UserProfile profile)
                {
                    profile.IsPointerOver = true;
                }
            }
        }

        private void Grid_OnPointerExited(object sender, PointerEventArgs e)
        {
            if (sender is Grid grid)
            {
                if (grid.DataContext is UserProfile profile)
                {
                    profile.IsPointerOver = false;
                }
            }
        }

        private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                int selectedIndex = listBox.SelectedIndex;

                if (selectedIndex >= 0 && selectedIndex < ViewModel.Profiles.Count)
                {
                    if (ViewModel.Profiles[selectedIndex] is UserProfile userProfile)
                    {
                        _accountManager?.OpenUser(userProfile.UserId);

                        foreach (BaseModel profile in ViewModel.Profiles)
                        {
                            if (profile is UserProfile uProfile)
                            {
                                uProfile.UpdateState();
                            }
                        }
                    }
                }
            }
        }
    }
}
