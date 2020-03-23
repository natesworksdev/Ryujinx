namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidKeyboardEntry
    {
        public ulong SampleTimestamp;
        public ulong SampleTimestamp2;
        public ulong Modifier;
        public fixed uint Keys[8];
    }

}