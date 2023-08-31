using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Pctl.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct TimeSpan
    {
        public long NanoSeconds;
    }
}
