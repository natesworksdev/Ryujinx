namespace Ryujinx.HLE.HOS.Services.Apm
{
    static class PerformanceState
    {
        public static bool CpuOverclockEnabled = false;

        public static PerformanceMode PerformanceMode = PerformanceMode.Default;
        public static CpuBoostMode    CpuBoostMode    = CpuBoostMode.Disabled;

        public static PerformanceConfiguration DefaultPerformanceConfiguration = PerformanceConfiguration.PerformanceConfiguration7;
        public static PerformanceConfiguration BoostPerformanceConfiguration   = PerformanceConfiguration.PerformanceConfiguration8;

        public static PerformanceConfiguration GetCurrentPerformanceConfiguration(PerformanceMode performanceMode)
        {
            return performanceMode switch
            {
                PerformanceMode.Default => DefaultPerformanceConfiguration,
                PerformanceMode.Boost   => BoostPerformanceConfiguration,
                _                       => PerformanceConfiguration.PerformanceConfiguration7
            };
        }
    }
}