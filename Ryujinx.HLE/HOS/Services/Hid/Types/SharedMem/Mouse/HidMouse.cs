
namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidMouse
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidMouseEntry> Entries;
        public fixed byte _Padding[0xB0];
    }
}