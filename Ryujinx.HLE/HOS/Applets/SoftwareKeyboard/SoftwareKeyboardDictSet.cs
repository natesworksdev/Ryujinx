using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    struct SoftwareKeyboardDictSet
    {
        public ulong BufferPosition;

        public uint BufferSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public ulong[] Entries;

        public ushort TotalEntries;

        public ushort Padding1;
    }
}
