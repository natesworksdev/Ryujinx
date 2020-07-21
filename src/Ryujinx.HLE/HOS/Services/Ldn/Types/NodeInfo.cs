using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x40, CharSet = CharSet.Ansi)]
    struct NodeInfo
    {
        public uint   Ipv4Address;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] MacAddress;
        public byte   NodeId;
        public byte   IsConnected;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x21)]
        public byte[] UserName;
        public byte   Unknown1;
        public ushort LocalCommunicationVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] Unknown2;
    }
}
