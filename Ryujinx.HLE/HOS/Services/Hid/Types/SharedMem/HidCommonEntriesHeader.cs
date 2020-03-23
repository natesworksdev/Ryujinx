namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidCommonEntriesHeader
    {
        public ulong TimestampTicks;
        public ulong NumEntries;
        public ulong LatestEntry;
        public ulong MaxEntryIndex;
    }
}

