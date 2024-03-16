using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Ts;
using Ryujinx.Horizon.Ts.Ipc;

namespace Ryujinx.Horizon.Ptm.Ipc
{
    partial class MeasurementServer : IMeasurementServer
    {
        // NOTE: Values are randomly choosen.
        public const int DefaultTemperature = 42;
        public const int MinimumTemperature = 0;
        public const int MaximumTemperature = 100;

        [CmifCommand(0)] // 1.0.0-16.1.0
        public Result GetTemperatureRange(Location location, out int minimumTemperature, out int maximumTemperature)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePtm, new { location });

            minimumTemperature = MinimumTemperature;
            maximumTemperature = MaximumTemperature;

            return Result.Success;
        }

        [CmifCommand(1)] // 1.0.0-16.1.0
        public Result GetTemperature(Location location, out int temperature)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePtm, new { location });

            temperature = DefaultTemperature;

            return Result.Success;
        }

        [CmifCommand(2)] // 1.0.0-13.2.1
        public Result SetMeasurementMode(Location location, byte measurementMode)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePtm, new { location, measurementMode });

            return Result.Success;
        }

        [CmifCommand(3)] // 1.0.0-13.2.1
        public Result GetTemperatureMilliC(Location location, out int temperatureMilliC)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePtm, new { location });

            temperatureMilliC = DefaultTemperature * 1000;

            return Result.Success;
        }

        [CmifCommand(4)] // 8.0.0+
        Result IMeasurementServer.OpenSession(DeviceCode deviceCode, out ISession session)
        {
            session = new Session(deviceCode);

            return Result.Success;
        }
    }
}
