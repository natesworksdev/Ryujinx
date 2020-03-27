namespace Ryujinx.HLE.HOS.Services.Hid
{
    // TODO: Add missing structs
    unsafe struct ShMemNpad
    {
        public NpadStateHeader Header;
        public Array7<NpadLayout> Layouts; // One for each NpadLayoutsIndex
        public Array6<NpadSixAxis> Sixaxis;
        public DeviceType DeviceType;
        uint padding1;
        public NpadSystemProperties SystemProperties;
        public uint NpadSystemButtonProperties;
        public Array3<BatteryCharge> BatteryState;
        fixed byte NfcXcdDeviceHandleHeader[0x20];
        fixed byte NfcXcdDeviceHandleState[0x20 * 2];
        ulong mutex;
        fixed byte NpadGcTriggerHeader[0x20];
        fixed byte NpadGcTriggerState[0x18 * 17];
        fixed byte padding2[0xC38];
    }
}