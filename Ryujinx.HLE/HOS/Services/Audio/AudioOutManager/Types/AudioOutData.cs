using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioOutManager.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct AudioOutData
    {
        public long NextBufferPtr;
        public long SampleBufferPtr;
        public long SampleBufferCapacity;
        public long SampleBufferSize;
        public long SampleBufferInnerOffset;
    }
}