using System;

namespace Ryujinx.Common.Configuration.Hid
{
    [Flags]
    public enum KeyModifier
    {
        None = 0,
        Alt = 1 << 0,
        Control = 1 << 1,
        Shift = 1 << 2,
        Meta = 1 << 3
    }
}