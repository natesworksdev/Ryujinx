namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct DebugPadEntry
    {
        public ulong SampleTimestamp;
        fixed byte unknown[0x20];
    }
}