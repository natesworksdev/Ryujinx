using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ro
{
    [StructLayout(LayoutKind.Sequential, Size = 0x220)]
    unsafe struct NRRCertification
    {
        public ulong ApplicationIdMask;
        public ulong ApplicationIdPattern;
        private Array16<byte> _reserved;
        public fixed byte Modulus[0x100];
        public fixed byte Signature[0x100];
    }
}
