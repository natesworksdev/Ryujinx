using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface ISelfController
    {
        Result Exit();
        Result LockExit();
        Result UnlockExit();
        Result EnterFatalSection();
        Result LeaveFatalSection();
        Result GetLibraryAppletLaunchableEvent();
        Result SetScreenShotPermission();
        Result SetOperationModeChangedNotification();
        Result SetPerformanceModeChangedNotification();
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
