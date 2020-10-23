using System;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    [Flags]
    enum HelperFunctionsMask
    {
        AtomicMinMaxS32Shared  = 1 << 0,
        AtomicMinMaxS32Storage = 1 << 1,
        Bindless               = 1 << 2,
        MultiplyHighS32        = 1 << 3,
        MultiplyHighU32        = 1 << 4,
        Shuffle                = 1 << 5,
        ShuffleDown            = 1 << 6,
        ShuffleUp              = 1 << 7,
        ShuffleXor             = 1 << 8,
        SwizzleAdd             = 1 << 9
    }
}