using System;
using Ryujinx.Common.Logging;

namespace Ryujinx.Profiler
{
    public class Profile
    {
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
            Logger.PrintInfo(LogClass.Gpu, $"Begin {config.Name}");
        }

        public static void End(ProfileConfig config)
        {
            if (!ProfilingEnabled())
                return;
            Logger.PrintInfo(LogClass.Gpu, $"End {config.Name}");
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
    }
}
