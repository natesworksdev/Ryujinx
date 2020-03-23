namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidControllerLayout
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidControllerInputEntry> Entries;
    }
}