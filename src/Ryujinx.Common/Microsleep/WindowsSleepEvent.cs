using Ryujinx.Common.SystemInterop;
using System;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    public class WindowsSleepEvent : IMicrosleepEvent
    {
        /// <summary>
        /// The clock can drift a bit, so add this to encourage the clock to still wait if the next tick is forecasted slightly before it.
        /// </summary>
        private const long ErrorBias = 50000;

        /// <summary>
        /// Allowed to be 0.05ms away from the clock granularity to reduce precision.
        /// </summary>
        private const long ClockAlignedBias = 50000;

        /// <summary>
        /// The fraction of clock granularity above the timepoint that will align it down to the lower timepoint.
        /// Currently set to the lower 1/4, so for 0.5ms granularity: 0.1ms would be rounded down, 0.2 ms would be rounded up.
        /// </summary>
        private const long ReverseTimePointFraction = 4;

        private readonly AutoResetEvent _waitEvent = new(false);
        private readonly WindowsGranularTimer _timer = WindowsGranularTimer.Instance;

        public bool Precise { get; set; } = false;

        public long AdjustTimePoint(long timePoint, long timeoutNs)
        {
            if (Precise || timePoint == long.MaxValue)
            {
                return timePoint;
            }

            // Does the timeout align with the host clock?

            long granularity = _timer.GranularityNs;
            long misalignment = timeoutNs % granularity;
            
            if (misalignment < ClockAlignedBias || misalignment > granularity - ClockAlignedBias)
            {
                // Inaccurate sleep for 0.5ms increments, typically.

                (long low, long high) = _timer.ReturnNearestTicks(timePoint);

                if (timePoint - low < granularity / ReverseTimePointFraction)
                {
                    timePoint = low;
                }
                else
                {
                    timePoint = high;
                }
            }
            
            return timePoint;
        }

        public bool SleepUntil(long timePoint, bool strictlyBefore = false)
        {
            return _timer.SleepUntilTimePoint(_waitEvent, timePoint + ErrorBias);
        }

        public void Sleep()
        {
            _waitEvent.WaitOne();
        }

        public void Signal()
        {
            _waitEvent.Set();
        }

        public void Dispose()
        {
            _waitEvent.Dispose();
        }
    }
}
