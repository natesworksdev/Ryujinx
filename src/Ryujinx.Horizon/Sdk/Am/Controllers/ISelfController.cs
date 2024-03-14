using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ISelfController : IServiceObject
    {
        Result Exit();
        Result LockExit();
        Result UnlockExit();
        Result EnterFatalSection();
        Result LeaveFatalSection();
        Result GetLibraryAppletLaunchableEvent(out int arg0);
        Result SetScreenShotPermission(int arg0);
        Result SetOperationModeChangedNotification(bool arg0);
        Result SetPerformanceModeChangedNotification(bool arg0);
        Result SetFocusHandlingMode();
        Result SetRestartMessageEnabled();
        Result SetScreenShotAppletIdentityInfo();
        Result SetOutOfFocusSuspendingEnabled();
        Result SetControllerFirmwareUpdateSection();
        Result SetRequiresCaptureButtonShortPressedMessage();
        Result SetAlbumImageOrientation();
        Result SetDesirableKeyboardLayout();
        Result GetScreenShotProgramId();
        Result CreateManagedDisplayLayer();
        Result IsSystemBufferSharingEnabled();
        Result GetSystemSharedLayerHandle();
        Result GetSystemSharedBufferHandle();
        Result CreateManagedDisplaySeparableLayer();
        Result SetManagedDisplayLayerSeparationMode();
        Result SetRecordingLayerCompositionEnabled();
        Result SetHandlesRequestToDisplay();
        Result ApproveToDisplay();
        Result OverrideAutoSleepTimeAndDimmingTime();
        Result SetMediaPlaybackState();
        Result SetIdleTimeDetectionExtension();
        Result GetIdleTimeDetectionExtension();
        Result SetInputDetectionSourceSet();
        Result ReportUserIsActive();
        Result GetCurrentIlluminance();
        Result IsIlluminanceAvailable();
        Result SetAutoSleepDisabled();
        Result IsAutoSleepDisabled();
        Result ReportMultimediaError();
        Result GetCurrentIlluminanceEx();
        Result SetInputDetectionPolicy();
        Result SetWirelessPriorityMode();
        Result GetAccumulatedSuspendedTickValue();
        Result GetAccumulatedSuspendedTickChangedEvent();
        Result SetAlbumImageTakenNotificationEnabled();
        Result SetApplicationAlbumUserData();
        Result SaveCurrentScreenshot();
        Result SetRecordVolumeMuted();
        Result GetDebugStorageChannel();
    }
}
