using System.Runtime.InteropServices;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    [StructLayout(LayoutKind.Sequential, Size = 0xc, Pack = 2)]
    struct BiquadFilter
    {
        public short B0;
        public short B1;
        public short B2;
        public short A1;
        public short A2;
    }
}
