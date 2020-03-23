namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidDebugPad
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidDebugPadEntry> Entries;
        public fixed byte _Padding[0x138];
    }
}