using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x60)]
    struct ScanFilter
    {
        public NetworkId      NetworkId;
        public NetworkType    NetworkType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[]         MacAddress;
        public Ssid           Ssid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[]         Reserved;
        public ScanFilterFlag Flag;
    }
}