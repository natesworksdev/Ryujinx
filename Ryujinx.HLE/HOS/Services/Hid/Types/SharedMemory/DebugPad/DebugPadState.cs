using Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.DebugPad
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    struct DebugPadState : ISampledDataStruct
    {
        public ulong SamplingNumber;
        public DebugPadAttribute Attributes;
        public DebugPadButton Buttons;
        public AnalogStickState AnalogStickR;
        public AnalogStickState AnalogStickL;
    }
}
