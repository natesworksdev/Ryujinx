using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KTimeManager : IDisposable
    {
        private class WaitingObject
        {
            public IKFutureSchedulerObject Object { get; private set; }

            public long TimePoint { get; private set; }

            public WaitingObject(IKFutureSchedulerObject Object, long timePoint)
            {
                this.Object    = Object;
                this.TimePoint = timePoint;
            }
        }

        private List<WaitingObject> _waitingObjects;

        private AutoResetEvent _waitEvent;

        private Stopwatch _counter;

        private bool _keepRunning;

        public KTimeManager()
        {
            _waitingObjects = new List<WaitingObject>();

            _counter = new Stopwatch();

            _counter.Start();

            _keepRunning = true;

            Thread work = new Thread(WaitAndCheckScheduledObjects);

            work.Start();
        }

        public void ScheduleFutureInvocation(IKFutureSchedulerObject Object, long timeout)
        {
            lock (_waitingObjects)
            {
                long timePoint = _counter.ElapsedMilliseconds + ConvertNanosecondsToMilliseconds(timeout);

                _waitingObjects.Add(new WaitingObject(Object, timePoint));
            }

            _waitEvent.Set();
        }

        private long ConvertNanosecondsToMilliseconds(long timeout)
        {
            timeout /= 1000000;

            if ((ulong)timeout > int.MaxValue) return int.MaxValue;

            return timeout;
        }

        public void UnscheduleFutureInvocation(IKFutureSchedulerObject Object)
        {
            lock (_waitingObjects)
            {
                _waitingObjects.RemoveAll(x => x.Object == Object);
            }
        }

        private void WaitAndCheckScheduledObjects()
        {
            using (_waitEvent = new AutoResetEvent(false))
            {
                while (_keepRunning)
                {
                    Monitor.Enter(_waitingObjects);

                    WaitingObject next = _waitingObjects.OrderBy(x => x.TimePoint).FirstOrDefault();

                    Monitor.Exit(_waitingObjects);

                    if (next != null)
                    {
                        long timePoint = _counter.ElapsedMilliseconds;

                        if (next.TimePoint > timePoint) _waitEvent.WaitOne((int)(next.TimePoint - timePoint));

                        Monitor.Enter(_waitingObjects);

                        bool timeUp = _counter.ElapsedMilliseconds >= next.TimePoint && _waitingObjects.Remove(next);

                        Monitor.Exit(_waitingObjects);

                        if (timeUp) next.Object.TimeUp();
                    }
                    else
                    {
                        _waitEvent.WaitOne();
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keepRunning = false;

                _waitEvent?.Set();
            }
        }
    }
}