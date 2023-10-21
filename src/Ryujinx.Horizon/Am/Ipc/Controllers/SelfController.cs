using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class SelfController : ISelfController
    {
        [CmifCommand(0)]
        public Result Exit()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1)]
        public Result LockExit()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(2)]
        public Result UnlockExit()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(3)]
        public Result EnterFatalSection()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(4)]
        public Result LeaveFatalSection()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(9)]
        public Result GetLibraryAppletLaunchableEvent()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(10)]
        public Result SetScreenShotPermission()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(11)]
        public Result SetOperationModeChangedNotification()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(12)]
        public Result SetPerformanceModeChangedNotification()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(13)]
        public Result SetFocusHandlingMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(14)]
        public Result SetRestartMessageEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(15)]
        public Result SetScreenShotAppletIdentityInfo()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(16)]
        public Result SetOutOfFocusSuspendingEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(17)]
        public Result SetControllerFirmwareUpdateSection()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(18)]
        public Result SetRequiresCaptureButtonShortPressedMessage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(19)]
        public Result SetAlbumImageOrientation()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(20)]
        public Result SetDesirableKeyboardLayout()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(21)]
        public Result GetScreenShotProgramId()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(40)]
        public Result CreateManagedDisplayLayer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(41)]
        public Result IsSystemBufferSharingEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(42)]
        public Result GetSystemSharedLayerHandle()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(43)]
        public Result GetSystemSharedBufferHandle()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(44)]
        public Result CreateManagedDisplaySeparableLayer()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(45)]
        public Result SetManagedDisplayLayerSeparationMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(46)]
        public Result SetRecordingLayerCompositionEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(50)]
        public Result SetHandlesRequestToDisplay()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(51)]
        public Result ApproveToDisplay()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(60)]
        public Result OverrideAutoSleepTimeAndDimmingTime()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(61)]
        public Result SetMediaPlaybackState()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(62)]
        public Result SetIdleTimeDetectionExtension()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(63)]
        public Result GetIdleTimeDetectionExtension()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(64)]
        public Result SetInputDetectionSourceSet()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(65)]
        public Result ReportUserIsActive()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(66)]
        public Result GetCurrentIlluminance()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(67)]
        public Result IsIlluminanceAvailable()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(68)]
        public Result SetAutoSleepDisabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(69)]
        public Result IsAutoSleepDisabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(70)]
        public Result ReportMultimediaError()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(71)]
        public Result GetCurrentIlluminanceEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(72)]
        public Result SetInputDetectionPolicy()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(80)]
        public Result SetWirelessPriorityMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(90)]
        public Result GetAccumulatedSuspendedTickValue()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(91)]
        public Result GetAccumulatedSuspendedTickChangedEvent()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(100)]
        public Result SetAlbumImageTakenNotificationEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(110)]
        public Result SetApplicationAlbumUserData()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(120)]
        public Result SaveCurrentScreenshot()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(130)]
        public Result SetRecordVolumeMuted()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1000)]
        public Result GetDebugStorageChannel()
        {
            throw new System.NotImplementedException();
        }
    }
}
