using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Microsleep
{
    public static partial class Nanosleep
    {
        private const long NanosleepBias = 50000; // 0.05ms

        private const long StrictNanosleepBias = 150000; // 0.15ms (todo: better)

        public static long Bias { get; }
        public static long StrictBias { get; }

        static Nanosleep()
        {
            // TODO: Operating systems can apply a bias depending on how long the requested timeout is.

            Bias = NanosleepBias;

            StrictBias = StrictNanosleepBias;
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

        public static void SleepBefore(long nanoseconds)
        {
            // Stricter bias to ensure we wake before the timepoint.
            nanoseconds -= StrictBias;

            if (nanoseconds >= 0)
            {
                Timespec req = GetTimespecFromNanoseconds((ulong)nanoseconds);
                Timespec rem = new();

                nanosleep(ref req, ref rem);
            }
        }
    }
}
