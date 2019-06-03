using System;

namespace ARMeilleure.CodeGen.X86
{
    static class CallingConvention
    {
        public static int GetIntAvailableRegisters()
        {
            int mask = 0xffff;

            mask &= ~(1 << (int)X86Register.Rbp);
            mask &= ~(1 << (int)X86Register.Rsp);

            return mask;
        }

        public static int GetIntCallerSavedRegisters()
        {
            return (1 << (int)X86Register.Rax) |
                   (1 << (int)X86Register.Rdx) |
                   (1 << (int)X86Register.Rcx) |
                   (1 << (int)X86Register.R8)  |
                   (1 << (int)X86Register.R9)  |
                   (1 << (int)X86Register.R10) |
                   (1 << (int)X86Register.R11);
        }

        public static int GetIntCalleeSavedRegisters()
        {
            return (1 << (int)X86Register.Rbx) |
                   (1 << (int)X86Register.Rbp) |
                   (1 << (int)X86Register.Rdi) |
                   (1 << (int)X86Register.Rsi) |
                   (1 << (int)X86Register.Rsp) |
                   (1 << (int)X86Register.R12) |
                   (1 << (int)X86Register.R13) |
                   (1 << (int)X86Register.R14) |
                   (1 << (int)X86Register.R15);
        }

        public static int GetIntArgumentsOnRegsCount()
        {
            return 4;
        }

        public static X86Register GetIntArgumentRegister(int index)
        {
            switch (index)
            {
                case 0: return X86Register.Rcx;
                case 1: return X86Register.Rdx;
                case 2: return X86Register.R8;
                case 3: return X86Register.R9;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public static X86Register GetIntReturnRegister()
        {
            return X86Register.Rax;
        }
    }
}