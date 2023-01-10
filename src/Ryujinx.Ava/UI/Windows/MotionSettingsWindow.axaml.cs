using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class MotionSettingsWindow : UserControl
    {
        private readonly InputConfiguration _viewmodel;

        public MotionSettingsWindow()
        {
            InitializeComponent();
            DataContext = _viewmodel;
        }

        public MotionSettingsWindow(ControllerSettingsViewModel viewmodel)
        {
            _viewmodel = viewmodel.Configuration;

            InitializeComponent();
            DataContext = _viewmodel;
        }

        public static async Task Show(ControllerSettingsViewModel viewmodel)
        {
            MotionSettingsWindow content = new MotionSettingsWindow(viewmodel);

            ContentDialog contentDialog = new ContentDialog
            {
                Title = LocaleManager.Instance[LocaleKeys.ControllerMotionTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsSave],
                SecondaryButtonText = "",
                CloseButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsClose],
                Content = content
            };
            contentDialog.PrimaryButtonClick += (sender, args) =>
            {
                viewmodel.Configuration.Slot = content._viewmodel.Slot;
                viewmodel.Configuration.EnableMotion = content._viewmodel.EnableMotion;
                viewmodel.Configuration.Sensitivity = content._viewmodel.Sensitivity;
                viewmodel.Configuration.GyroDeadzone = content._viewmodel.GyroDeadzone;
                viewmodel.Configuration.AltSlot = content._viewmodel.AltSlot;
                viewmodel.Configuration.DsuServerHost = content._viewmodel.DsuServerHost;
                viewmodel.Configuration.DsuServerPort = content._viewmodel.DsuServerPort;
                viewmodel.Configuration.EnableCemuHookMotion = content._viewmodel.EnableCemuHookMotion;
                viewmodel.Configuration.MirrorInput = content._viewmodel.MirrorInput;
            };

            await contentDialog.ShowAsync();
        }
    }
}