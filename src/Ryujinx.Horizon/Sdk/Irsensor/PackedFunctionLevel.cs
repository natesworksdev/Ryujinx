using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Irsensor
{
    [StructLayout(LayoutKind.Sequential, Size = 0x4)]
    struct PackedFunctionLevel
    {
        public byte IrSensorFunctionLevel;
    }
}
