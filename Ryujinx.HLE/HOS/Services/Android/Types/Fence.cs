using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Android.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x8)]
    struct Fence
    {
        public int id;
        public int value;
    }
}