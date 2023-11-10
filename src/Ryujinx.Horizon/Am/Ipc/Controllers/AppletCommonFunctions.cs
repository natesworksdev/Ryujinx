using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class AppletCommonFunctions : IAppletCommonFunctions

    {
        [CmifCommand(0)]
        public Result SetTerminateResult()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result ReadThemeStorage()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result WriteThemeStorage()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result PushToAppletBoundChannel()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result TryPopFromAppletBoundChannel()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result GetDisplayLogicalResolution()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(42)]
        public Result SetDisplayMagnification()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(50)]
        public Result SetHomeButtonDoubleClickEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result GetHomeButtonDoubleClickEnabled()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(52)]
        public Result IsHomeButtonShortPressedBlocked()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(60)]
        public Result IsVrModeCurtainRequired()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(61)]
        public Result IsSleepRequiredByHighTemperature()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(62)]
        public Result IsSleepRequiredByLowBattery()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(70)]
        public Result SetCpuBoostRequestPriority()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(80)]
        public Result SetHandlingCaptureButtonShortPressedMessageEnabledForApplet()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(81)]
        public Result SetHandlingCaptureButtonLongPressedMessageEnabledForApplet()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(90)]
        public Result OpenNamedChannelAsParent()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(91)]
        public Result OpenNamedChannelAsChild()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result SetApplicationCoreUsageMode()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
