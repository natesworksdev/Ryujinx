using System;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    [Flags]
    enum HelperFunctionsMask
    {
        AtomicMinMaxS32Shared  = 1 << 0,
        AtomicMinMaxS32Storage = 1 << 1,
        GlobalMemory           = 1 << 2,
        MultiplyHighS32        = 1 << 3,
        MultiplyHighU32        = 1 << 4,
        Shuffle                = 1 << 5,
        ShuffleDown            = 1 << 6,
        ShuffleUp              = 1 << 7,
        ShuffleXor             = 1 << 8,
        StoreSharedSmallInt    = 1 << 9,
        StoreStorageSmallInt   = 1 << 10,
        SwizzleAdd             = 1 << 11,
        FSI                    = 1 << 12
    }
}