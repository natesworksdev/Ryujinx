namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidControllerSixAxisLayout
    {
        public HidCommonEntriesHeader Header;
        public Array17<HidControllerSixAxisEntry> Entries;
    }
}