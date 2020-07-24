using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x19)]
    struct LdnHeader
    {
        public uint Magic;
        public byte Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] UserId;
        public int DataSize;
    }
}