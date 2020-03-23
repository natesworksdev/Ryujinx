namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidTouchScreenEntryHeader
    {
        public ulong SampleTimestamp;
        public ulong SampleTimestamp2;
        public ulong NumTouches;
    }
}