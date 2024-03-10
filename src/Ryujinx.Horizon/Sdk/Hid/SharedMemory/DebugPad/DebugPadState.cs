using Ryujinx.Horizon.Sdk.Hid.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Hid.SharedMemory.DebugPad
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DebugPadState : ISampledDataStruct
    {
        public ulong SamplingNumber;
        public DebugPadAttribute Attributes;
        public DebugPadButton Buttons;
        public AnalogStickState AnalogStickR;
        public AnalogStickState AnalogStickL;
    }
}
