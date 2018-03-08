using System;

namespace ChocolArm64.Memory
{
    [Flags]
    public enum AMemoryPerm : byte
    {
        None    = 0,
        Read    = 1 << 0,
        Write   = 1 << 1,
        Execute = 1 << 2,
        RW      = Read | Write,
        RX      = Read | Execute
    }
}