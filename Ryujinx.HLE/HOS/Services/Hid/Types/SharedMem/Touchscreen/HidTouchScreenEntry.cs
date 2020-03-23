namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidTouchScreenEntry
    {
        public HidTouchScreenEntryHeader Header;
        public Array16<HidTouchScreenEntryTouch> Touches;
    }
}