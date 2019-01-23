using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ryujinx.Profiler
{
    public class Profile
    {
        private struct TimingInfo
        {
            public long BeginTime, LastTime, TotalTime, Count;
            public long AverageTime
            {
                get => (Count == 0) ? -1 : TotalTime / Count;
            }
        }


        // Static
        private static Profile ProfileInstance;
        private static ProfilerSettings Settings;

        private static bool ProfilingEnabled()
        {
            if (!Settings.Enabled)
                return false;

            if (ProfileInstance == null)
                ProfileInstance = new Profile();
            return true;
        }

        public static void Configure(ProfilerSettings settings)
        {
            Settings = settings;
        }

        public static void Begin(ProfileConfig config)
        {
            if (!ProfilingEnabled())
                return;
            ProfileInstance.BeginProfile(config);
        }

        public static void End(ProfileConfig config)
        {
            if (!ProfilingEnabled())
                return;
            ProfileInstance.EndProfile(config);
        }

        public static void Method(ProfileConfig config, Action method)
        {
            // If profiling is disabled just call the method
            if (!ProfilingEnabled())
                method();

            Begin(config);
            method();
            End(config);
        }


        // Non-static
        private Stopwatch SW;
        private ConcurrentDictionary<string, TimingInfo> Timers;

        public Profile()
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
    }
}
