using System.Runtime.InteropServices;

namespace Ryujinx
{
    public struct HidMouseHeader //Size: 0x20
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }

    public struct HidMouseEntry //Size: 0x30
    {
        public ulong Timestamp;
        public ulong Timestamp_2;
        public uint X;
        public uint Y;
        public uint VelocityX;
        public uint VelocityY;
        public uint ScrollVelocityX;
        public uint ScrollVelocityY;
        public ulong Buttons;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidMouse //Size: 0x400
    {
        public HidMouseHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public HidMouseEntry[] Entries;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xB0)]
        public byte[] Padding;
    }
}
