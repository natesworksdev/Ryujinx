using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    /// <summary>
    /// Access to Linux/MacOS nanosleep, with platform specific bias to improve precision.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("android")]
    [SupportedOSPlatform("ios")]
    public static partial class Nanosleep
    {
        private const long LinuxBaseNanosleepBias = 50000; // 0.05ms
        
        // Penalty for max allowed sleep duration
        private const long LinuxNanosleepAccuracyPenaltyThreshold = 200000; // 0.2ms
        private const long LinuxNanosleepAccuracyPenalty = 30000; // 0.03ms

        // Penalty for base sleep duration
        private const long LinuxNanosleepBasePenaltyThreshold = 500000; // 0.5ms
        private const long LinuxNanosleepBasePenalty = 30000; // 0.03ms
        private const long LinuxNanosleepPenaltyPerMillisecond = 18000; // 0.018ms
        private const long LinuxNanosleepPenaltyCap = 18000; // 0.018ms

        private const long LinuxStrictBiasOffset = 150_000; // 0.15ms

        // Nanosleep duration is biased depending on the requested timeout on MacOS.
        // These match the results when measuring on an M1 processor at AboveNormal priority.
        private const long MacosBaseNanosleepBias = 5000; // 0.005ms
        private const long MacosBiasPerMillisecond = 140000; // 0.14ms
        private const long MacosBiasMaxNanoseconds = 20_000_000; // 20ms
        private const long MacosStrictBiasOffset = 150_000; // 0.15ms

        public static long Bias { get; }

        private static bool NoBias;

        /// <summary>
        /// Get bias for a given nanosecond timeout.
        /// Some platforms calculate their bias differently, this method can be used to counteract it.
        /// </summary>
        /// <param name="timeoutNs">Nanosecond timeout</param>
        /// <returns>Bias in nanoseconds</returns>
        public static long GetBias(long timeoutNs)
        {
            if (NoBias)
            {
                return 0;
            }
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                long biasNs = Math.Min(timeoutNs, MacosBiasMaxNanoseconds);
                return MacosBaseNanosleepBias + biasNs * MacosBiasPerMillisecond / 1_000_000;
            }
            else
            {
                long bias = LinuxBaseNanosleepBias;
                
                if (timeoutNs > LinuxNanosleepBasePenaltyThreshold)
                {
                    long penalty = (timeoutNs - LinuxNanosleepBasePenaltyThreshold) * LinuxNanosleepPenaltyPerMillisecond / 1_000_000;
                    bias += LinuxNanosleepBasePenalty + Math.Min(LinuxNanosleepPenaltyCap, penalty);
                }

                return bias;
            }
        }

        /// <summary>
        /// Get a stricter bias for a given nanosecond timeout,
        /// which can improve the chances the sleep completes before the timeout.
        /// Some platforms calculate their bias differently, this method can be used to counteract it.
        /// </summary>
        /// <param name="timeoutNs">Nanosecond timeout</param>
        /// <returns>Strict bias in nanoseconds</returns>
        public static long GetStrictBias(long timeoutNs)
        {
            if (NoBias)
            {
                return 0;
            }
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                return GetBias(timeoutNs) + MacosStrictBiasOffset;
            }
            else
            {
                long bias = GetBias(timeoutNs) + LinuxStrictBiasOffset;

                if (timeoutNs > LinuxNanosleepAccuracyPenaltyThreshold)
                {
                    bias += LinuxNanosleepAccuracyPenalty;
                }

                return bias;
            }
        }

        static Nanosleep()
        {
            Bias = GetBias(0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Timespec
        {
            public long tv_sec;  // Seconds
            public long tv_nsec; // Nanoseconds
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int nanosleep(ref Timespec req, ref Timespec rem);

        /// <summary>
        /// Convert a timeout in nanoseconds to a timespec for nanosleep.
        /// </summary>
        /// <param name="nanoseconds">Timeout in nanoseconds</param>
        /// <returns>Timespec for nanosleep</returns>
        private static Timespec GetTimespecFromNanoseconds(ulong nanoseconds)
        {
            return new Timespec
            {
                tv_sec = (long)(nanoseconds / 1_000_000_000),
                tv_nsec = (long)(nanoseconds % 1_000_000_000)
            };
        }

        /// <summary>
        /// Sleep for approximately a given time period in nanoseconds.
        /// </summary>
        /// <param name="nanoseconds">Time to sleep for in nanoseconds</param>
        public static void Sleep(long nanoseconds)
        {
            nanoseconds -= GetBias(nanoseconds);

            if (nanoseconds >= 0)
            {
                Timespec req = GetTimespecFromNanoseconds((ulong)nanoseconds);
                Timespec rem = new();

                nanosleep(ref req, ref rem);
            }
        }

        /// <summary>
        /// Sleep for at most a given time period in nanoseconds.
        /// Uses a stricter bias to wake before the requested duration.
        /// </summary>
        /// <remarks>
        /// Due to OS scheduling behaviour, this timeframe may still be missed.
        /// </remarks>
        /// <param name="nanoseconds">Maximum allowed time for sleep</param>
        public static void SleepBefore(long nanoseconds)
        {
            // Stricter bias to ensure we wake before the timepoint.
            nanoseconds -= GetStrictBias(nanoseconds);

            if (nanoseconds >= 0)
            {
                Timespec req = GetTimespecFromNanoseconds((ulong)nanoseconds);
                Timespec rem = new();

                nanosleep(ref req, ref rem);
            }
        }

        // TODO: remove all this stuff?

        private static float TicksToMs(long ticks)
        {
            return ticks / (float)PerformanceCounter.TicksPerMillisecond;
        }

        private static void TestOne(long targetNanoseconds, int count)
        {
            var ticks = new List<long>();

            for (int i = 0; i < count; i++)
            {
                long before = Stopwatch.GetTimestamp();
                Nanosleep.Sleep(targetNanoseconds);
                long after = Stopwatch.GetTimestamp();
                ticks.Add(after - before);
            }

            long targetTicks = (long)((targetNanoseconds / 1_000_000f) * PerformanceCounter.TicksPerMillisecond);

            ticks.Sort();

            long avg = (long)ticks.Average() - targetTicks;

            long low = ticks[count / 6] - targetTicks;
            long median = ticks[count / 2] - targetTicks;
            long high = ticks[(count * 5) / 6] - targetTicks;

            //Console.WriteLine($"{targetNanoseconds / 1_000_000f}ms: {TicksToMs(avg)}ms avg, ({TicksToMs(low)} low, {TicksToMs(high)})");

            Console.WriteLine($"{targetNanoseconds / 1_000_000f}, {TicksToMs(avg)}, {TicksToMs(low)}, {TicksToMs(high)}");
        }

        public static void NanosleepTest()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            Console.WriteLine($"TargetNs, Average error (ms), Low 1/6 error (ms), High 1/6 error (ms)");
            NoBias = true;

            for (int i = 0; i < 20; i++)
            {
                long ns = (1_000_000L) * i / 20;

                TestOne(ns, 1000);
            }

            for (int i = 0; i < 12; i++)
            {
                long ns = (1_000_000L) + (250_000L) * i;

                TestOne(ns, 1000);
            }

            TestOne(19_000_000, 100);
        }
    }
}
