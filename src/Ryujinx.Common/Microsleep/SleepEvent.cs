using System;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    public class SleepEvent : IMicrosleepEvent
    {
        private AutoResetEvent _waitEvent = new(false);

        public bool CanSleepTil(long timePoint)
        {
            // Delay to timePoint is more than 1ms
            long now = PerformanceCounter.ElapsedTicks;
            long ms = Math.Min((timePoint - now) / PerformanceCounter.TicksPerMillisecond, int.MaxValue);

            return ms > 0;
        }

        public long AdjustTimePoint(long timePoint)
        {
            // No adjustment
            return timePoint;
        }

        public bool SleepUntil(long timePoint, bool strictlyBefore = false)
        {
            long now = PerformanceCounter.ElapsedTicks;
            long ms = Math.Min((timePoint - now) / PerformanceCounter.TicksPerMillisecond, int.MaxValue);

            if (ms > 0)
            {
                _waitEvent.WaitOne((int)ms);

                return true;
            }
            
            return false;
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
