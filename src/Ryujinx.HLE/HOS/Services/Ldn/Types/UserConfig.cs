using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x30)]
    struct UserConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x21)]
        public byte[] UserName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public byte[] Unknown1;
    }
}