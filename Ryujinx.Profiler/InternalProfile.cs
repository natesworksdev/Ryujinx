using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Ryujinx.Common;

namespace Ryujinx.Profiler
{
    public class InternalProfile
    {
        private struct TimerQueueValue
        {
            public ProfileConfig Config;
            public long Time;
            public bool IsBegin;
        }

        internal Dictionary<ProfileConfig, TimingInfo> Timers { get; set; }

        private readonly object _timerQueueClearLock = new object();
        private ConcurrentQueue<TimerQueueValue> _timerQueue;

        private readonly object _sessionLock = new object();
        private int _sessionCounter = 0;

        // Cleanup thread
        private readonly Thread _cleanupThread;
        private bool _cleanupRunning;
        private readonly long _history;
        private long _preserve;

        // Timing flags
        private TimingFlag[] _timingFlags;
        private int _timingFlagCount;
        private int _timingFlagIndex;

        private const int MaxFlags = 500;

        private Action<TimingFlag> _timingFlagCallback;

        public InternalProfile(long history)
        {
            Timers          = new Dictionary<ProfileConfig, TimingInfo>();
            _timingFlags    = new TimingFlag[MaxFlags];
            _timerQueue     = new ConcurrentQueue<TimerQueueValue>();
            _history        = history;
            _cleanupRunning = true;

            // Create low priority cleanup thread, it only cleans up RAM hence the low priority
            _cleanupThread = new Thread(CleanupLoop)
            {
                Priority = ThreadPriority.Lowest
            };
            _cleanupThread.Start();
        }

        private void CleanupLoop()
        {
            while (_cleanupRunning)
            {
                ClearTimerQueue();

                foreach (var timer in Timers)
                {
                    timer.Value.Cleanup(PerformanceCounter.ElapsedTicks - _history, _preserve - _history, _preserve);
                }

                // No need to run too often
                Thread.Sleep(5);
            }
        }

        private void ClearTimerQueue()
        {
            // Ensure we only ever have 1 instance running
            if (!Monitor.TryEnter(_timerQueueClearLock))
            {
                return;
            }

            while (_timerQueue.TryDequeue(out var item))
            {
                if (!Timers.TryGetValue(item.Config, out var value))
                {
                    value = new TimingInfo();
                    Timers.Add(item.Config, value);
                }

                if (item.IsBegin)
                {
                    value.Begin(item.Time);
                }
                else
                {
                    value.End(item.Time);
                }
            }
            Monitor.Exit(_timerQueueClearLock);
        }

        public void FlagTime(TimingFlagType flagType)
        {
            _timingFlags[_timingFlagIndex] = new TimingFlag()
            {
                FlagType  = flagType,
                Timestamp = PerformanceCounter.ElapsedTicks
            };

            if (++_timingFlagIndex >= MaxFlags)
            {
                _timingFlagIndex = 0;
            }

            _timingFlagCount = Math.Max(_timingFlagCount + 1, MaxFlags);

            _timingFlagCallback?.Invoke(_timingFlags[_timingFlagIndex]);
        }

        public void BeginProfile(ProfileConfig config)
        {
            _timerQueue.Enqueue(new TimerQueueValue()
            {
                Config  = config,
                IsBegin = true,
                Time    = PerformanceCounter.ElapsedTicks,
            });
        }

        public void EndProfile(ProfileConfig config)
        {
            _timerQueue.Enqueue(new TimerQueueValue()
            {
                Config  = config,
                IsBegin = false,
                Time    = PerformanceCounter.ElapsedTicks,
            });
        }

        public string GetSession()
        {
            // Can be called from multiple threads so locked to ensure no duplicate sessions are generated
            lock (_sessionLock)
            {
                return (_sessionCounter++).ToString();
            }
        }

        public Dictionary<ProfileConfig, TimingInfo> GetProfilingData()
        {
            _preserve = PerformanceCounter.ElapsedTicks;
            ClearTimerQueue();

            return Timers;
        }

        public TimingFlag[] GetTimingFlags()
        {
            int count = Math.Max(_timingFlagCount, MaxFlags);
            TimingFlag[] outFlags = new TimingFlag[count];
            
            for (int i = 0, sourceIndex = _timingFlagIndex; i < count; i++, sourceIndex++)
            {
                if (sourceIndex >= MaxFlags)
                    sourceIndex = 0;
                outFlags[i] = _timingFlags[sourceIndex];
            }

            return outFlags;
        }

        public void RegisterFlagReciever(Action<TimingFlag> reciever)
        {
            _timingFlagCallback = reciever;
        }

        public void Dispose()
        {
            _cleanupRunning = false;
            _cleanupThread.Join();
        }
    }
}
