namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct ShMemKeyboard
    {
        public CommonEntriesHeader Header;
        public Array17<KeyboardState> Entries;
        fixed byte padding[0x28];
    }
}