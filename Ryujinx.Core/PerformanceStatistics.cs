using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryujinx.Core
{
    public class PerformanceStatistics
    {
        Stopwatch ExecutionTime = new Stopwatch();

        long CurrentGameFrameEnded;
        long CurrentSystemFrameEnded;
        long CurrentSystemFrameStart;
        long LastGameFrameEnded;
        long LastSystemFrameEnded;

        public double CurrentGameFrameTime;
        public double CurrentSystemFrameTime;
        public double PreviousGameFrameTime;
        public double PreviousSystemFrameTime;
        public double GameFrameRate => 1000f / (CurrentSystemFrameTime / 1000); 
        public double SystemFrameRate => 1000f/(CurrentSystemFrameTime/1000);
        public long SystemFramesRendered;
        public long GameFramesRendered;
        public long ElapsedMilliseconds { get => ExecutionTime.ElapsedMilliseconds; }
        public long ElapsedMicroseconds { get => (long)
                (((double)ExecutionTime.ElapsedTicks / Stopwatch.Frequency) * 1000000); }
        public long ElapsedNanoseconds  { get => (long)
                (((double)ExecutionTime.ElapsedTicks / Stopwatch.Frequency) * 1000000000); }

        public PerformanceStatistics()
        {
            ExecutionTime.Start();
        }
        
        public void StartSystemFrame()
        {
            PreviousSystemFrameTime = CurrentSystemFrameTime;
            LastSystemFrameEnded = CurrentSystemFrameEnded;
            CurrentSystemFrameStart = ElapsedMicroseconds;
        }

        public void EndSystemFrame()
        {
            CurrentSystemFrameEnded = ElapsedMicroseconds;
            CurrentSystemFrameTime = CurrentSystemFrameEnded - CurrentSystemFrameStart;
            SystemFramesRendered++;
        }

        public void RecordGameFrameTime()
        {
            CurrentGameFrameEnded = ElapsedMicroseconds;
            CurrentGameFrameTime = CurrentGameFrameEnded - LastGameFrameEnded;
            PreviousGameFrameTime = CurrentGameFrameTime;
            LastGameFrameEnded = CurrentGameFrameEnded;
            GameFramesRendered++;
        }
    }
}
