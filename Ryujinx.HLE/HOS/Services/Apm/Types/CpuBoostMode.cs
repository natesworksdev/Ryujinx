namespace Ryujinx.HLE.HOS.Services.Apm
{
    enum CpuBoostMode
    {
        Disabled      = 0,
        BoostCPU      = 1, // Use PerformanceConfiguration13 and PerformanceConfiguration14, or PerformanceConfiguration15 and PerformanceConfiguration16
        ConservePower = 2  // Use PerformanceConfiguration15 and PerformanceConfiguration16.
    }
}