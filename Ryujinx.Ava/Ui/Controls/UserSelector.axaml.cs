using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    public class UserSelector : UserControl
    {
        public AccountManager AccountManager { get; }
        public ContentManager ContentManager { get; }

        public UserProfileViewModel ViewModel { get; set; }
        
        public UserSelector()
        {
            InitializeComponent();
        }
        
        public UserSelector(AccountManager accountManager, ContentManager contentManager,
            VirtualFileSystem virtualFileSystem)
        {
            AccountManager = accountManager;
            ContentManager = contentManager;
            ViewModel = new UserProfileViewModel(this);

            DataContext = ViewModel;

            InitializeComponent();
            
            if (contentManager.GetCurrentFirmwareVersion() != null)
            {
                Task.Run(() =>
                {
                    AvatarProfileViewModel.PreloadAvatars(contentManager, virtualFileSystem);
                });
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}