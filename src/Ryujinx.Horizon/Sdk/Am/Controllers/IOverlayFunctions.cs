using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    public interface IOverlayFunctions
    {
        Result BeginToWatchShortHomeButtonMessage();
        Result EndToWatchShortHomeButtonMessage();
        Result GetApplicationIdForLogo();
        Result SetGpuTimeSliceBoost();
        Result SetAutoSleepTimeAndDimmingTimeEnabled();
        Result TerminateApplicationAndSetReason();
        Result SetScreenShotPermissionGlobally();
        Result StartShutdownSequenceForOverlay();
        Result StartRebootSequenceForOverlay();
        Result SetHandlingHomeButtonShortPressedEnabled();
        Result SetHandlingTouchScreenInputEnabled();
        Result SetHealthWarningShowingState();
        Result IsHealthWarningRequired();
        Result SetRequiresGpuResourceUse();
        Result BeginToObserveHidInputForDevelop();
    }
}
