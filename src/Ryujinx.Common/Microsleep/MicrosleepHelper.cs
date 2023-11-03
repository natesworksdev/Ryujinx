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

        /// <summary>
        /// Sleeps up to the closest point to the timepoint that the OS reasonably allows.
        /// The provided event is used by the timer to wake the current thread, and should not be signalled from any other source.
        /// </summary>
        /// <param name="evt">Event used to wake this thread</param>
        /// <param name="timePoint">Timepoint in host ticks</param>
        public static void SleepUntilTimePoint(AutoResetEvent evt, long timePoint)
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsGranularTimer.Instance.SleepUntilTimePointWithoutExternalSignal(evt, timePoint);
            }
            else
            {
                // Events might oversleep by a little, depending on OS.
                // We don't want to miss the timepoint, so bias the wait to be lower.
                // Nanosleep can possibly handle it better, too.
                long accuracyBias = PerformanceCounter.TicksPerMillisecond / 2;
                long now = PerformanceCounter.ElapsedTicks + accuracyBias;
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
