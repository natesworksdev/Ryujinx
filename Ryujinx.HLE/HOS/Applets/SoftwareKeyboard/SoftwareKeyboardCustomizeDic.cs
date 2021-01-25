using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct SoftwareKeyboardCustomizeDic
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
        public byte[] Unknown;
    }
}
