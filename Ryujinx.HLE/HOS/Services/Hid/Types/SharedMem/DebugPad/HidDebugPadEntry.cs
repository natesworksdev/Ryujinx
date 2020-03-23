namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidDebugPadEntry
    {
        public ulong SampleTimestamp;
        public fixed byte _Unk[0x20];
    }
}