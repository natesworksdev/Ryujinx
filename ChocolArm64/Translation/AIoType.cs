using System;

namespace ChocolArm64.Translation
{
    [Flags]
    internal enum AIoType
    {
        Arg,
        Fields,
        Flag,
        Int,
        Float,
        Vector
    }
}