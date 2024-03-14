using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ICommonStateGetter : IServiceObject
    {
        Result GetEventHandle();
        Result ReceiveMessage();
        Result GetThisAppletKind();
        Result AllowToEnterSleep();
        Result DisallowToEnterSleep();
        Result GetOperationMode();
        Result GetPerformanceMode();
        Result GetCradleStatus();
        Result GetBootMode();
        Result GetCurrentFocusState();
        Result RequestToAcquireSleepLock();
        Result ReleaseSleepLock();
        Result ReleaseSleepLockTransiently();
        Result GetAcquiredSleepLockEvent();
        Result GetWakeupCount();
        Result PushToGeneralChannel();
        Result GetHomeButtonReaderLockAccessor();
        Result GetReaderLockAccessorEx();
        Result GetWriterLockAccessorEx();
        Result GetCradleFwVersion();
        Result IsVrModeEnabled();
        Result SetVrModeEnabled();
        Result SetLcdBacklightOffEnabled();
        Result BeginVrModeEx();
        Result EndVrModeEx();
        Result IsInControllerFirmwareUpdateSection();
        Result SetVrPositionForDebug();
        Result GetDefaultDisplayResolution();
        Result GetDefaultDisplayResolutionChangeEvent();
        Result GetHdcpAuthenticationState();
        Result GetHdcpAuthenticationStateChangeEvent();
        Result SetTvPowerStateMatchingMode();
        Result GetApplicationIdByContentActionName();
        Result SetCpuBoostMode();
        Result CancelCpuBoostMode();
        Result GetBuiltInDisplayType();
        Result PerformSystemButtonPressingIfInFocus();
        Result SetPerformanceConfigurationChangedNotification();
        Result GetCurrentPerformanceConfiguration();
        Result SetHandlingHomeButtonShortPressedEnabled();
        Result OpenMyGpuErrorHandler();
        Result GetAppletLaunchedHistory();
        Result GetOperationModeSystemInfo();
        Result GetSettingsPlatformRegion();
        Result ActivateMigrationService();
        Result DeactivateMigrationService();
        Result DisableSleepTillShutdown();
        Result SuppressDisablingSleepTemporarily();
        Result IsSleepEnabled();
        Result IsDisablingSleepSuppressed();
        Result OpenNamedChannelAsChild();
        Result SetRequestExitToLibraryAppletAtExecuteNextProgramEnabled();
        Result GetLaunchRequiredTick();
    }
}
