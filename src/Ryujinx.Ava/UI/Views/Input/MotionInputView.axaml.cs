using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class MotionInputView : UserControl
    {
        private MotionInputViewModel ViewModel;

        public MotionInputView()
        {
            InitializeComponent();
        }

        public MotionInputView(ControllerInputViewModel viewmodel)
        {
            var config = viewmodel.Configuration as InputConfiguration<GamepadInputId, StickInputId>;

            ViewModel = new MotionInputViewModel
            {
                Slot = config.Slot,
                AltSlot = config.AltSlot,
                DsuServerHost = config.DsuServerHost,
                DsuServerPort = config.DsuServerPort,
                MirrorInput = config.MirrorInput,
                EnableMotion = config.EnableMotion,
                Sensitivity = config.Sensitivity,
                GyroDeadzone = config.GyroDeadzone,
                EnableCemuHookMotion = config.EnableCemuHookMotion
            };

            InitializeComponent();
            DataContext = ViewModel;
        }

        public static async Task Show(ControllerInputViewModel viewmodel)
        {
            MotionInputView content = new MotionInputView(viewmodel);

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
                var config = viewmodel.Configuration as InputConfiguration<GamepadInputId, StickInputId>;
                config.Slot = content.ViewModel.Slot;
                config.EnableMotion = content.ViewModel.EnableMotion;
                config.Sensitivity = content.ViewModel.Sensitivity;
                config.GyroDeadzone = content.ViewModel.GyroDeadzone;
                config.AltSlot = content.ViewModel.AltSlot;
                config.DsuServerHost = content.ViewModel.DsuServerHost;
                config.DsuServerPort = content.ViewModel.DsuServerPort;
                config.EnableCemuHookMotion = content.ViewModel.EnableCemuHookMotion;
                config.MirrorInput = content.ViewModel.MirrorInput;
            };

            await contentDialog.ShowAsync();
        }
    }
}