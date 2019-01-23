using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ryujinx.Profiler
{
    public class InternalProfile
    {
        private Stopwatch SW;
        internal ConcurrentDictionary<string, TimingInfo> Timers;

        public InternalProfile()
        {
            Timers = new ConcurrentDictionary<string, TimingInfo>();
            SW = new Stopwatch();
            SW.Start();
        }

        public void BeginProfile(ProfileConfig config)
        {
            long timestamp = SW.ElapsedTicks;

            Timers.AddOrUpdate(config.Name,
                (string s) => CreateTimer(timestamp),
                ((s, info) =>
                {
                    info.BeginTime = timestamp;
                    return info;
                }));
        }

        public void EndProfile(ProfileConfig config)
        {
            long timestamp = SW.ElapsedTicks;

            Timers.AddOrUpdate(config.Name,
                (s => new TimingInfo()),
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
    }
}
