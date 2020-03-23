namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidTouchScreenEntryTouch
    {
        public ulong SampleTimestamp;
        public uint _Padding;
        public uint TouchIndex;
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint Angle;
        public uint _Padding2;
    }
}