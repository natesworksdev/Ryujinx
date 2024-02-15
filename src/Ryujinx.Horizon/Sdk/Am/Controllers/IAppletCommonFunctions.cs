using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IAppletCommonFunctions : IServiceObject
    {
        Result SetTerminateResult();
        Result ReadThemeStorage();
        Result WriteThemeStorage();
        Result PushToAppletBoundChannel();
        Result TryPopFromAppletBoundChannel();
        Result GetDisplayLogicalResolution();
        Result SetDisplayMagnification();
        Result SetHomeButtonDoubleClickEnabled();
        Result GetHomeButtonDoubleClickEnabled();
        Result IsHomeButtonShortPressedBlocked();
        Result IsVrModeCurtainRequired();
        Result IsSleepRequiredByHighTemperature();
        Result IsSleepRequiredByLowBattery();
        Result SetCpuBoostRequestPriority();
        Result SetHandlingCaptureButtonShortPressedMessageEnabledForApplet();
        Result SetHandlingCaptureButtonLongPressedMessageEnabledForApplet();
        Result OpenNamedChannelAsParent();
        Result OpenNamedChannelAsChild();
        Result SetApplicationCoreUsageMode();
        // 300 (17.0.0+) Unknown Function
    }
}
