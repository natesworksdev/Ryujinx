using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class RumbleSettingsWindow : UserControl
    {
        private readonly InputConfiguration _viewmodel;

        public RumbleSettingsWindow()
        {
            InitializeComponent();
            DataContext = _viewmodel;
        }

        public RumbleSettingsWindow(ControllerSettingsViewModel viewmodel)
        {
            _viewmodel = viewmodel.Configuration;

            InitializeComponent();
            DataContext = _viewmodel;
        }

        public static async Task Show(ControllerSettingsViewModel viewmodel)
        {
            RumbleSettingsWindow content = new RumbleSettingsWindow(viewmodel);

            ContentDialog contentDialog = new ContentDialog
            {
                Title = LocaleManager.Instance[LocaleKeys.ControllerRumbleTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsSave],
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsClose],
                Content = content,
            };

            contentDialog.PrimaryButtonClick += (sender, args) =>
            {
                viewmodel.Configuration.StrongRumble = content._viewmodel.StrongRumble;
                viewmodel.Configuration.WeakRumble = content._viewmodel.WeakRumble;
            };

            await contentDialog.ShowAsync();
        }
    }
}