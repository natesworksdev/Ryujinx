using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x22, CharSet = CharSet.Ansi)]
    struct Ssid
    {
        public byte Length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x21)]
        public byte[] Name;
    }
}
