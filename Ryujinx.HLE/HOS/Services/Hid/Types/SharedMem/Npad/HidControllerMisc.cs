namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidControllerMisc
    {
        public DeviceType DeviceType;
        public uint _Padding;
        public DeviceFlags DeviceFlags;
        public uint UnintendedHomeButtonInputProtectionEnabled;
        public Array3<BatteryCharge> BatteryCharge;
        public fixed byte _Unk1[0x20];
        HidControllerMAC MacLeft;
        HidControllerMAC MacRight;
    }
}