using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0xC)]
    struct AddressEntry
    {
        public uint   Ipv4Address;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] MacAddress;
        public ushort Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x60)]
    struct AddressList
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public AddressEntry[] Addresses;
    }
}