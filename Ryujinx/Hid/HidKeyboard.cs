using System.Runtime.InteropServices;

namespace Ryujinx
{
    public struct HidKeyboardHeader //Size: 0x20
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidKeyboardEntry //Size: 0x38
    {
        public ulong Timestamp;
        public ulong Timestamp_2;
        public ulong Modifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public uint[] Keys;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidKeyboard //Size: 0x400
    {
        public HidKeyboardHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public HidKeyboardEntry[] Entries;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x28)]
        public byte[] Padding;
    }
}
