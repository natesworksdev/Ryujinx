using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Time;
using Ryujinx.Horizon.Sdk.Time.Sf;
using System;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    interface ITimeZoneService : IServiceObject
    {
        Result GetDeviceLocationName(out LocationName arg0);
        Result SetDeviceLocationName(LocationName arg0);
        Result GetTotalLocationNameCount(out int arg0);
        Result LoadLocationNameList(out int arg0, Span<LocationName> arg1, int arg2);
        Result LoadTimeZoneRule(out TimeZoneRule arg0, LocationName arg1);
        Result GetTimeZoneRuleVersion(out TimeZoneRuleVersion arg0);
        Result GetDeviceLocationNameAndUpdatedTime(out LocationName arg0, out SteadyClockTimePoint arg1);
        Result SetDeviceLocationNameWithTimeZoneRule(LocationName arg0, ReadOnlySpan<byte> arg1);
        Result ParseTimeZoneBinary(out TimeZoneRule arg0, ReadOnlySpan<byte> arg1);
        Result GetDeviceLocationNameOperationEventReadableHandle(out int arg0);
        Result ToCalendarTime(out CalendarTime arg0, out CalendarAdditionalInfo arg1, PosixTime arg2, in TimeZoneRule arg3);
        Result ToCalendarTimeWithMyRule(out CalendarTime arg0, out CalendarAdditionalInfo arg1, PosixTime arg2);
        Result ToPosixTime(out int arg0, Span<PosixTime> arg1, CalendarTime arg2, in TimeZoneRule arg3);
        Result ToPosixTimeWithMyRule(out int arg0, Span<PosixTime> arg1, CalendarTime arg2);
    }
}
