using System.Diagnostics;

namespace Ryujinx.Common
{
    public static class PerformanceCounter
    {
        /// <summary>
        /// Gets the number of milliseconds elapsed since the system started.
        /// </summary>
        public static long ElapsedTicks
        {
            get
            {
                long timestamp = Stopwatch.GetTimestamp();

                return (long)(timestamp * (1000.0f / Stopwatch.Frequency));
            }
        }
    }
}
