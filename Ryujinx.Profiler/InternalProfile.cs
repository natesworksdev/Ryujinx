using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ryujinx.Profiler
{
    public class InternalProfile
    {
        private Stopwatch SW;
        internal ConcurrentDictionary<ProfileConfig, TimingInfo> Timers;

        private object sessionLock = new object();
        private int sessionCounter = 0;

        public InternalProfile()
        {
            Timers = new ConcurrentDictionary<ProfileConfig, TimingInfo>();
            SW = new Stopwatch();
            SW.Start();
        }

        public void BeginProfile(ProfileConfig config)
        {
            long timestamp = SW.ElapsedTicks;

            Timers.AddOrUpdate(config,
                (c) => CreateTimer(timestamp),
                ((s, info) =>
                {
                    info.BeginTime = timestamp;
                    return info;
                }));
        }

        public void EndProfile(ProfileConfig config)
        {
            long timestamp = SW.ElapsedTicks;

            Timers.AddOrUpdate(config,
                (c => new TimingInfo()),
                ((s, time) => UpdateTimer(time, timestamp)));
        }

        private TimingInfo CreateTimer(long timestamp)
        {
            return new TimingInfo()
            {
                BeginTime = timestamp,
                LastTime = 0,
                Count = 0,
            };
        }

        private TimingInfo UpdateTimer(TimingInfo time, long timestamp)
        {
            time.Count++;
            time.LastTime = timestamp - time.BeginTime;
            time.TotalTime += time.LastTime;

            return time;
        }

        public double ConvertTicksToMS(long ticks)
        {
            return (((double)ticks) / Stopwatch.Frequency) * 1000.0;
        }

        public int GetSession()
        {
            // Can be called from multiple threads so locked to ensure no duplicate sessions are generated
            lock (sessionLock)
            {
                return sessionCounter++;
            }
        }
    }
}
