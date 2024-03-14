using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class GlobalStateController : IGlobalStateController
    {
        [CmifCommand(0)]
        public Result RequestToEnterSleep()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return AmResult.Stubbed;
        }

        [CmifCommand(1)]
        public Result EnterSleep()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return AmResult.Stubbed;
        }

        [CmifCommand(2)]
        public Result StartSleepSequence(bool arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result StartShutdownSequence()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result StartRebootSequence()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(9)]
        public Result IsAutoPowerDownRequested(out bool arg0)
        {
            arg0 = false;
            // Uses #idle:sys cmd1
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result LoadAndApplyIdlePolicySettings()
        {
            // Uses #idle:sys cmd LoadAndApplySettings
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result NotifyCecSettingsChanged()
        {
            // Uses #omm cmd NotifyCecSettingsChanged
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(12)]
        public Result SetDefaultHomeButtonLongPressTime(long arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(13)]
        public Result UpdateDefaultDisplayResolution()
        {
            // Uses #omm cmd UpdateDefaultDisplayResolution
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(14)]
        public Result ShouldSleepOnBoot(out bool arg0)
        {
            arg0 = false;
            // Uses #omm cmd ShouldSleepOnBoot
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(15)]
        public Result GetHdcpAuthenticationFailedEvent(out int arg0)
        {
            arg0 = 0;
            // Uses #omm cmd GetHdcpAuthenticationFailedEvent
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(30)]
        public Result OpenCradleFirmwareUpdater(out ICradleFirmwareUpdater arg0)
        {
            arg0 = new CradleFirmwareUpdater();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
