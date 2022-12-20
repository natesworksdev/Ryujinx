using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class UserFirmwareAvatarSelector : UserControl
    {
        private NavigationDialogHost _parent;
        private TempProfile _profile;

        public UserFirmwareAvatarSelector(ContentManager contentManager)
        {
            ContentManager = contentManager;

            DataContext = ViewModel;

            InitializeComponent();
        }

        public UserFirmwareAvatarSelector()
        {
            InitializeComponent();

            AddHandler(Frame.NavigatedToEvent, (s, e) =>
            {
                NavigatedTo(e);
            }, RoutingStrategies.Direct);
        }

        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (Program.PreviewerDetached)
            {
                if (arg.NavigationMode == NavigationMode.New)
                {
                    (_parent, _profile) = ((NavigationDialogHost, TempProfile))arg.Parameter;
                    ContentManager = _parent.ContentManager;
                    if (Program.PreviewerDetached)
                    {
                        ViewModel = new UserFirmwareAvatarSelectorViewModel(() => ViewModel.ReloadImages());
                    }

                    DataContext = ViewModel;
                }
            }
        }

        public ContentManager ContentManager { get; private set; }

        internal UserFirmwareAvatarSelectorViewModel ViewModel { get; set; }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Dispose();

            _parent.GoBack();
        }

        private void ChooseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedIndex > -1)
            {
                _profile.Image = ViewModel.SelectedImage;

                ViewModel.Dispose();

                _parent.GoBack();
            }
        }
    }
}