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

        public static X86Register GetIntReturnRegister()
        {
            return X86Register.Rax;
        }
    }
}