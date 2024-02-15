using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ISystemAppletControllerForDebug : IServiceObject
    {
        Result RequestLaunchApplicationForDebug();
        Result GetDebugStorageChannel();
        Result CreateStorageForDebug();
        Result CreateCradleFirmwareUpdaterForDebug();
    }
}
