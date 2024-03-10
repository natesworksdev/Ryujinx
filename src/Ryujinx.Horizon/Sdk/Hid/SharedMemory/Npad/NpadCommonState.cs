using Ryujinx.Horizon.Sdk.Hid.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Hid.SharedMemory.Npad
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NpadCommonState : ISampledDataStruct
    {
        public ulong SamplingNumber;
        public NpadButton Buttons;
        public AnalogStickState AnalogStickL;
        public AnalogStickState AnalogStickR;
        public NpadAttribute Attributes;
        private readonly uint _reserved;
    }
}
