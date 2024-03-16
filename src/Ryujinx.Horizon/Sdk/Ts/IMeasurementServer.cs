using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Ts
{
    interface IMeasurementServer : IServiceObject
    {
        Result GetTemperatureRange(Location location, out int minimumTemperature, out int maximumTemperature);
        Result GetTemperature(Location location, out int temperature);
        Result SetMeasurementMode(Location location, byte measurementMode);
        Result GetTemperatureMilliC(Location location, out int temperatureMilliC);
        Result OpenSession(DeviceCode deviceCode, out ISession session);
    }
}
