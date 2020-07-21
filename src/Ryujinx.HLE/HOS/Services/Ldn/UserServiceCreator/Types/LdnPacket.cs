using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x619)]
    struct LdnPacket
    {
        public uint Magic;
        public byte Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] UserId;
        public int DataSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x600)]
        public byte[] Data;
    }
}