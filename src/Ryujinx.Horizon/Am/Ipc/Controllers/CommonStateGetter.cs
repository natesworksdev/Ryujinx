using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class CommonStateGetter : ICommonStateGetter
    {
        [CmifCommand(0)]
        public Result GetEventHandle()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result ReceiveMessage()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetThisAppletKind()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result AllowToEnterSleep()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result DisallowToEnterSleep()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(5)]
        public Result GetOperationMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(6)]
        public Result GetPerformanceMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(7)]
        public Result GetCradleStatus()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(8)]
        public Result GetBootMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(9)]
        public Result GetCurrentFocusState()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result RequestToAcquireSleepLock()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result ReleaseSleepLock()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(12)]
        public Result ReleaseSleepLockTransiently()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(13)]
        public Result GetAcquiredSleepLockEvent()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(14)]
        public Result GetWakeupCount()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result PushToGeneralChannel()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(30)]
        public Result GetHomeButtonReaderLockAccessor()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(31)]
        public Result GetReaderLockAccessorEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(32)]
        public Result GetWriterLockAccessorEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result GetCradleFwVersion()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(50)]
        public Result IsVrModeEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result SetVrModeEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(52)]
        public Result SetLcdBacklightOffEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(53)]
        public Result BeginVrModeEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(54)]
        public Result EndVrModeEx()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(55)]
        public Result IsInControllerFirmwareUpdateSection()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(59)]
        public Result SetVrPositionForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(60)]
        public Result GetDefaultDisplayResolution()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(61)]
        public Result GetDefaultDisplayResolutionChangeEvent()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(62)]
        public Result GetHdcpAuthenticationState()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(63)]
        public Result GetHdcpAuthenticationStateChangeEvent()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(64)]
        public Result SetTvPowerStateMatchingMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(65)]
        public Result GetApplicationIdByContentActionName()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(66)]
        public Result SetCpuBoostMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(67)]
        public Result CancelCpuBoostMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(68)]
        public Result GetBuiltInDisplayType()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(80)]
        public Result PerformSystemButtonPressingIfInFocus()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(90)]
        public Result SetPerformanceConfigurationChangedNotification()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(91)]
        public Result GetCurrentPerformanceConfiguration()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result SetHandlingHomeButtonShortPressedEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(110)]
        public Result OpenMyGpuErrorHandler()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(120)]
        public Result GetAppletLaunchedHistory()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result GetOperationModeSystemInfo()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(300)]
        public Result GetSettingsPlatformRegion()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(400)]
        public Result ActivateMigrationService()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(401)]
        public Result DeactivateMigrationService()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(500)]
        public Result DisableSleepTillShutdown()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(501)]
        public Result SuppressDisablingSleepTemporarily()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(502)]
        public Result IsSleepEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(503)]
        public Result IsDisablingSleepSuppressed()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(600)]
        public Result OpenNamedChannelAsChild()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(900)]
        public Result SetRequestExitToLibraryAppletAtExecuteNextProgramEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(910)]
        public Result GetLaunchRequiredTick()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
