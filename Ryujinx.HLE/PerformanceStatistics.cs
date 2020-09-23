using System.Diagnostics;
using System.Timers;

namespace Ryujinx.HLE
{
    public class PerformanceStatistics
    {
        private const double FrameRateWeight = 0.5;

        private const int FrameTypeSystem = 0;
        private const int FrameTypeGame   = 1;

        private const int PercentTypeFifo = 0;

        private double[] _averageFrameRate;
        private double[] _accumulatedFrameTime;
        private double[] _previousFrameTime;

        private double[] _averagePercent;
        private double[] _accumulatedPercent;
        private double[] _percentLastEndTime;
        private double[] _percentStartTime;

        private long[] _framesRendered;
        private long[] _percentCount;

        private object[] _frameLock;
        private object[] _percentLock;

        private double _ticksToSeconds;

        private Stopwatch _executionTime;

        private Timer _resetTimer;

        public PerformanceStatistics()
        {
            _averageFrameRate     = new double[2];
            _accumulatedFrameTime = new double[2];
            _previousFrameTime    = new double[2];

            _averagePercent     = new double[1];
            _accumulatedPercent = new double[1];
            _percentLastEndTime = new double[1];
            _percentStartTime   = new double[1];

            _framesRendered = new long[2];
            _percentCount   = new long[1];

            _frameLock   = new object[] { new object(), new object() };
            _percentLock = new object[] { new object() };

            _executionTime = new Stopwatch();

            _executionTime.Start();

            _resetTimer = new Timer(1000);

            _resetTimer.Elapsed += ResetTimerElapsed;

            _resetTimer.AutoReset = true;

            _resetTimer.Start();

            _ticksToSeconds = 1.0 / Stopwatch.Frequency;
        }

        private void ResetTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CalculateAverageFrameRate(FrameTypeSystem);
            CalculateAverageFrameRate(FrameTypeGame);
            CalculateAveragePercent(PercentTypeFifo);
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

        private void CalculateAveragePercent(int percentType)
        {
            double percent = 0;

            if (_accumulatedPercent[percentType] > 0)
            {
                percent = _accumulatedPercent[percentType] / _percentCount[percentType];

                if (double.IsNaN(percent))
                {
                    percent = 0;
                }
            }

            lock (_percentLock[percentType])
            {
                _averagePercent[percentType] = percent;

                _percentCount[percentType] = 0;

                _accumulatedPercent[percentType] = 0;
            }
        }

        private double LinearInterpolate(double old, double New)
        {
            return old * (1.0 - FrameRateWeight) + New * FrameRateWeight;
        }

        public void RecordSystemFrameTime()
        {
            RecordFrameTime(FrameTypeSystem);
        }

        public void RecordGameFrameTime()
        {
            RecordFrameTime(FrameTypeGame);
        }

        public void RecordFifoStart()
        {
            StartPercentTime(PercentTypeFifo);
        }

        public void RecordFifoEnd()
        {
            EndPercentTime(PercentTypeFifo);
        }

        private void StartPercentTime(int percentType)
        {
            double currentTime = _executionTime.ElapsedTicks * _ticksToSeconds;

            _percentStartTime[percentType] = currentTime;
        }

        private void EndPercentTime(int percentType)
        {
            double currentTime = _executionTime.ElapsedTicks * _ticksToSeconds;

            double elapsedTime = currentTime - _percentLastEndTime[percentType];
            double elapsedActiveTime = currentTime - _percentStartTime[percentType];

            double percentActive = (elapsedActiveTime / elapsedTime) * 100;

            lock (_percentLock[percentType])
            {
                _accumulatedPercent[percentType] += percentActive;

                _percentCount[percentType]++;
            }

            _percentLastEndTime[percentType] = currentTime;
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

        public double GetFifoPercent()
        {
            return _averagePercent[PercentTypeFifo];
        }
    }
}
