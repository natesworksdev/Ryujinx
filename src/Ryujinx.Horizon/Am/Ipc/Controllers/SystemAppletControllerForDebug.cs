using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class SystemAppletControllerForDebug : ISystemAppletControllerForDebug
    {
        [CmifCommand(1)]
        public Result RequestLaunchApplicationForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetDebugStorageChannel()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result CreateStorageForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result CreateCradleFirmwareUpdaterForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
