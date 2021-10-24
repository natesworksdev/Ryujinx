using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Time.TimeZone
{
    [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x2C)]
    struct TzifHeader
    {
        public Array4<byte> Magic;
        public byte Version;
        private Array15<byte> _reserved;
        public Array4<byte> TtisGMTCount;
        public Array4<byte> TtisSTDCount;
        public Array4<byte> LeapCount;
        public Array4<byte> TimeCount;
        public Array4<byte> TypeCount;
        public Array4<byte> CharCount;
    }
}