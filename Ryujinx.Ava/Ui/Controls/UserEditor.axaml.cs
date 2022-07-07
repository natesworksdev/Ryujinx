using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Ui.Controls
{
    public partial class UserEditor : UserControl
    {
        private UserProfileWindow _parent;
        private UserProfile _profile;
        private bool _isNewUser;
        private byte[] _image;

        public TempProfile TempProfile { get; set; }

        public UserEditor()
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
                var args = ((UserProfileWindow parent, UserProfile profile, bool isNewUser)) arg.Parameter;
                _isNewUser = args.isNewUser;
                if (!_isNewUser)
                {
                    _profile = args.profile;
                    TempProfile = new TempProfile(_profile);
                }
                else
                {
                    TempProfile = new TempProfile();
                }

                _parent = args.parent;

                DataContext = TempProfile;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _parent?.GoBack();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TempProfile.Name) || TempProfile.Image == null)
            {
                return;
            }
            if (_profile != null)
            {
                _profile.Name = TempProfile.Name;
                _profile.Image = TempProfile.Image;
                _profile.UpdateState();
                _parent.AccountManager.SetUserName(_profile.UserId, _profile.Name);
                _parent.AccountManager.SetUserImage(_profile.UserId, _profile.Image);
            }
            else if (_isNewUser)
            {
                _parent.AccountManager.AddUser(TempProfile.Name, TempProfile.Image);
            }
            else
            {
                return;
            }

            _parent?.GoBack();
        }
        
        public async Task SelectProfileImage()
        {
            ProfileImageSelectionDialog selectionDialog = new(_parent.ContentManager);

            await selectionDialog.ShowDialog(_parent.GetVisualRoot() as Window);

            if (selectionDialog.BufferImageProfile != null)
            {
                TempProfile.Image = selectionDialog.BufferImageProfile;
            }
        }

        private async void ChangePictureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profile != null || _isNewUser)
            {
                await SelectProfileImage();
            }
        }
    }
}