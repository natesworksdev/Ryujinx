namespace Ryujinx.Profiler
{
    public struct TimingInfo
    {
        public long BeginTime, LastTime, TotalTime, Count;
        public long AverageTime
        {
            get => (Count == 0) ? -1 : TotalTime / Count;
        }
    }
}
