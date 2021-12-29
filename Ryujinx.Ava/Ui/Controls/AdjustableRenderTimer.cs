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
        public int FrameRate
        {
            get => _frameRate; set
            {
                _frameRate = value;
                _intervalTicks = Stopwatch.Frequency / _frameRate;
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
        private int _frameRate;

        private long _intervalTicks;
        private bool _isRunning;

        public AdjustableRenderTimer(int framerate)
        {
            _frameRate = framerate;
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
            _timingThread = new Thread(Run);
            _timingThread.Name = "RenderTimerThread";
            _timingThread.IsBackground = true;
            _isRunning = true;
            _timingThread.Start();
        }

        private void Run()
        {
            long lastElapsed = 0;
            while (_isRunning)
            {
                var elapsed = _timer.ElapsedTicks;

                if (elapsed > lastElapsed + _intervalTicks)
                {
                    _tick?.Invoke(TimeSpan.FromMilliseconds(Environment.TickCount));

                    lastElapsed = elapsed;
                }
                else
                {
                    Thread.Yield();
                }
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _isRunning = false;
            _timingThread.Join();
        }

        public void Dispose()
        {
            _timer.Stop();
            _isRunning = false;
        }
    }
}
