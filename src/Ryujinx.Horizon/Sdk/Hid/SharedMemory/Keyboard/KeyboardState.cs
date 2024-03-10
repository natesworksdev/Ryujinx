using Ryujinx.Horizon.Sdk.Hid.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Hid.SharedMemory.Keyboard
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct KeyboardState : ISampledDataStruct
    {
        public ulong SamplingNumber;
        public KeyboardModifier Modifiers;
        public KeyboardKey Keys;
    }
}
