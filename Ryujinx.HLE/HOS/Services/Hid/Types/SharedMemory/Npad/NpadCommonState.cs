using Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Npad
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    struct NpadCommonState : ISampledDataStruct
    {
        public ulong SamplingNumber;
        public NpadButton Buttons;
        public AnalogStickState AnalogStickL;
        public AnalogStickState AnalogStickR;
        public NpadAttribute Attributes;
        private uint _reserved;
    }
}
