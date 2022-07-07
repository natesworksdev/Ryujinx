using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Ui.Controls
{
    public partial class UserSelector : UserControl
    {
        private UserProfileWindow _parent;
        public UserProfileViewModel ViewModel { get; set; }
        
        public UserSelector()
        {
            InitializeComponent();

            if (Program.PreviewerDetached)
            {
                AddHandler(Frame.NavigatedToEvent, (s, e) =>
                {
                    NavigatedTo(e);
                }, Avalonia.Interactivity.RoutingStrategies.Direct);
            }
        }
        
        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (Program.PreviewerDetached)
            {
                var args = ((UserProfileWindow parent, UserProfileViewModel viewModel)) arg.Parameter;
                _parent = args.parent;
                ViewModel = args.viewModel;

                DataContext = ViewModel;
            }
        }
        
        private void ProfilesList_DoubleTapped(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                int selectedIndex = listBox.SelectedIndex;

                if (selectedIndex >= 0 && selectedIndex < ViewModel.Profiles.Count)
                {
                    ViewModel.SelectedProfile = ViewModel.Profiles[selectedIndex];

                    _parent?.AccountManager?.OpenUser(ViewModel.SelectedProfile.UserId);

                    ViewModel.LoadProfiles();

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
    }
}