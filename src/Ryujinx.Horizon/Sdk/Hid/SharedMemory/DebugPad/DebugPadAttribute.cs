using System;

namespace Ryujinx.Horizon.Sdk.Hid.SharedMemory.DebugPad
{
    [Flags]
    enum DebugPadAttribute : uint
    {
        None = 0,
        Connected = 1 << 0,
    }
}
