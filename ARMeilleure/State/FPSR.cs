using System;

namespace ARMeilleure.State
{
    [Flags]
    public enum FPSR : uint
    {
        Ufc = 1u << 3,

        Mask = 0xF800009Fu
    }
}
