using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Ava.Ui.ViewModels;
using Ryujinx.Common.Configuration.Hid.Controller;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Windows
{
    public class CemuHookMotionSettingsWindow : UserControl
    {
        private readonly InputConfiguration<GamepadInputId, StickInputId> _viewmodel;

        public CemuHookMotionSettingsWindow()
        {
            InitializeComponent();
        }

        public CemuHookMotionSettingsWindow(ControllerSettingsViewModel viewmodel)
        {
            var config = viewmodel.Configuration as InputConfiguration<GamepadInputId, StickInputId>;

            _viewmodel = new InputConfiguration<GamepadInputId, StickInputId>()
            {
                Slot = config.Slot,
                AltSlot = config.AltSlot,
                DsuServerHost = config.DsuServerHost,
                DsuServerPort = config.DsuServerPort,
                MirrorInput = config.MirrorInput,
                EnableCemuHookMotion = config.EnableCemuHookMotion
            };
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            DataContext = _viewmodel;
            
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task Show(ControllerSettingsViewModel viewmodel, StyleableWindow window)
        {
            ContentDialog contentDialog = window.ContentDialog;

            string name = string.Empty;

            CemuHookMotionSettingsWindow content = new CemuHookMotionSettingsWindow(viewmodel);

            if (contentDialog != null)
            {
                contentDialog.Title = "CemuHook Input Source Settings";
                contentDialog.PrimaryButtonText = "Save";
                contentDialog.SecondaryButtonText = "";
                contentDialog.CloseButtonText = "Close";
                contentDialog.Content = content;
                contentDialog.PrimaryButtonClick += (sender, args) =>
                {
                    var config = viewmodel.Configuration as InputConfiguration<GamepadInputId, StickInputId>;
                    config.Slot = content._viewmodel.Slot;
                    config.AltSlot = content._viewmodel.AltSlot;
                    config.DsuServerHost = content._viewmodel.DsuServerHost;
                    config.DsuServerPort = content._viewmodel.DsuServerPort;
                    config.EnableCemuHookMotion = content._viewmodel.EnableCemuHookMotion;
                    config.MirrorInput = content._viewmodel.MirrorInput;
                };

                await contentDialog.ShowAsync();
            }
        }
    }
}