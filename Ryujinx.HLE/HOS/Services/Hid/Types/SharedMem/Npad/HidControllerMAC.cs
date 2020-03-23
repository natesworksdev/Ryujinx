namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidControllerMAC
    {
        public ulong Timestamp;
        public fixed byte Mac[0x8];
        public ulong _Unk;
        public ulong Timestamp2;
    }
}