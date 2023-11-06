using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("android")]
    [SupportedOSPlatform("ios")]
    public class NanosleepPool : IDisposable
    {
        public const int MaxThreads = 8;

        private class NanosleepThread : IDisposable
        {
            private static readonly long _timePointEpsilon;

            static NanosleepThread()
            {
                _timePointEpsilon = PerformanceCounter.TicksPerMillisecond / 100; // 0.01ms
            }

            private readonly Thread _thread;
            private readonly NanosleepPool _parent;
            private readonly AutoResetEvent _newWaitEvent;
            private bool _running = true;

            private long _signalId;
            private long _nanoseconds;
            private long _timePoint;

            public long SignalId => _signalId;

            public NanosleepThread(NanosleepPool parent, int id)
            {
                _parent = parent;
                _newWaitEvent = new(false);

                _thread = new Thread(Loop)
                {
                    Name = $"Common.Nanosleep.{id}",
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = true
                };

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

                    Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Ptc, $"Sleep: {_nanoseconds / 1_000_000f}ms, Nanosleep inaccuracy: {diff / (float)PerformanceCounter.TicksPerMillisecond}ms");
                    */

                    _parent.Signal(this);
                    _newWaitEvent.WaitOne();
                }

                _newWaitEvent.Dispose();
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
                if (Math.Abs(timePoint - _timePoint) < _timePointEpsilon)
                {
                    _signalId = signalId;

                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                _running = false;
                _newWaitEvent.Set();
            }
        }

        private readonly object _lock = new();
        private readonly List<NanosleepThread> _threads = new();
        private readonly List<NanosleepThread> _active = new();
        private readonly Stack<NanosleepThread> _free = new();
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
                _free.Push(thread);

                if (thread.SignalId == _signalId)
                {
                    _signalTarget.Set();
                }
            }
        }


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

                if (!_free.TryPop(out NanosleepThread thread))
                {
                    if (_threads.Count >= MaxThreads)
                    {
                        return false;
                    }

                    thread = new NanosleepThread(this, _threads.Count);

                    _threads.Add(thread);
                }

                _active.Add(thread);

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
            GC.SuppressFinalize(this);

            foreach (NanosleepThread thread in _threads)
            {
                thread.Dispose();
            }
        }
    }
}
