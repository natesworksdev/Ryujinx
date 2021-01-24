using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct SoftwareKeyboardUserWord
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] Unknown;
    }
}
