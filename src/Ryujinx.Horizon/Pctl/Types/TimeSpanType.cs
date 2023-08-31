using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Pctl.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct TimeSpanType
    {
        public long NanoSeconds;
    }
}
