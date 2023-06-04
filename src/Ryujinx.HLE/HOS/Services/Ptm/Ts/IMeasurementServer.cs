using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ptm.Ts.Types;

namespace Ryujinx.HLE.HOS.Services.Ptm.Ts
{
    [Service("ts")]
    class IMeasurementServer : IpcService
    {
        private const uint DefaultTemperature = 42u;

#pragma warning disable IDE0060 // Remove unused parameter
        public IMeasurementServer(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(1)]
        // GetTemperature(Location location) -> u32
        public static ResultCode GetTemperature(ServiceCtx context)
        {
            Location location = (Location)context.RequestData.ReadByte();

            Logger.Stub?.PrintStub(LogClass.ServicePtm, new { location });

            context.ResponseData.Write(DefaultTemperature);

            return ResultCode.Success;
        }

        [CommandCmif(3)]
        // GetTemperatureMilliC(Location location) -> u32
        public static ResultCode GetTemperatureMilliC(ServiceCtx context)
        {
            Location location = (Location)context.RequestData.ReadByte();

            Logger.Stub?.PrintStub(LogClass.ServicePtm, new { location });

            context.ResponseData.Write(DefaultTemperature * 1000);

            return ResultCode.Success;
        }
    }
}