namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct SixAxisSensorValues
    {
        public HidVector Accelerometer;
        public HidVector Gyroscope;
        public HidVector _Unk;
        public Array3<HidVector> Orientation;
    }
}