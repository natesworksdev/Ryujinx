using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    public class NanosleepPool : IDisposable
    {
        public const int MaxThreads = 32;

        private class NanosleepThread : IDisposable
        {
            private static long TimePointEpsilon;

            static NanosleepThread()
            {
                TimePointEpsilon = PerformanceCounter.TicksPerMillisecond / 100; // 0.01ms
            }

            private Thread _thread;
            private NanosleepPool _parent;
            private AutoResetEvent _newWaitEvent = new(false);
            private bool _running = true;

            private long _signalId;
            private long _nanoseconds;
            private long _timePoint;

            public long SignalId => _signalId;

            public NanosleepThread(NanosleepPool parent, int id)
            {
                _parent = parent;

                _thread = new Thread(Loop);
                _thread.Name = $"Common.Nanosleep.{id}";
                _thread.IsBackground = true;
                _thread.Start();
            }

            private void Loop()
            {
                _newWaitEvent.WaitOne();
                while (_running)
                {
                    Nanosleep.Sleep(_nanoseconds);

                    /*
                    var ticks = PerformanceCounter.ElapsedTicks;

                    var diff = ticks - _timePoint;

                    Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Ptc, $"Nanosleep inaccuracy: {diff / (float)PerformanceCounter.TicksPerMillisecond}ms");
                    */

                    _parent.Signal(this);
                    _newWaitEvent.WaitOne();
                }
            }

            public void SleepAndSignal(long nanoseconds, long signalId, long timePoint)
            {
                _signalId = signalId;
                _nanoseconds = nanoseconds;
                _timePoint = timePoint;
                _newWaitEvent.Set();
            }

            public bool Resurrect(long signalId, long timePoint)
            {
                if (Math.Abs(timePoint - _timePoint) < TimePointEpsilon)
                {
                    _signalId = signalId;

                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                _newWaitEvent.Dispose();
            }
        }

        private readonly object _lock = new();
        private readonly List<NanosleepThread> _threads = new();
        private readonly List<NanosleepThread> _active = new();
        private readonly Queue<NanosleepThread> _free = new();
        private readonly AutoResetEvent _signalTarget;

        private long _signalId;

        public NanosleepPool(AutoResetEvent signalTarget)
        {
            _signalTarget = signalTarget;
        }

        private void Signal(NanosleepThread thread)
        {
            lock (_lock)
            {
                _active.Remove(thread);
                _free.Enqueue(thread);

                if (thread.SignalId == _signalId)
                {
                    _signalTarget.Set();
                }
            }
        }

        private int _updater;

        public bool SleepAndSignal(long nanoseconds, long timePoint)
        {
            lock (_lock)
            {
                _signalId++;

                // Check active sleeps, if any line up with the requested timepoint then resurrect that nanosleep.
                foreach (NanosleepThread existing in _active)
                {
                    if (existing.Resurrect(_signalId, timePoint))
                    {
                        return true;
                    }
                }

                if (!_free.TryDequeue(out NanosleepThread thread))
                {
                    if (_threads.Count >= MaxThreads)
                    {
                        return false;
                    }

                    thread = new NanosleepThread(this, _threads.Count);

                    _threads.Add(thread);

                    Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Ptc, $"Nanosleep count: {_threads.Count}");
                }

                _active.Add(thread);

                /*
                if (++_updater % 100 == 0)
                {
                    Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Ptc, $"Active Nanosleep count: {_active.Count}");
                }
                */

                thread.SleepAndSignal(nanoseconds, _signalId, timePoint);

                return true;
            }
        }

        public void IgnoreSignal()
        {
            _signalId++;
        }

        public void Dispose()
        {
            foreach (NanosleepThread thread in _threads)
            {
                thread.Dispose();
            }
        }
    }
}
