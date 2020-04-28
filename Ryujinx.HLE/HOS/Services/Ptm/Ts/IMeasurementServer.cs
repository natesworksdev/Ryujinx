using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ptm.Ts.Types;

namespace Ryujinx.HLE.HOS.Services.Ptm.Ts
{
    [Service("ts")]
    class IMeasurementServer : IpcService
    {
        public IMeasurementServer(ServiceCtx context) { }

        [Command(3)]
        // GetTemperatureMilliC(Location location) -> u32
        public ResultCode GetTemperatureMilliC(ServiceCtx context)
        {
            Location location = (Location)context.RequestData.ReadByte();

            Logger.PrintStub(LogClass.ServicePsm, new { location });

            context.ResponseData.Write(42000u);

            return ResultCode.Success;
        }
    }
}