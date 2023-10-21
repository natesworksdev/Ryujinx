using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class CommonStateGetter : ICommonStateGetter
    {
        [CmifCommand(0)]
        public Result GetEventHandle()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1)]
        public Result ReceiveMessage()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(2)]
        public Result GetThisAppletKind()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(3)]
        public Result AllowToEnterSleep()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(4)]
        public Result DisallowToEnterSleep()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(5)]
        public Result GetOperationMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(6)]
        public Result GetPerformanceMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(7)]
        public Result GetCradleStatus()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(8)]
        public Result GetBootMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(9)]
        public Result GetCurrentFocusState()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(10)]
        public Result RequestToAcquireSleepLock()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(11)]
        public Result ReleaseSleepLock()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(12)]
        public Result ReleaseSleepLockTransiently()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(13)]
        public Result GetAcquiredSleepLockEvent()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(14)]
        public Result GetWakeupCount()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(20)]
        public Result PushToGeneralChannel()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(30)]
        public Result GetHomeButtonReaderLockAccessor()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(31)]
        public Result GetReaderLockAccessorEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(32)]
        public Result GetWriterLockAccessorEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(40)]
        public Result GetCradleFwVersion()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(50)]
        public Result IsVrModeEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(51)]
        public Result SetVrModeEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(52)]
        public Result SetLcdBacklightOffEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(53)]
        public Result BeginVrModeEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(54)]
        public Result EndVrModeEx()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(55)]
        public Result IsInControllerFirmwareUpdateSection()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(59)]
        public Result SetVrPositionForDebug()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(60)]
        public Result GetDefaultDisplayResolution()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(61)]
        public Result GetDefaultDisplayResolutionChangeEvent()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(62)]
        public Result GetHdcpAuthenticationState()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(63)]
        public Result GetHdcpAuthenticationStateChangeEvent()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(64)]
        public Result SetTvPowerStateMatchingMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(65)]
        public Result GetApplicationIdByContentActionName()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(66)]
        public Result SetCpuBoostMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(67)]
        public Result CancelCpuBoostMode()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(68)]
        public Result GetBuiltInDisplayType()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(80)]
        public Result PerformSystemButtonPressingIfInFocus()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(90)]
        public Result SetPerformanceConfigurationChangedNotification()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(91)]
        public Result GetCurrentPerformanceConfiguration()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(100)]
        public Result SetHandlingHomeButtonShortPressedEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(110)]
        public Result OpenMyGpuErrorHandler()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(120)]
        public Result GetAppletLaunchedHistory()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(200)]
        public Result GetOperationModeSystemInfo()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(300)]
        public Result GetSettingsPlatformRegion()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(400)]
        public Result ActivateMigrationService()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(401)]
        public Result DeactivateMigrationService()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(500)]
        public Result DisableSleepTillShutdown()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(501)]
        public Result SuppressDisablingSleepTemporarily()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(502)]
        public Result IsSleepEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(503)]
        public Result IsDisablingSleepSuppressed()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(600)]
        public Result OpenNamedChannelAsChild()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(900)]
        public Result SetRequestExitToLibraryAppletAtExecuteNextProgramEnabled()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(910)]
        public Result GetLaunchRequiredTick()
        {
            throw new System.NotImplementedException();
        }
    }
}
