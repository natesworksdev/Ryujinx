using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Aud.AudioRenderer
{
    [StructLayout(LayoutKind.Sequential, Size = 0xc0, Pack = 4)]
    unsafe struct EffectIn
    {
        public EffectType Type;

        public byte       FirstUpdate;
        public byte       Enabled;

        public byte       Unknown11;

        public uint       MixID;

        public ulong      BufferBase;
        public ulong      BufferSZ;

        public int        Priority;

        public uint       Unknown24;
        public fixed byte Padding[0xa0];
    }
}
