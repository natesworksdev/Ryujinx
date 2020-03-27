
namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct ShMemMouse
    {
        public CommonEntriesHeader Header;
        public Array17<MouseState> Entries;
        fixed byte padding[0xB0];
    }
}