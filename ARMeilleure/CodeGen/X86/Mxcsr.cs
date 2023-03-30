using System;

namespace ARMeilleure.CodeGen.X86
{
    [Flags]
    public enum Mxcsr
    {
        Ftz = 1 << 15, // Flush To Zero.
        Um = 1 << 11, // Underflow Mask.
        Dm = 1 << 8,  // Denormal Mask.
        Daz = 1 << 6   // Denormals Are Zero.
    }
}
