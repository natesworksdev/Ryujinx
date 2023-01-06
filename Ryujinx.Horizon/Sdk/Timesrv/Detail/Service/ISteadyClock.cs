using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Time;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    interface ISteadyClock : IServiceObject
    {
        Result GetCurrentTimePoint(out SteadyClockTimePoint arg0);
        Result GetTestOffset(out TimeSpanType arg0);
        Result SetTestOffset(TimeSpanType arg0);
        Result GetRtcValue(out long arg0);
        Result IsRtcResetDetected(out bool arg0);
        Result GetSetupResultValue(out uint arg0);
        Result GetInternalOffset(out TimeSpanType arg0);
    }
}