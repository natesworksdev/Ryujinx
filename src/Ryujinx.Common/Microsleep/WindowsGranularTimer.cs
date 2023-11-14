using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Common.SystemInterop
{
    /// <summary>
    /// Timer that attempts to align with the hardware timer interrupt,
    /// and can alert listeners on ticks.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class WindowsGranularTimer
    {
        private static WindowsGranularTimer _instance = new();
        public static WindowsGranularTimer Instance => _instance;

        private readonly struct WaitingObject
        {
            public readonly long Id;
            public readonly AutoResetEvent Signal;
            public readonly long TimePoint;

            public WaitingObject(long id, AutoResetEvent signal, long timePoint)
            {
                Id = id;
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

        public long GranularityNs => _granularityNs;
        public long GranularityTicks => _granularityTicks;

        private Thread _timerThread;
        private long _granularityNs = 500000;
        private long _granularityTicks;
        private bool _running = true;
        private long _lastTicks = PerformanceCounter.ElapsedTicks;
        private long _lastId;

        private object _lock = new();
        private List<WaitingObject> _waitingObjects = new();

        private WindowsGranularTimer()
        {
            _timerThread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "Common.WindowsTimer",
                Priority = ThreadPriority.Highest
            };
            _timerThread.Start();
        }

        /// <summary>
        /// Measure and initialize the timer's target granularity.
        /// </summary>
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

        /// <summary>
        /// Main loop for the timer thread. Wakes every clock tick and signals any listeners,
        /// as well as keeping track of clock alignment.
        /// </summary>
        private void Loop()
        {
            Initialize();
            while (_running)
            {
                long delayInterval = -1; // Next tick
                NtSetTimerResolution((int)(_granularityNs / 100), true, out _);
                NtDelayExecution(false, ref delayInterval);

                long newTicks = PerformanceCounter.ElapsedTicks;
                long nextTicks = newTicks + _granularityTicks;

                /*
                if (newTicks > _lastTicks + (_granularityTicks * 3) / 2)
                {
                    System.Console.WriteLine($"Missed sleep... {(newTicks - _lastTicks) / (float)PerformanceCounter.TicksPerMillisecond}ms");
                }
                */

                lock (_lock)
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

                    _lastTicks = newTicks;
                }
            }
        }

        /// <summary>
        /// Sleep until a timepoint.
        /// </summary>
        /// <param name="evt">Reset event to use to be awoken by the clock tick, or an external signal</param>
        /// <param name="timePoint">Target timepoint</param>
        /// <returns>True if waited or signalled, false otherwise</returns>
        public bool SleepUntilTimePoint(AutoResetEvent evt, long timePoint)
        {
            if (evt.WaitOne(0))
            {
                return true;
            }

            long id;

            lock (_lock)
            {
                // Return immediately if the next tick is after the requested timepoint.
                long nextTicks = _lastTicks + _granularityTicks;

                if (nextTicks > timePoint)
                {
                    return false;
                }

                id = ++_lastId;

                _waitingObjects.Add(new WaitingObject(id, evt, timePoint));
            }

            evt.WaitOne();

            lock (_lock)
            {
                for (int i = 0; i < _waitingObjects.Count; i++)
                {
                    if (id == _waitingObjects[i].Id)
                    {
                        _waitingObjects.RemoveAt(i--);
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Sleep until a timepoint, but don't expect any external signals.
        /// </summary>
        /// <remarks>
        /// Saves some effort compared to the sleep that expects to be signalled.
        /// </remarks>
        /// <param name="evt">Reset event to use to be awoken by the clock tick</param>
        /// <param name="timePoint">Target timepoint</param>
        /// <returns>True if waited, false otherwise</returns>
        public bool SleepUntilTimePointWithoutExternalSignal(AutoResetEvent evt, long timePoint)
        {
            long id;

            lock (_lock)
            {
                // Return immediately if the next tick is after the requested timepoint.
                long nextTicks = _lastTicks + _granularityTicks;

                if (nextTicks > timePoint)
                {
                    return false;
                }

                id = ++_lastId;

                _waitingObjects.Add(new WaitingObject(id, evt, timePoint));
            }

            evt.WaitOne();

            return true;
        }

        /// <summary>
        /// Returns the two nearest clock ticks for a given timepoint.
        /// </summary>
        /// <param name="timePoint">Target timepoint</param>
        /// <returns>The nearest clock ticks before and after the given timepoint</returns>
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
