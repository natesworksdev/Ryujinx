using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Profiler
{
    public static class Profile
    {
        public static float UpdateRate => _settings.UpdateRate;
        public static long HistoryLength => _settings.History;

        private static InternalProfile  _profileInstance;
        private static ProfilerSettings _settings;

        public static bool ProfilingEnabled()
        {
            if (!_settings.Enabled)
                return false;

            if (_profileInstance == null)
                _profileInstance = new InternalProfile(_settings.History);

            return true;
        }

        public static void Configure(ProfilerSettings settings)
        {
            _settings = settings;
        }

        public static void FinishProfiling()
        {
            if (!ProfilingEnabled())
                return;

            if (_settings.FileDumpEnabled)
                DumpProfile.ToFile(_settings.DumpLocation, _profileInstance);

            _profileInstance.Dispose();
        }

        public static void FlagTime(TimingFlagType flagType)
        {
            if (!ProfilingEnabled())
                return;
            _profileInstance.FlagTime(flagType);
        }

        public static void RegisterFlagReciever(Action<TimingFlag> reciever)
        {
            if (!ProfilingEnabled())
                return;
            _profileInstance.RegisterFlagReciever(reciever);
        }

        public static void Begin(ProfileConfig config)
        {
            if (!ProfilingEnabled())
                return;
            _profileInstance.BeginProfile(config);
        }

        public static void End(ProfileConfig config)
        {
            if (!ProfilingEnabled())
                return;
            _profileInstance.EndProfile(config);
        }

        public static void Method(ProfileConfig config, Action method)
        {
            // If profiling is disabled just call the method
            if (!ProfilingEnabled())
            {
                method();
                return;
            }

            Begin(config);
            method();
            End(config);
        }

        public static string GetSession()
        {
            if (!ProfilingEnabled())
                return null;
            return _profileInstance.GetSession();
        }

        public static double ConvertTicksToMS(long ticks)
        {
            return (((double)ticks) / Stopwatch.Frequency) * 1000.0;
        }

        public static long ConvertSecondsToTicks(double seconds)
        {
            return (long)(seconds * Stopwatch.Frequency);
        }

        public static long ConvertMSToTicks(double ms)
        {
            return (long)((ms / 1000) * Stopwatch.Frequency);
        }

        public static Dictionary<ProfileConfig, TimingInfo> GetProfilingData()
        {
            if (!ProfilingEnabled())
                return new Dictionary<ProfileConfig, TimingInfo>();
            return _profileInstance.GetProfilingData();
        }

        public static TimingFlag[] GetTimingFlags()
        {
            if (!ProfilingEnabled())
                return new TimingFlag[0];
            return _profileInstance.GetTimingFlags();
        }

        public static long GetCurrentTime()
        {
            if (!ProfilingEnabled())
                return 0;
            return _profileInstance.CurrentTime;
        }
    }
}
