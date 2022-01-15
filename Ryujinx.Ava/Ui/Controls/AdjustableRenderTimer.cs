using Avalonia.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class AdjustableRenderTimer : IRenderTimer, IDisposable
    {
        public uint TargetFrameRate
        {
            get => _targetFrameRate; set
            {
                _targetFrameRate = value;
                _intervalTicks = Stopwatch.Frequency / _targetFrameRate;
            }
        }

        public event Action<TimeSpan> Tick
        {
            add
            {
                _tick += value;

                if (_subscriberCount++ == 0)
                {
                    Start();
                }
            }

            remove
            {
                if (--_subscriberCount == 0)
                {
                    Stop();
                }

                _tick -= value;
            }
        }

        private Thread _timingThread;
        private Stopwatch _timer;

        private Action<TimeSpan> _tick;
        private int _subscriberCount;
        private uint _targetFrameRate;

        private long _intervalTicks;
        private bool _isRunning;
        private bool _isSuspended;

        public AdjustableRenderTimer(uint framerate)
        {
            _targetFrameRate = framerate;
            _timer = new Stopwatch();
            _intervalTicks = Stopwatch.Frequency / framerate;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _tick?.Invoke(TimeSpan.FromMilliseconds(Environment.TickCount));
        }

        public void Start()
        {
            _timer.Start();
            if (_timingThread == null)
            {
                _timingThread = new Thread(Run);
                _timingThread.Name = "RenderTimerThread";
                _timingThread.IsBackground = true;
                _isRunning = true;
                _timingThread.Start();
            }

            _isSuspended = false;
        }

        private void Run()
        {
            long lastElapsed = 0;
            while (_isRunning)
            {
                var elapsed = _timer.ElapsedTicks;
                var nextElapsed = lastElapsed + _intervalTicks;

                if ((elapsed > nextElapsed) && !_isSuspended)
                {
                    _tick?.Invoke(TimeSpan.FromMilliseconds(Environment.TickCount));

                    lastElapsed = elapsed;
                }
                else
                {
                    var ticksTillNext = nextElapsed - elapsed;
                    var msTillNext = ticksTillNext * 1000f / Stopwatch.Frequency;
                    if((int)(msTillNext / 2) > 0)
                    {
                        Thread.Sleep((int)(msTillNext / 2));
                    }
                }
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _isSuspended = true;
        }

        public void Dispose()
        {
            _timer.Stop();
            _isRunning = false;
            _timingThread.Join();
        }
    }
}
