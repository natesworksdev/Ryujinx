using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Utilities
{
    public class PerformanceProfiler
    {
        private Stopwatch stopwatch;
        private long totalTicks = 0;
        private long totalMilliseconds = 0;
        private long totalCount = 0;
        private long maxTicks = 0;
        private long maxMs = 0;

        public PerformanceProfiler()
        {
            stopwatch = new Stopwatch();
        }

        public void StartCapture()
        {
            stopwatch.Restart();
        }

        public void EndCapture()
        {
            long ticks = stopwatch.ElapsedTicks;
            long ms = stopwatch.ElapsedMilliseconds;

            totalTicks += ticks;
            totalMilliseconds += ms;
            totalCount++;

            maxTicks = Math.Max(maxTicks, ticks);
            maxMs = Math.Max(maxMs, ms);
        }

        public String GetMetrics()
        {
            if (totalCount == 0) return "StartCapture and EndCapture must be called to output metrics";

            long avgTicks = totalTicks / totalCount;
            long avgMs = totalMilliseconds / totalCount;

            return $"[Performance Report] ->  Avg Ticks: {avgTicks} | Max Ticks: {maxTicks} | Avg Ms: {avgMs} | Max Ms: {maxMs}";
        }
    }
}
