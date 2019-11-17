using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    [StructLayout(LayoutKind.Explicit)]
    struct SoftwareKeyboardConfig
    {
        [FieldOffset(0x0)]
        public SoftwareKeyboardType Type;
        
        [FieldOffset(0x3AC)]
        public uint StringLengthMax;
        
        [FieldOffset(0x3B0)]
        public uint StringLengthMaxExtended;

        [FieldOffset(0x3D0), MarshalAs(UnmanagedType.I1)]
        public bool CheckText;
    }
}
