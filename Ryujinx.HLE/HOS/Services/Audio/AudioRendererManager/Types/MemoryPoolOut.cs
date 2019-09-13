using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10, Pack = 4)]
    struct MemoryPoolOut
    {
        public MemoryPoolState State;
        public int             Unknown14;
        public long            Unknown18;
    }
}