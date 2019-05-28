using ARMeilleure.Memory;
using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation
{
    class TranslatedCache
    {
        public TranslatedFunction CreateFunction(byte[] code)
        {
            IntPtr funcPtr = MapCodeAsExecutable(code);

            GuestFunction func = Marshal.GetDelegateForFunctionPointer<GuestFunction>(funcPtr);

            return new TranslatedFunction(func);
        }

        private static IntPtr MapCodeAsExecutable(byte[] code)
        {
            ulong codeLength = (ulong)code.Length;

            IntPtr funcPtr = MemoryManagement.Allocate(codeLength);

            unsafe
            {
                fixed (byte* codePtr = code)
                {
                    byte* dest = (byte*)funcPtr;

                    long size = (long)codeLength;

                    Buffer.MemoryCopy(codePtr, dest, size, size);
                }
            }

            MemoryManagement.Reprotect(funcPtr, codeLength, MemoryProtection.Execute);

            return funcPtr;
        }
    }
}