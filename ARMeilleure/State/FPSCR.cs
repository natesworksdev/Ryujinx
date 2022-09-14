using System;

namespace ARMeilleure.State
{
    [Flags]
    public enum FPSCR : uint
    {
        Mask = FPSR.Mask | FPCR.Mask
    }
}
