namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidControllerSixAxisEntry
    {
        public ulong SampleTimestamp;
        public ulong _Unk1;
        public ulong SampleTimestamp2;
        public SixAxisSensorValues Values;
        public ulong _Unk3;
    }
}