using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryujinx.Core
{
    public static class PerformanceStatistics
    {
        static Stopwatch ExecutionTime = new Stopwatch();

        static long LastFrameEnded;
        static long CurrentFrameEnded;
        static long CurrentFrameStart;

        public static double PreviousFrameTime;
        public static double CurrentFrameTime;
        public static double FrameRate { get => 1000f/(CurrentFrameTime/1000);}
        public static long RenderedFrames;
        public static long ElapsedMilliseconds { get => ExecutionTime.ElapsedMilliseconds; }
        public static long ElapsedMicroseconds { get => (long)
                (((double)ExecutionTime.ElapsedTicks / Stopwatch.Frequency) * 1000000); }
        public static long ElapsedNanoseconds  { get => (long)
                (((double)ExecutionTime.ElapsedTicks / Stopwatch.Frequency) * 1000000000); }

        static PerformanceStatistics()
        {
            ExecutionTime.Start();
        }
        
        public static void StartFrame()
        {
            PreviousFrameTime = CurrentFrameTime;
            LastFrameEnded = CurrentFrameEnded;
            CurrentFrameStart = ElapsedMicroseconds;
        }

        public static void EndFrame()
        {
            CurrentFrameEnded = ElapsedMicroseconds;
            CurrentFrameTime = CurrentFrameEnded - CurrentFrameStart;
            RenderedFrames++;
        }
    }
}
