using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Time;
using Ryujinx.Horizon.Sdk.Time.Sf;
using System;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    class TimeZoneService : ITimeZoneService
    {
        [CmifCommand(0)]
        public Result GetDeviceLocationName(out LocationName arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result SetDeviceLocationName(LocationName arg0)
        {
            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetTotalLocationNameCount(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result LoadLocationNameList(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<LocationName> arg1, int arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result LoadTimeZoneRule([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias, 0x4000)] out TimeZoneRule arg0, LocationName arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(5)]
        public Result GetTimeZoneRuleVersion(out TimeZoneRuleVersion arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(6)]
        public Result GetDeviceLocationNameAndUpdatedTime(out LocationName arg0, out SteadyClockTimePoint arg1)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(7)]
        public Result SetDeviceLocationNameWithTimeZoneRule(LocationName arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ReadOnlySpan<byte> arg1)
        {
            return Result.Success;
        }

        [CmifCommand(8)]
        public Result ParseTimeZoneBinary([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias, 0x4000)] out TimeZoneRule arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ReadOnlySpan<byte> arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result GetDeviceLocationNameOperationEventReadableHandle([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result ToCalendarTime(out CalendarTime arg0, out CalendarAdditionalInfo arg1, PosixTime arg2, [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias, 0x4000)] in TimeZoneRule arg3)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result ToCalendarTimeWithMyRule(out CalendarTime arg0, out CalendarAdditionalInfo arg1, PosixTime arg2)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(201)]
        public Result ToPosixTime(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<PosixTime> arg1, CalendarTime arg2, [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias, 0x4000)] in TimeZoneRule arg3)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(202)]
        public Result ToPosixTimeWithMyRule(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<PosixTime> arg1, CalendarTime arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        public IReadOnlyDictionary<int, CommandHandler> GetCommandHandlers()
        {
            throw new NotImplementedException();
        }
    }
}