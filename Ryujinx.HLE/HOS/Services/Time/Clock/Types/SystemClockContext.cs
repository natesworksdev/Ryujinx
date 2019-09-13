using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Time.Clock.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct SystemClockContext
    {
        public long                 Offset;
        public SteadyClockTimePoint SteadyTimePoint;
    }
}