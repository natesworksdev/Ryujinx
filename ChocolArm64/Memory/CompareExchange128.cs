using System;
using System.Runtime.InteropServices;

namespace ChocolArm64.Memory
{
    static class CompareExchange128
    {
        private struct Int128
        {
            public ulong Low  { get; }
            public ulong High { get; }

            public Int128(ulong low, ulong high)
            {
                Low  = low;
                High = high;
            }
        }

        private delegate Int128 InterlockedCompareExchange(IntPtr address, Int128 expected, Int128 desired);

        private static InterlockedCompareExchange _interlockedCompareExchange;

        static CompareExchange128()
        {
            //TODO: Also check if cmpxchg16b is supported on cpu flags.
            if (RuntimeInformation.OSArchitecture != Architecture.X64)
            {
                throw new PlatformNotSupportedException();
            }

            byte[] interlockedCompareExchange128Code;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                interlockedCompareExchange128Code = new byte[]
                {
                    0x53,                         // push rbx
                    0x49, 0x8B, 0x00,             // mov  rax, [r8]
                    0x49, 0x8B, 0x19,             // mov  rbx, [r9]
                    0x49, 0x89, 0xCA,             // mov  r10, rcx
                    0x49, 0x89, 0xD3,             // mov  r11, rdx
                    0x49, 0x8B, 0x49, 0x08,       // mov  rcx, [r9+8]
                    0x49, 0x8B, 0x50, 0x08,       // mov  rdx, [r8+8]
                    0xF0, 0x49, 0x0F, 0xC7, 0x0B, // lock cmpxchg16b [r11]
                    0x49, 0x89, 0x02,             // mov  [r10], rax
                    0x4C, 0x89, 0xD0,             // mov  rax, r10
                    0x49, 0x89, 0x52, 0x08,       // mov  [r10+8], rdx
                    0x5B,                         // pop  rbx
                    0xC3                          // ret
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                interlockedCompareExchange128Code = new byte[]
                {
                        0x53,                         // push rbx
                        0x49, 0x89, 0xd1,             // mov  r9,rdx
                        0x48, 0x89, 0xcb,             // mov  rbx,rcx
                        0x48, 0x89, 0xf0,             // mov  rax,rsi
                        0x4c, 0x89, 0xca,             // mov  rdx,r9
                        0x4c, 0x89, 0xc1,             // mov  rcx,r8
                        0xf0, 0x48, 0x0f, 0xc7, 0x0f, // lock cmpxchg16b [rdi]
                        0x5b,                         // pop  rbx
                        0xc3                          // ret
                };
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            ulong codeLength = (ulong)interlockedCompareExchange128Code.Length;

            IntPtr funcPtr = MemoryAlloc.Allocate(codeLength);

            unsafe
            {
                fixed (byte* codePtr = interlockedCompareExchange128Code)
                {
                    byte* dest = (byte*)funcPtr;

                    long size = (long)codeLength;

                    Buffer.MemoryCopy(codePtr, dest, size, size);
                }
            }

            MemoryAlloc.Reprotect(funcPtr, codeLength, MemoryProtection.Execute);

            _interlockedCompareExchange = Marshal.GetDelegateForFunctionPointer<InterlockedCompareExchange>(funcPtr);
        }

        public static bool InterlockedCompareExchange128(
            IntPtr address,
            ulong  expectedLow,
            ulong  expectedHigh,
            ulong  desiredLow,
            ulong  desiredHigh)
        {
            Int128 expected = new Int128(expectedLow, expectedHigh);
            Int128 desired  = new Int128(desiredLow,  desiredHigh);

            Int128 old = _interlockedCompareExchange(address, expected, desired);

            return old.Low == expected.Low && old.High == expected.High;
        }

        public static void InterlockedRead128(IntPtr address, out ulong low, out ulong high)
        {
            Int128 zero = new Int128(0, 0);

            Int128 old = _interlockedCompareExchange(address, zero, zero);

            low  = old.Low;
            high = old.High;
        }
    }
}