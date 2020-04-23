using Ryujinx.Debugger.Profiler;
using System.Diagnostics;
using System.Timers;

namespace Ryujinx.HLE
{
    public class PerformanceStatistics
    {
        private const double FrameRateWeight = 0.5;

        private const int FrameTypeSystem = 0;
        private const int FrameTypeGame   = 1;

        private static readonly double _ticksToSeconds = 1.0 / Stopwatch.Frequency;

        private readonly double[] _averageFrameRate;
        private readonly double[] _accumulatedFrameTime;
        private readonly double[] _previousFrameTime;

        private readonly long[] _framesRendered;

        private readonly object[] _frameLock;

        private readonly Stopwatch _executionTime;

        private readonly Timer _resetTimer;

        public PerformanceStatistics()
        {
            _averageFrameRate     = new double[2];
            _accumulatedFrameTime = new double[2];
            _previousFrameTime    = new double[2];

            _framesRendered = new long[2];

            _frameLock = new [] { new object(), new object() };

            _executionTime = new Stopwatch();

            _executionTime.Start();

            _resetTimer = new Timer(1000);

            _resetTimer.Elapsed += ResetTimerElapsed;

            _resetTimer.AutoReset = true;

            _resetTimer.Start();
        }

        private void ResetTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CalculateAverageFrameRate(FrameTypeSystem);
            CalculateAverageFrameRate(FrameTypeGame);
        }

        private void CalculateAverageFrameRate(int frameType)
        {
            double frameRate = 0;

            if (_accumulatedFrameTime[frameType] > 0)
            {
                frameRate = _framesRendered[frameType] / _accumulatedFrameTime[frameType];
            }

            lock (_frameLock[frameType])
            {
                _averageFrameRate[frameType] = LinearInterpolate(_averageFrameRate[frameType], frameRate);

                _framesRendered[frameType] = 0;

                _accumulatedFrameTime[frameType] = 0;
            }
        }

        private double LinearInterpolate(double old, double New)
        {
            return old * (1.0 - FrameRateWeight) + New * FrameRateWeight;
        }

        public void RecordSystemFrameTime()
        {
            RecordFrameTime(FrameTypeSystem);
            Profile.FlagTime(TimingFlagType.SystemFrame);
        }

        public void RecordGameFrameTime()
        {
            RecordFrameTime(FrameTypeGame);
            Profile.FlagTime(TimingFlagType.FrameSwap);
        }

        private void RecordFrameTime(int frameType)
        {
            double currentFrameTime = _executionTime.ElapsedTicks * _ticksToSeconds;

            double elapsedFrameTime = currentFrameTime - _previousFrameTime[frameType];

            _previousFrameTime[frameType] = currentFrameTime;

            lock (_frameLock[frameType])
            {
                _accumulatedFrameTime[frameType] += elapsedFrameTime;

                _framesRendered[frameType]++;
            }
        }

        public double GetSystemFrameRate()
        {
            return _averageFrameRate[FrameTypeSystem];
        }

        public double GetGameFrameRate()
        {
            return _averageFrameRate[FrameTypeGame];
        }
    }
}
