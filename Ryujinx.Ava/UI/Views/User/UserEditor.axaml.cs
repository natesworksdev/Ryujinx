using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserEditor : UserControl
    {
        private NavigationDialogHost _parent;
        private UserProfile _profile;
        private bool _isNewUser;

        public TempProfile TempProfile { get; set; }
        public uint MaxProfileNameLength => 0x20;
        public bool IsDeletable => _profile.UserId != AccountManager.DefaultUserId;

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
                switch (arg.NavigationMode)
                {
                    case NavigationMode.New:
                        var args = ((NavigationDialogHost parent, UserProfile profile, bool isNewUser))arg.Parameter;
                        _isNewUser = args.isNewUser;
                        _profile = args.profile;
                        TempProfile = new TempProfile(_profile);

                        _parent = args.parent;
                        ((ContentDialog)_parent.Parent).Title = $"{LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle]} - " +
                                                                $"{ (_isNewUser ? LocaleManager.Instance[LocaleKeys.UserEditorTitleCreate] : LocaleManager.Instance[LocaleKeys.UserEditorTitle])}";
                        break;
                }

                DataContext = TempProfile;

                AddPictureButton.IsVisible = _isNewUser;
                ChangePictureButton.IsVisible = !_isNewUser;
                IdLabel.IsVisible = _profile != null;
                IdText.IsVisible = _profile != null;
                if (!_isNewUser && IsDeletable)
                {
                    DeleteButton.IsVisible = true;
                }
                else
                {
                    DeleteButton.IsVisible = false;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _parent?.GoBack();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _parent.DeleteUser(_profile);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DataValidationErrors.ClearErrors(NameBox);

            if (string.IsNullOrWhiteSpace(TempProfile.Name))
            {
                DataValidationErrors.SetError(NameBox, new DataValidationException(LocaleManager.Instance[LocaleKeys.UserProfileEmptyNameError]));

                return;
            }

            if (TempProfile.Image == null)
            {
                _parent.Navigate(typeof(UserProfileImageSelector), (_parent, TempProfile));

                return;
            }

            if (_profile != null && !_isNewUser)
            {
                _profile.Name = TempProfile.Name;
                _profile.Image = TempProfile.Image;
                _profile.UpdateState();
                _parent.AccountManager.SetUserName(_profile.UserId, _profile.Name);
                _parent.AccountManager.SetUserImage(_profile.UserId, _profile.Image);
            }
            else if (_isNewUser)
            {
                _parent.AccountManager.AddUser(TempProfile.Name, TempProfile.Image, TempProfile.UserId);
            }
            else
            {
                return;
            }

            _parent?.GoBack();
        }

        public void SelectProfileImage()
        {
            _parent.Navigate(typeof(UserProfileImageSelector), (_parent, TempProfile));
        }

        private void ChangePictureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profile != null || _isNewUser)
            {
                SelectProfileImage();
            }
        }
    }
}