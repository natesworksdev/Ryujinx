using System;

namespace ARMeilleure.CodeGen.X86
{
    static class CallingConvention
    {
        private const int RegistersMask = 0xffff;

        public static int GetIntAvailableRegisters()
        {
            return RegistersMask & ~(1 << (int)X86Register.Rsp);
        }

        public static int GetVecAvailableRegisters()
        {
            return RegistersMask;
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

        public static int GetVecCallerSavedRegisters()
        {
            return (1 << (int)X86Register.Xmm0) |
                   (1 << (int)X86Register.Xmm1) |
                   (1 << (int)X86Register.Xmm2) |
                   (1 << (int)X86Register.Xmm3) |
                   (1 << (int)X86Register.Xmm4) |
                   (1 << (int)X86Register.Xmm5);
        }

        public static int GetIntCalleeSavedRegisters()
        {
            return GetIntCallerSavedRegisters() ^ RegistersMask;
        }

        public static int GetVecCalleeSavedRegisters()
        {
            return GetVecCallerSavedRegisters() ^ RegistersMask;
        }

        public static int GetArgumentsOnRegsCount()
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

        public static X86Register GetVecArgumentRegister(int index)
        {
            switch (index)
            {
                case 0: return X86Register.Xmm0;
                case 1: return X86Register.Xmm1;
                case 2: return X86Register.Xmm2;
                case 3: return X86Register.Xmm3;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public static X86Register GetIntReturnRegister()
        {
            return X86Register.Rax;
        }

        public static X86Register GetVecReturnRegister()
        {
            return X86Register.Xmm0;
        }
    }
}