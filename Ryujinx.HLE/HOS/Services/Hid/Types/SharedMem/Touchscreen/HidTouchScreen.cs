namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidTouchScreen
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidTouchScreenEntry> Entries;
        public fixed byte _Padding[0x3c8];
    }
}