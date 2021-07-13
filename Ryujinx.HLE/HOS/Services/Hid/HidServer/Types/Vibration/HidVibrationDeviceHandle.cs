namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidVibrationDeviceHandle
    {
        public byte DeviceType;
        public byte PlayerId;
        public HidVibrationDevicePosition Position;
        public byte Reserved;
    }
}