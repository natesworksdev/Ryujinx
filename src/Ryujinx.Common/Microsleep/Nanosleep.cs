using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Microsleep
{
    public static partial class Nanosleep
    {
        private const long X64NanosleepBias = 50000; // 0.05ms
        private const long ARMNanosleepBias = 5000; // 0.005ms

        public static long Bias { get; }

        static Nanosleep()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                Bias = X64NanosleepBias;
            }
            else
            {
                Bias = ARMNanosleepBias;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Timespec
        {
            public long tv_sec;  // Seconds
            public long tv_nsec; // Nanoseconds
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int nanosleep(ref Timespec req, ref Timespec rem);

        private static Timespec GetTimespecFromNanoseconds(ulong nanoseconds)
        {
            return new Timespec
            {
                tv_sec = (long)(nanoseconds / 1_000_000_000),
                tv_nsec = (long)(nanoseconds % 1_000_000_000)
            };
        }

        public static void Sleep(long nanoseconds)
        {
            nanoseconds -= Bias;

            if (nanoseconds >= 0)
            {
                Timespec req = GetTimespecFromNanoseconds((ulong)nanoseconds);
                Timespec rem = new();

                nanosleep(ref req, ref rem);
            }
        }
    }
}
