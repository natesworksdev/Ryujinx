namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct SixAxisState
    {
        public ulong SampleTimestamp;
        ulong _Unk1;
        public ulong SampleTimestamp2;
        public HidVector Accelerometer;
        public HidVector Gyroscope;
        HidVector _UnknownSensor;
        public fixed float Orientation[9];
        ulong _Unk2;
    }
}