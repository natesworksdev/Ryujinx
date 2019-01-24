using System;

namespace Ryujinx.Profiler
{
    public static class Profile
    {
        // Static
        private static InternalProfile ProfileInstance;
        private static ProfilerSettings Settings;

        private static bool ProfilingEnabled()
        {
            if (!Settings.Enabled)
                return false;

            if (ProfileInstance == null)
                ProfileInstance = new InternalProfile();
            return true;
        }

        public static void Configure(ProfilerSettings settings)
        {
            Settings = settings;
        }

        public static void FinishProfiling()
        {
            if (!ProfilingEnabled())
                return;

            if (Settings.FileDumpEnabled)
                DumpProfile.ToFile(Settings.DumpLocation, ProfileInstance);
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

        public static string GetSession()
        {
            if (!ProfilingEnabled())
                return null;
            return ProfileInstance.GetSession();
        }
    }
}
