using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ICradleFirmwareUpdater : IServiceObject
    {
        Result StartUpdate();
        Result FinishUpdate();
        Result GetCradleDeviceInfo(out CradleDeviceInfo arg0);
        Result GetCradleDeviceInfoChangeEvent(out int arg0);
        Result GetUpdateProgressInfo(out UpdateProgressInfo arg0);
        Result GetLastInternalResult();
    }
}
