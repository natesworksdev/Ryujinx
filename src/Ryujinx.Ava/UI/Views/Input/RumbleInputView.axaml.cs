using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class RumbleInputView : UserControl
    {
        private RumbleInputViewModel ViewModel;

        public RumbleInputView()
        {
            InitializeComponent();
        }

        public RumbleInputView(ControllerInputViewModel viewmodel)
        {
            var config = viewmodel.Configuration as InputConfiguration<GamepadInputId, StickInputId>;

            ViewModel = new RumbleInputViewModel
            {
                StrongRumble = config.StrongRumble,
                WeakRumble = config.WeakRumble
            };

            InitializeComponent();
            DataContext = ViewModel;
        }

        public static async Task Show(ControllerInputViewModel viewmodel)
        {
            RumbleInputView content = new RumbleInputView(viewmodel);

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
                var config = viewmodel.Configuration as InputConfiguration<GamepadInputId, StickInputId>;
                config.StrongRumble = content.ViewModel.StrongRumble;
                config.WeakRumble = content.ViewModel.WeakRumble;
            };

            await contentDialog.ShowAsync();
        }
    }
}