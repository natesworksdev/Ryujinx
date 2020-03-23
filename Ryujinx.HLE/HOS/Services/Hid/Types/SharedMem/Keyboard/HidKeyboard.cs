namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidKeyboard
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidKeyboardEntry> Entries;
        public fixed byte _Padding[0x28];
    }
}