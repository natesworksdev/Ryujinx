using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Aud.AudioRenderer
{
    [StructLayout(LayoutKind.Sequential, Size = 0x13, Pack = 4)]
    struct EffectOut
    {
        public EffectState State;
        public int         Unknown14;
        public long        Unknown18;
        public short       Unknown12;
        public byte        Unknown11;
    }
}
