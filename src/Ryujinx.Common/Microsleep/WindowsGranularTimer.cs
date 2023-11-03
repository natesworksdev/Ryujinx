using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Common.SystemInterop
{
    /// <summary>
    /// Timer that attempts to align with the system timer interrupt.
    /// </summary>
    public partial class WindowsGranularTimer
    {
        private readonly struct WaitingObject
        {
            public readonly AutoResetEvent Signal;
            public readonly long TimePoint;

            public WaitingObject(AutoResetEvent signal, long timePoint)
            {
                Signal = signal;
                TimePoint = timePoint;
            }
        }

        [LibraryImport("ntdll.dll", SetLastError = true)]
        private static partial int NtSetTimerResolution(int DesiredResolution, [MarshalAs(UnmanagedType.Bool)] bool SetResolution, out int CurrentResolution);

        [LibraryImport("ntdll.dll", SetLastError = true)]
        private static partial int NtQueryTimerResolution(out int MaximumResolution, out int MinimumResolution, out int CurrentResolution);

        [LibraryImport("ntdll.dll", SetLastError = true)]
        private static partial uint NtDelayExecution([MarshalAs(UnmanagedType.Bool)] bool Alertable, ref long DelayInterval);

        private static WindowsGranularTimer _instance = new();
        public static WindowsGranularTimer Instance => _instance;

        public long GranularityNs => _granularityNs;

        private Thread _timerThread;
        private long _granularityNs = 500000;
        private long _granularityTicks;
        private bool _running = true;
        private long _lastTicks = PerformanceCounter.ElapsedTicks;

        private object _lock = new();
        private List<WaitingObject> _waitingObjects = new();

        public WindowsGranularTimer()
        {
            _timerThread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "Common.WindowsTimer",
                Priority = ThreadPriority.Highest
            };
            _timerThread.Start();
        }

        private void Initialize()
        {
            NtQueryTimerResolution(out _, out int min, out int curr);

            if (min > 0)
            {
                _granularityNs = min * 100L;
                NtSetTimerResolution(min, true, out _);
            }
            else
            {
                _granularityNs = curr * 100L;
            }

            _granularityTicks = (_granularityNs * PerformanceCounter.TicksPerMillisecond) / 1_000_000;
        }

        public void Loop()
        {
            Initialize();
            while (_running)
            {
                long delayInterval = -1; // Next tick
                NtSetTimerResolution((int)(_granularityNs / 100), true, out _);
                NtDelayExecution(false, ref delayInterval);

                long newTicks = PerformanceCounter.ElapsedTicks;
                long nextTicks = newTicks + _granularityTicks;

                if (newTicks > _lastTicks + (_granularityNs * 3) / 2)
                {
                    System.Console.WriteLine($"Missed sleep... {(newTicks - _lastTicks) / (float)PerformanceCounter.TicksPerMillisecond}ms");
                }

                lock (_lock)
                {
                    if (_waitingObjects.Count > 0)
                    {
                        for (int i = 0; i < _waitingObjects.Count; i++)
                        {
                            if (nextTicks > _waitingObjects[i].TimePoint)
                            {
                                // The next clock tick will be after the timepoint, we need to signal now.
                                _waitingObjects[i].Signal.Set();

                                _waitingObjects.RemoveAt(i--);
                            }
                        }
                    }

                    _lastTicks = newTicks;
                }
            }
        }

        public bool SleepUntilTimePoint(AutoResetEvent evt, long timePoint)
        {
            if (evt.WaitOne(0))
            {
                return true;
            }

            lock (_lock)
            {
                // Return immediately if the next tick is after the requested timepoint.
                long nextTicks = _lastTicks + _granularityTicks;

                if (nextTicks > timePoint)
                {
                    return false;
                }

                _waitingObjects.Add(new WaitingObject(evt, timePoint));
            }

            evt.WaitOne();

            return true;
        }

        public (long, long) ReturnNearestTicks(long timePoint)
        {
            long last = _lastTicks;
            long delta = timePoint - last;

            long lowTicks = delta / _granularityTicks;
            long highTicks = (delta + _granularityTicks - 1) / _granularityTicks;

            return (last + lowTicks * _granularityTicks, last + highTicks * _granularityTicks);
        }
    }
}
