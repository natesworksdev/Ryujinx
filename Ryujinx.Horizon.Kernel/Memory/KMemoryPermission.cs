using System;

namespace Ryujinx.Horizon.Kernel.Memory
{
    [Flags]
    public enum KMemoryPermission
    {
        None = 0,
        Mask = 0xff,

        Read    = 1 << 0,
        Write   = 1 << 1,
        Execute = 1 << 2,

        ReadAndWrite   = Read | Write,
        ReadAndExecute = Read | Execute,

        DontCare = 1 << 28
    }
}