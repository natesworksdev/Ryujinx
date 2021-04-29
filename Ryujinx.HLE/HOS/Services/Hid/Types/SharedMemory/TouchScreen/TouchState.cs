namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.TouchScreen
{
    struct TouchState
    {
        public ulong DeltaTime;
        public TouchAttribute Attribute;
        public uint FingerId;
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint RotationAngle;
        private uint _reserved;
    }
}
