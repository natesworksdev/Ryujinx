using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Spacemeowx2Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    internal struct LanPacketHeader
    {
        public uint Magic;
        public LanPacketType Type;
        public byte Compressed;
        public ushort Length;
        public ushort DecompressLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] _reserved;
    }
}