using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Time;
using Ryujinx.Horizon.Sdk.Time.Sf;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    interface IStaticService : IServiceObject
    {
        Result GetStandardUserSystemClock(out ISystemClock arg0);
        Result GetStandardNetworkSystemClock(out ISystemClock arg0);
        Result GetStandardSteadyClock(out ISteadyClock arg0);
        Result GetTimeZoneService(out ITimeZoneService arg0);
        Result GetStandardLocalSystemClock(out ISystemClock arg0);
        Result GetEphemeralNetworkSystemClock(out ISystemClock arg0);
        Result GetSharedMemoryNativeHandle(out int arg0);
        Result SetStandardSteadyClockInternalOffset(TimeSpanType arg0);
        Result GetStandardSteadyClockRtcValue(out long arg0);
        Result IsStandardUserSystemClockAutomaticCorrectionEnabled(out bool arg0);
        Result SetStandardUserSystemClockAutomaticCorrectionEnabled(bool arg0);
        Result GetStandardUserSystemClockInitialYear(out int arg0);
        Result IsStandardNetworkSystemClockAccuracySufficient(out bool arg0);
        Result GetStandardUserSystemClockAutomaticCorrectionUpdatedTime(out SteadyClockTimePoint arg0);
        Result CalculateMonotonicSystemClockBaseTimePoint(out long arg0, SystemClockContext arg1);
        Result GetClockSnapshot(out ClockSnapshot arg0, byte arg1);
        Result GetClockSnapshotFromSystemClockContext(out ClockSnapshot arg0, SystemClockContext arg1, SystemClockContext arg2, byte arg3);
        Result CalculateStandardUserSystemClockDifferenceByUser(out TimeSpanType arg0, in ClockSnapshot arg1, in ClockSnapshot arg2);
        Result CalculateSpanBetween(out TimeSpanType arg0, in ClockSnapshot arg1, in ClockSnapshot arg2);
    }
}