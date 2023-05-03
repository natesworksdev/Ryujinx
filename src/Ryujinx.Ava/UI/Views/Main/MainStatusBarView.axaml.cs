using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Ui.Common.Configuration;
using System;

namespace Ryujinx.Ava.UI.Views.Main
{
    public partial class MainStatusBarView : UserControl
    {
        public MainWindow Window;

        public MainStatusBarView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is MainWindow window)
            {
                Window = window;
            }

            DataContext = Window.ViewModel;
        }

        private void SpeedState_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var emulationContext = Window.ViewModel.AppHost.Device;
            var currentState = emulationContext.SpeedState;

            if (currentState.HasFlag(SpeedState.Turbo))
            {
                emulationContext.SetSpeedState(SpeedState.Normal);
            }
            else if (currentState.HasFlag(SpeedState.FastForward))
            {
                emulationContext.SetSpeedState(SpeedState.Turbo);
            }
            else
            {
                emulationContext.SetSpeedState(SpeedState.FastForward);
            }

            Logger.Info?.Print(LogClass.Application, $"Speed State set to: {emulationContext.GetSpeedStateStatus()}");
        }

        private void DockedStatus_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            ConfigurationState.Instance.System.EnableDockedMode.Value = !ConfigurationState.Instance.System.EnableDockedMode.Value;
        }

        private void AspectRatioStatus_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            AspectRatio aspectRatio = ConfigurationState.Instance.Graphics.AspectRatio.Value;

            ConfigurationState.Instance.Graphics.AspectRatio.Value = (int)aspectRatio + 1 > Enum.GetNames(typeof(AspectRatio)).Length - 1 ? AspectRatio.Fixed4x3 : aspectRatio + 1;
        }

        private void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            Window.LoadApplications();
        }
    }
}
