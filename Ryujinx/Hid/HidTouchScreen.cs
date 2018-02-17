using System.Runtime.InteropServices;

namespace Ryujinx
{
    public struct HidTouchScreenHeader //Size: 0x28
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
        public ulong Timestamp;
    }

    public struct HidTouchScreenEntryHeader //Size: 0x10
    {
        public ulong Timestamp;
        public ulong NumTouches;
    }

    public struct HidTouchScreenEntryTouch //Size: 0x28
    {
        public ulong Timestamp;
        public uint Padding;
        public uint TouchIndex;
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint Angle;
        public uint Padding_2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidTouchScreenEntry //Size: 0x298
    {
        public HidTouchScreenEntryHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public HidTouchScreenEntryTouch[] Touches; 
        public ulong Unknown;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidTouchScreen //Size: 0x3000
    {
        public HidTouchScreenHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public HidTouchScreenEntry[] Entries;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3C0)]
        public byte[] Padding;
    }
}
