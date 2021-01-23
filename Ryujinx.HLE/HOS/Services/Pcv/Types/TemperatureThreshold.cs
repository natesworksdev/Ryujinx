using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Pcv
{
    [StructLayout(LayoutKind.Sequential)]
    struct TemperatureThreshold
    {
        public int MinMilliC;
        public int MaxMilliC;
    }
}