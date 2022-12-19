using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.ViewModels;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.Views
{
    public partial class UserSelector : UserControl
    {
        private NavigationDialogHost _parent;
        public UserProfileViewModel ViewModel { get; set; }

        public UserSelector()
        {
            InitializeComponent();

            if (Program.PreviewerDetached)
            {
                AddHandler(Frame.NavigatedToEvent, (s, e) =>
                {
                    NavigatedTo(e);
                }, RoutingStrategies.Direct);
            }
        }

        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (Program.PreviewerDetached)
            {
                if (arg.NavigationMode == NavigationMode.New)
                {
                    _parent = (NavigationDialogHost)arg.Parameter;
                    ViewModel = _parent.ViewModel;
                }

                DataContext = ViewModel;
            }
        }

        private void ProfilesList_DoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                int selectedIndex = listBox.SelectedIndex;

                if (selectedIndex >= 0 && selectedIndex < ViewModel.Profiles.Count)
                {
                    ViewModel.SelectedProfile = ViewModel.Profiles[selectedIndex];

                    _parent?.AccountManager?.OpenUser(ViewModel.SelectedProfile.UserId);

                    _parent.LoadProfiles();

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

        private void AddUser(object sender, RoutedEventArgs e)
        {
            _parent.AddUser();
        }

        private void EditUser(object sender, RoutedEventArgs e)
        {
            _parent.EditUser();
        }

        private void ManageSaves(object sender, RoutedEventArgs e)
        {
            _parent.ManageSaves();
        }

        private void RecoverLostAccounts(object sender, RoutedEventArgs e)
        {
            _parent.RecoverLostAccounts();
        }

        private void DeleteUser(object sender, RoutedEventArgs e)
        {
            _parent.DeleteUser();
        }
    }
}