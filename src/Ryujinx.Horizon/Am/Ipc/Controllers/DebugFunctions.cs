using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class DebugFunctions : IDebugFunctions
    {
        [CmifCommand(0)]
        public Result NotifyMessageToHomeMenuForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result OpenMainApplication()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result PerformSystemButtonPressing()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result InvalidateTransitionLayer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(30)]
        public Result RequestLaunchApplicationWithUserAndArgumentForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(31)]
        public Result RequestLaunchApplicationByApplicationLaunchInfoForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result GetAppletResourceUsageInfo()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(50)]
        public Result AddSystemProgramIdAndAppletIdForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result AddOperationConfirmedLibraryAppletIdForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(52)]
        public Result GetProgramIdFromAppletIdForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result SetCpuBoostModeForApplet()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result CancelCpuBoostModeForApplet()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(110)]
        public Result PushToAppletBoundChannelForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(111)]
        public Result TryPopFromAppletBoundChannelForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(120)]
        public Result AlarmSettingNotificationEnableAppEventReserve()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(121)]
        public Result AlarmSettingNotificationDisableAppEventReserve()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(122)]
        public Result AlarmSettingNotificationPushAppEventNotify()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(130)]
        public Result FriendInvitationSetApplicationParameter()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(131)]
        public Result FriendInvitationClearApplicationParameter()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(132)]
        public Result FriendInvitationPushApplicationParameter()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(140)]
        public Result RestrictPowerOperationForSecureLaunchModeForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result CreateFloatingLibraryAppletAccepterForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(300)]
        public Result TerminateAllRunningApplicationsForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(900)]
        public Result GetGrcProcessLaunchedSystemEvent()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
