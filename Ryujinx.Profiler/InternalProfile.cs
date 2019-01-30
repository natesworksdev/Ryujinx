﻿using System;
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
        internal ConcurrentDictionary<ProfileConfig, TimingInfo> Timers { get; set; }

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
            _timingFlags    = new TimingFlag[MaxFlags];
            Timers          = new ConcurrentDictionary<ProfileConfig, TimingInfo>();
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
                foreach (var timer in Timers)
                {
                    timer.Value.Cleanup(PerformanceCounter.ElapsedTicks - _history, _preserve - _history, _preserve);
                }

                // No need to run too often
                Thread.Sleep(50);
            }
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
            Timers.GetOrAdd(config, profileConfig => new TimingInfo()).Begin(PerformanceCounter.ElapsedTicks);
        }

        public void EndProfile(ProfileConfig config)
        {
            if (Timers.TryGetValue(config, out var timingInfo))
            {
                timingInfo.End(PerformanceCounter.ElapsedTicks);
            }
            else
            {
                // Throw exception if config isn't already being tracked
                throw new Exception($"Profiler end called before begin for {config.Tag}");
            }
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
            Dictionary<ProfileConfig, TimingInfo> outDict = new Dictionary<ProfileConfig, TimingInfo>();

            _preserve = PerformanceCounter.ElapsedTicks;

            // Forcibly get copy so user doesn't block profiling
            ProfileConfig[] configs = Timers.Keys.ToArray();
            TimingInfo[]    times   = Timers.Values.ToArray();

            for (int i = 0; i < configs.Length; i++)
            {
                outDict.Add(configs[i], times[i]);
            }

            foreach (ProfileConfig key in Timers.Keys)
            {
                TimingInfo value, prevValue;
                if (Timers.TryGetValue(key, out value))
                {
                    prevValue          = value;
                    value.Instant      = 0;
                    value.InstantCount = 0;
                    Timers.TryUpdate(key, value, prevValue);
                }
            }

            return outDict;
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
