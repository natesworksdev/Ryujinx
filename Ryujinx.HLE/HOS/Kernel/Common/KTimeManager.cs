using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    class KTimeManager : IDisposable
    {
        private class WaitingObject
        {
            public IKFutureSchedulerObject Object { get; }
            public long TimePoint { get; set; }

            public WaitingObject(IKFutureSchedulerObject schedulerObj, long timePoint)
            {
                Object = schedulerObj;
                TimePoint = timePoint;
            }
        }

        private readonly KernelContext _context;
        // TODO: PriorityQueue will have the best performance here.
        private readonly IDictionary<int, WaitingObject> _waitingObjects;
        private AutoResetEvent _waitEvent;
        private bool _keepRunning;

        public KTimeManager(KernelContext context)
        {
            _context = context;
            _waitingObjects = new Dictionary<int, WaitingObject>();
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

            var key = schedulerObj.GetHashCode();
            lock (_context.CriticalSection.Lock)
            {
                if (_waitingObjects.TryGetValue(key, out var existing))
                {
                    if (timePoint < existing.TimePoint)
                    {
                        existing.TimePoint = timePoint;
                    }
                }
                else
                {
                    _waitingObjects.Add(key, new WaitingObject(schedulerObj, timePoint));
                }
            }

            _waitEvent.Set();
        }

        public void UnscheduleFutureInvocation(IKFutureSchedulerObject schedulerObj)
        {
            var key = schedulerObj.GetHashCode();
            if (_waitingObjects.ContainsKey(key))
            {
                lock (_context.CriticalSection.Lock)
                {
                    // Call TimeUp here?
                    _waitingObjects.Remove(key);
                }
            }
        }

        private void WaitAndCheckScheduledObjects()
        {
            using (_waitEvent = new AutoResetEvent(false))
            {
                while (_keepRunning)
                {
                    KeyValuePair<int, WaitingObject> nextPair;
                    lock (_context.CriticalSection.Lock)
                    {
                        // TODO: PriorityQueue will has the best performance here.
                        nextPair = _waitingObjects.OrderBy(x => x.Value.TimePoint).FirstOrDefault();
                    }

                    var next = nextPair.Value;
                    if (next != null)
                    {
                        long timePoint = PerformanceCounter.ElapsedMilliseconds;
                        if (next.TimePoint > timePoint)
                        {
                            _waitEvent.WaitOne((int)(next.TimePoint - timePoint));
                        }

                        lock (_context.CriticalSection.Lock)
                        {
                            if (_waitingObjects.Remove(nextPair.Key))
                            {
                                next.Object.TimeUp();
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