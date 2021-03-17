using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    class KTimeManager : IDisposable
    {
        private readonly KernelContext _context;
        // TODO: PriorityQueue will have the best performance here.
        private readonly HashSet<IKFutureSchedulerObject> _waitingObjects = new HashSet<IKFutureSchedulerObject>();
        private AutoResetEvent _waitEvent;
        private bool _keepRunning;

        public KTimeManager(KernelContext context)
        {
            _context = context;
            _keepRunning = true;

            Thread work = new Thread(WaitAndCheckScheduledObjects)
            {
                Name = "HLE.TimeManager"
            };

            work.Start();
        }

        public void ScheduleFutureInvocation(IKFutureSchedulerObject schedulerObj, long timeout)
        {
            long timePoint = PerformanceCounter.ElapsedMilliseconds + ConvertNanosecondsToMilliseconds(timeout);

            lock (_context.CriticalSection.Lock)
            {
                if (_waitingObjects.TryGetValue(schedulerObj, out var existing))
                {
                    if (timePoint < existing.TimePoint)
                    {
                        existing.TimePoint = timePoint;
                    }
                }
                else
                {
                    schedulerObj.TimePoint = timePoint;
                    _waitingObjects.Add(schedulerObj);
                }
            }

            _waitEvent.Set();
        }

        public void UnscheduleFutureInvocation(IKFutureSchedulerObject schedulerObj)
        {
            lock (_context.CriticalSection.Lock)
            {
                // Not calling TimeUp here, 
                // TimeUp should only be called when the timeout expires,
                // if it expires (the scheduler object may be signaled and remove itself from the list before that happens).
                _waitingObjects.Remove(schedulerObj);
            }
        }

        private void WaitAndCheckScheduledObjects()
        {
            using (_waitEvent = new AutoResetEvent(false))
            {
                while (_keepRunning)
                {
                    IKFutureSchedulerObject next;
                    lock (_context.CriticalSection.Lock)
                    {
                        // TODO: PriorityQueue will has the best performance here.
                        next = _waitingObjects.OrderBy(x => x.TimePoint).FirstOrDefault();
                    }

                    if (next != null)
                    {
                        long timePoint = PerformanceCounter.ElapsedMilliseconds;
                        if (next.TimePoint > timePoint)
                        {
                            _waitEvent.WaitOne((int)(next.TimePoint - timePoint));
                        }

                        var timeUp = PerformanceCounter.ElapsedMilliseconds >= next.TimePoint;
                        if (timeUp)
                        {
                            lock (_context.CriticalSection.Lock)
                            {
                                if (_waitingObjects.Remove(next))
                                {
                                    next.TimeUp();
                                }
                            }
                        }
                    }
                    else
                    {
                        _waitEvent.WaitOne();
                    }
                }
            }
        }

        public static long ConvertNanosecondsToMilliseconds(long time)
        {
            time /= 1000000;

            if ((ulong)time > int.MaxValue)
            {
                return int.MaxValue;
            }

            return time;
        }

        public static long ConvertMillisecondsToNanoseconds(long time)
        {
            return time * 1000000;
        }

        public static long ConvertHostTicksToTicks(long time)
        {
            return (long)((time / (double)PerformanceCounter.TicksPerSecond) * 19200000.0);
        }

        public void Dispose()
        {
            _keepRunning = false;
            _waitEvent?.Set();
        }
    }
}