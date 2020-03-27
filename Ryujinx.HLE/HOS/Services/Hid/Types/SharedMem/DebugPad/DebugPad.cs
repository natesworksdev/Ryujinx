namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct ShMemDebugPad
    {
        public CommonEntriesHeader Header;
        public Array17<DebugPadEntry> Entries;
        fixed byte padding[0x138];
    }
}