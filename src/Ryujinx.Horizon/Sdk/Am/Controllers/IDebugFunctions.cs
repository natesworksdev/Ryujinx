using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IDebugFunctions : IServiceObject
    {
        Result NotifyMessageToHomeMenuForDebug();
        Result OpenMainApplication();
        Result PerformSystemButtonPressing();
        Result InvalidateTransitionLayer();
        Result RequestLaunchApplicationWithUserAndArgumentForDebug();
        Result RequestLaunchApplicationByApplicationLaunchInfoForDebug();
        Result GetAppletResourceUsageInfo();
        Result AddSystemProgramIdAndAppletIdForDebug();
        Result AddOperationConfirmedLibraryAppletIdForDebug();
        Result GetProgramIdFromAppletIdForDebug();
        Result SetCpuBoostModeForApplet();
        Result CancelCpuBoostModeForApplet();
        Result PushToAppletBoundChannelForDebug();
        Result TryPopFromAppletBoundChannelForDebug();
        Result AlarmSettingNotificationEnableAppEventReserve();
        Result AlarmSettingNotificationDisableAppEventReserve();
        Result AlarmSettingNotificationPushAppEventNotify();
        Result FriendInvitationSetApplicationParameter();
        Result FriendInvitationClearApplicationParameter();
        Result FriendInvitationPushApplicationParameter();
        Result RestrictPowerOperationForSecureLaunchModeForDebug();
        Result CreateFloatingLibraryAppletAccepterForDebug();
        Result TerminateAllRunningApplicationsForDebug();
        Result GetGrcProcessLaunchedSystemEvent();
    }
}
