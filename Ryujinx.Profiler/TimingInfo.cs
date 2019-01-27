namespace Ryujinx.Profiler
{
    public struct TimingInfo
    {
        // Timestamps
        public long BeginTime;
        public long LastTime;
        public long TotalTime;
        public long Instant;

        // Measurement counts
        public int  Count;
        public int  InstantCount;
        
        // Work out average
        public long AverageTime => (Count == 0) ? -1 : TotalTime / Count;
    }
}
