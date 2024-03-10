using Ryujinx.Horizon.Sdk.Hid.SharedMemory.TouchScreen;

namespace Ryujinx.Horizon.Sdk.Hid
{
    public struct TouchPoint
    {
        public TouchAttribute Attribute;
        public uint X;
        public uint Y;
        public uint DiameterX;
        public uint DiameterY;
        public uint Angle;
    }
}
