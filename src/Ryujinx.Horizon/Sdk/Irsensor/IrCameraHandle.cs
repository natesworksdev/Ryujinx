using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Irsensor
{
    [StructLayout(LayoutKind.Sequential, Size = 0x4)]
    struct IrCameraHandle
    {
        public byte PlayerNumber;
        public byte DeviceType;
        public ushort Reserved;
    }
}
