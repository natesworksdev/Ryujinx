using System;

namespace ARMeilleure.State
{
    [Flags]
    public enum FPCR : uint
    {
        Ufe = 1u << 11,
        Fz  = 1u << 24,
        Dn  = 1u << 25,
        Ahp = 1u << 26,

        Mask = 0x07F79F00u
    }
}
