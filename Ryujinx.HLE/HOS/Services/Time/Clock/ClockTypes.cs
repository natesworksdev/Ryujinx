using Ryujinx.HLE.Utilities;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeSpanType
    {
        public ulong NanoSeconds;

        public TimeSpanType(ulong nanoSeconds)
        {
            NanoSeconds = nanoSeconds;
        }

        public ulong ToSeconds()
        {
            return NanoSeconds / 1000000000;
        }

        public static TimeSpanType FromTicks(ulong ticks, ulong frequency)
        {
            return new TimeSpanType(ticks * 1000000000 / frequency);
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SteadyClockTimePoint
    {
        public ulong        TimePoint;
        public UInt128      ClockSourceId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemClockContext
    {
        public ulong                Offset;
        public SteadyClockTimePoint SteadyTimePoint;
    }


}
