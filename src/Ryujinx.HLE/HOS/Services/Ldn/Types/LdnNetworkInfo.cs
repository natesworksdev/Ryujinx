using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x430, CharSet = CharSet.Ansi)]
    struct LdnNetworkInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[]     UnknownRandom;
        public ushort     SecurityMode;
        public byte       StationAcceptPolicy;
        public byte       Reserved1;
        public ushort     Reserved2;
        public byte       NodeCountMax;
        public byte       NodeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public NodeInfo[] Nodes;
        public ushort     Reserved3;
        public ushort     AdvertiseDataSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x180)]
        public byte[]     AdvertiseData;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x94)]
        public byte[]     Unknown;
    }
}
