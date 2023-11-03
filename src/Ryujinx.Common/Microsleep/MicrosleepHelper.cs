using Ryujinx.Common.SystemInterop;
using System;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    public static class MicrosleepHelper
    {
        public static IMicrosleepEvent CreateEvent()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
            {
                return new NanosleepEvent();
            }
            else if (OperatingSystem.IsWindows())
            {
                return new WindowsSleepEvent();
            }
            else
            {
                return new SleepEvent();
            }
        }

        public static void SleepUntilTimePoint(AutoResetEvent evt, long timePoint)
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsGranularTimer.Instance.SleepUntilTimePoint(evt, timePoint);
            }
            else
            {
                long now = PerformanceCounter.ElapsedTicks;
                long ms = Math.Min((timePoint - now) / PerformanceCounter.TicksPerMillisecond, int.MaxValue);

                if (ms > 0)
                {
                    evt.WaitOne((int)ms);
                }

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
                {
                    // Do a nanosleep.
                    now = PerformanceCounter.ElapsedTicks;
                    long ns = ((timePoint - now) * 1_000_000) / PerformanceCounter.TicksPerMillisecond;

                    Nanosleep.SleepBefore(ns);
                }
            }
        }
    }
}
