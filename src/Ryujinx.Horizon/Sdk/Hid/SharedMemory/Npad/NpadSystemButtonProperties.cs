using System;

namespace Ryujinx.Horizon.Sdk.Hid.SharedMemory.Npad
{
    [Flags]
    enum NpadSystemButtonProperties : uint
    {
        None = 0,
        IsUnintendedHomeButtonInputProtectionEnabled = 1 << 0,
    }
}
