using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class SelfController : ISelfController
    {
        [CmifCommand(0)]
        public Result Exit()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result LockExit()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result UnlockExit()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result EnterFatalSection()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result LeaveFatalSection()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(9)]
        public Result GetLibraryAppletLaunchableEvent(out int arg0)
        {
            arg0 = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result SetScreenShotPermission(int arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result SetOperationModeChangedNotification(bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(12)]
        public Result SetPerformanceModeChangedNotification(bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(13)]
        public Result SetFocusHandlingMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(14)]
        public Result SetRestartMessageEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(15)]
        public Result SetScreenShotAppletIdentityInfo()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(16)]
        public Result SetOutOfFocusSuspendingEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(17)]
        public Result SetControllerFirmwareUpdateSection()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(18)]
        public Result SetRequiresCaptureButtonShortPressedMessage()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(19)]
        public Result SetAlbumImageOrientation()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result SetDesirableKeyboardLayout()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result GetScreenShotProgramId()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result CreateManagedDisplayLayer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(41)]
        public Result IsSystemBufferSharingEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(42)]
        public Result GetSystemSharedLayerHandle()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(43)]
        public Result GetSystemSharedBufferHandle()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(44)]
        public Result CreateManagedDisplaySeparableLayer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(45)]
        public Result SetManagedDisplayLayerSeparationMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(46)]
        public Result SetRecordingLayerCompositionEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(50)]
        public Result SetHandlesRequestToDisplay()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result ApproveToDisplay()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(60)]
        public Result OverrideAutoSleepTimeAndDimmingTime()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(61)]
        public Result SetMediaPlaybackState()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(62)]
        public Result SetIdleTimeDetectionExtension()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(63)]
        public Result GetIdleTimeDetectionExtension()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(64)]
        public Result SetInputDetectionSourceSet()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(65)]
        public Result ReportUserIsActive()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(66)]
        public Result GetCurrentIlluminance()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(67)]
        public Result IsIlluminanceAvailable()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(68)]
        public Result SetAutoSleepDisabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(69)]
        public Result IsAutoSleepDisabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(70)]
        public Result ReportMultimediaError()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(71)]
        public Result GetCurrentIlluminanceEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(72)]
        public Result SetInputDetectionPolicy()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(80)]
        public Result SetWirelessPriorityMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(90)]
        public Result GetAccumulatedSuspendedTickValue()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(91)]
        public Result GetAccumulatedSuspendedTickChangedEvent()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result SetAlbumImageTakenNotificationEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(110)]
        public Result SetApplicationAlbumUserData()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(120)]
        public Result SaveCurrentScreenshot()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(130)]
        public Result SetRecordVolumeMuted()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result GetDebugStorageChannel()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
