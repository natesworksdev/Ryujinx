namespace Ryujinx.HLE.HOS.Services.Hid
{
    unsafe struct ShMemNpad
    {
        public NpadStateHeader Header;
        public Array7<NpadLayout> Layouts; // One for each ControllerLayoutType
        public Array6<NpadSixAxis> Sixaxis;
        public DeviceType DeviceType;
        uint _padding;
        public NpadSystemProperties SystemProperties;
        public uint NpadSystemButtonProperties;
        public Array3<BatteryCharge> BatteryState;
        fixed byte _NfcXcdDeviceHandleHeader[0x20];
        fixed byte _NfcXcdDeviceHandleState[0x20 * 2];
        ulong mutex;
        fixed byte _NpadGcTriggerHeader[0x20];
        fixed byte _NpadGcTriggerState[0x18 * 17];
        fixed byte _padding2[0xC38];
    }
}