namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidControllerInputEntry
    {
        public ulong SampleTimestamp;
        public ulong SampleTimestamp2;
        public ControllerKeys Buttons;
        public Array2<JoystickPosition> Joysticks;
        public HidControllerConnectionState ConnectionState;
    }
}