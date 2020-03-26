namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct DebugPadEntry
    {
        public ulong SampleTimestamp;
        fixed byte _Unk[0x20];
    }
}