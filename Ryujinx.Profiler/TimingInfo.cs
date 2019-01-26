namespace Ryujinx.Profiler
{
    public struct TimingInfo
    {
        public long BeginTime, LastTime, TotalTime, Instant;
        public int Count, InstantCount;
        public long AverageTime
        {
            get => (Count == 0) ? -1 : TotalTime / Count;
        }
    }
}
