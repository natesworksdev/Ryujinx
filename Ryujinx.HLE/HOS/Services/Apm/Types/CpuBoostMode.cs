namespace Ryujinx.HLE.HOS.Services.Apm
{
    public enum CpuBoostMode
    {
        Disabled      = 0,
        BoostCPU      = 1, // Uses PerformanceConfiguration13 and PerformanceConfiguration14, or PerformanceConfiguration15 and PerformanceConfiguration16
        ConservePower = 2  // Uses PerformanceConfiguration15 and PerformanceConfiguration16.
    }
}