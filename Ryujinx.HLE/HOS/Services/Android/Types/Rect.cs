using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Android.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    struct Rect
    {
        public int Top;
        public int Left;
        public int Right;
        public int Bottom;
    }
}