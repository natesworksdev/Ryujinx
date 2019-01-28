using System;
using System.Collections.Generic;

namespace Ryujinx.Profiler
{
    public struct Timestamp
    {
        public long BeginTime;
        public long EndTime;
    }

    public class TimingInfo
    {
        // Timestamps
        public long TotalTime;
        public long Instant;

        // Measurement counts
        public int  Count;
        public int  InstantCount;
        
        // Work out average
        public long AverageTime => (Count == 0) ? -1 : TotalTime / Count;

        // Timestamp collection
        public List<Timestamp> Timestamps;
        private readonly object timestampLock = new object();
        private Timestamp currentTimestamp;

        // Depth of current timer,
        // each begin call increments and each end call decrements
        private int depth;
        

        public TimingInfo()
        {
            Timestamps = new List<Timestamp>();
            depth      = 0;
        }

        public void Begin(long beginTime)
        {
            lock (timestampLock)
            {
                // Finish current timestamp if already running
                if (depth > 0)
                {
                    EndUnsafe(beginTime);
                }

                BeginUnsafe(beginTime);
                depth++;
            }
        }

        private void BeginUnsafe(long beginTime)
        {
            currentTimestamp.BeginTime = beginTime;
            currentTimestamp.EndTime   = -1;
        }

        public void End(long endTime)
        {
            lock (timestampLock)
            {
                depth--;

                if (depth < 0)
                {
                    throw new Exception("Timing info end called without corresponding begin");
                }

                EndUnsafe(endTime);

                // Still have others using this timing info so recreate start for them
                if (depth > 0)
                {
                    BeginUnsafe(endTime);
                }
            }
        }

        private void EndUnsafe(long endTime)
        {
            currentTimestamp.EndTime = endTime;
            Timestamps.Add(currentTimestamp);

            var delta  = currentTimestamp.EndTime - currentTimestamp.BeginTime;
            TotalTime += delta;
            Instant   += delta;

            Count++;
            InstantCount++;
        }

        // Remove any timestamps before given timestamp to free memory
        public void Cleanup(long before)
        {
            lock (timestampLock)
            {
                int toRemove = 0;

                for (int i = 0; i < Timestamps.Count; i++)
                {
                    if (Timestamps[i].EndTime < before)
                    {
                        toRemove++;
                    }
                    else
                    {
                        // Assume timestamps are in chronological order so no more need to be removed
                        break;
                    }
                }

                if (toRemove > 0)
                    Timestamps.RemoveRange(0, toRemove);
            }
        }
    }
}
