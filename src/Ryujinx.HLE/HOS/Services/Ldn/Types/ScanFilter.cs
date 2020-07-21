using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x60, CharSet = CharSet.Ansi)]
    struct ScanFilter
    {
        public NetworkId      NetworkId; //0x20
        public uint           NetworkType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[]         MacAddress;
        public Ssid           Ssid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[]         Unknown;
        public ScanFilterFlag Flag;
    }
}