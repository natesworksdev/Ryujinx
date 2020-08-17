using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioOutManager
{
    [StructLayout(LayoutKind.Sequential)]
    struct AudioOutData
    {
        public ulong NextBufferPtr;
        public ulong SampleBufferPtr;
        public ulong SampleBufferCapacity;
        public ulong SampleBufferSize;
        public ulong SampleBufferInnerOffset;
    }
}