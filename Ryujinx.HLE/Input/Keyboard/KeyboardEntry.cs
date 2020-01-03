using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardEntry
    {
        public long SamplesTimestamp;
        public long SamplesTimestamp2;
        public long Modifier;
        public fixed int Keys[8];
    }
}
