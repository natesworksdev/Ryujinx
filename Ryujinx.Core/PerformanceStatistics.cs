using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryujinx.Core
{
    public static class PerformanceStatistics
    {
        static Stopwatch ExecutionTime = new Stopwatch();

        static long CurrentGameFrameEnded;
        static long CurrentSystemFrameEnded;
        static long CurrentSystemFrameStart;
        static long LastGameFrameEnded;
        static long LastSystemFrameEnded;

        public static double CurrentGameFrameTime;
        public static double CurrentSystemFrameTime;
        public static double PreviousGameFrameTime;
        public static double PreviousSystemFrameTime;
        public static double GameFrameRate => 1000f / (CurrentSystemFrameTime / 1000); 
        public static double SystemFrameRate => 1000f/(CurrentSystemFrameTime/1000);
        public static long SystemFramesRendered;
        public static long GameFramesRendered;
        public static long ElapsedMilliseconds { get => ExecutionTime.ElapsedMilliseconds; }
        public static long ElapsedMicroseconds { get => (long)
                (((double)ExecutionTime.ElapsedTicks / Stopwatch.Frequency) * 1000000); }
        public static long ElapsedNanoseconds  { get => (long)
                (((double)ExecutionTime.ElapsedTicks / Stopwatch.Frequency) * 1000000000); }

        static PerformanceStatistics()
        {
            ExecutionTime.Start();
        }
        
        public static void StartSystemFrame()
        {
            PreviousSystemFrameTime = CurrentSystemFrameTime;
            LastSystemFrameEnded = CurrentSystemFrameEnded;
            CurrentSystemFrameStart = ElapsedMicroseconds;
        }

        public static void EndSystemFrame()
        {
            CurrentSystemFrameEnded = ElapsedMicroseconds;
            CurrentSystemFrameTime = CurrentSystemFrameEnded - CurrentSystemFrameStart;
            SystemFramesRendered++;
        }

        public static void RecordGameFrameTime()
        {
            CurrentGameFrameEnded = ElapsedMicroseconds;
            CurrentGameFrameTime = CurrentGameFrameEnded - LastGameFrameEnded;
            PreviousGameFrameTime = CurrentGameFrameTime;
            LastGameFrameEnded = CurrentGameFrameEnded;
            GameFramesRendered++;
        }
    }
}
