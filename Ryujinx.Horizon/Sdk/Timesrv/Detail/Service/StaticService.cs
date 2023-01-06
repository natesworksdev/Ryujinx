using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Time;
using Ryujinx.Horizon.Sdk.Time.Sf;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    class StaticService : IStaticService
    {
        [CmifCommand(0)]
        public Result GetStandardUserSystemClock(out ISystemClock arg0)
        {
            arg0 = new SystemClock();

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetStandardNetworkSystemClock(out ISystemClock arg0)
        {
            arg0 = new SystemClock();

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetStandardSteadyClock(out ISteadyClock arg0)
        {
            arg0 = new SteadyClock();

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetTimeZoneService(out ITimeZoneService arg0)
        {
            arg0 = new TimeZoneService();

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result GetStandardLocalSystemClock(out ISystemClock arg0)
        {
            arg0 = new SystemClock();

            return Result.Success;
        }

        [CmifCommand(5)]
        public Result GetEphemeralNetworkSystemClock(out ISystemClock arg0)
        {
            arg0 = new SystemClock();

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result GetSharedMemoryNativeHandle([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(50)]
        public Result SetStandardSteadyClockInternalOffset(TimeSpanType arg0)
        {
            return Result.Success;
        }

        [CmifCommand(51)]
        public Result GetStandardSteadyClockRtcValue(out long arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result IsStandardUserSystemClockAutomaticCorrectionEnabled(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result SetStandardUserSystemClockAutomaticCorrectionEnabled(bool arg0)
        {
            return Result.Success;
        }

        [CmifCommand(102)]
        public Result GetStandardUserSystemClockInitialYear(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result IsStandardNetworkSystemClockAccuracySufficient(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(201)]
        public Result GetStandardUserSystemClockAutomaticCorrectionUpdatedTime(out SteadyClockTimePoint arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(300)]
        public Result CalculateMonotonicSystemClockBaseTimePoint(out long arg0, SystemClockContext arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(400)]
        public Result GetClockSnapshot([Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer, 0xD0)] out ClockSnapshot arg0, byte arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(401)]
        public Result GetClockSnapshotFromSystemClockContext([Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer, 0xD0)] out ClockSnapshot arg0, SystemClockContext arg1, SystemClockContext arg2, byte arg3)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(500)]
        public Result CalculateStandardUserSystemClockDifferenceByUser(out TimeSpanType arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0xD0)] in ClockSnapshot arg1, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0xD0)] in ClockSnapshot arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(501)]
        public Result CalculateSpanBetween(out TimeSpanType arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0xD0)] in ClockSnapshot arg1, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer, 0xD0)] in ClockSnapshot arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        public IReadOnlyDictionary<int, CommandHandler> GetCommandHandlers()
        {
            throw new System.NotImplementedException();
        }
    }
}