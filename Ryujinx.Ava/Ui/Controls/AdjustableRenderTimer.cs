using Avalonia.Rendering;
using SPB.Graphics;
using SPB.Platform;
using SPB.Windowing;
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
        private Thread _tickThread;
        private Thread _vsyncThread;
        private Stopwatch _timer;

        private Action<TimeSpan> _tick;
        private int _subscriberCount;
        private uint _targetFrameRate;

        private long _intervalTicks;
        private bool _isRunning;
        private bool _isSuspended;

        private AutoResetEvent _resetEvent;
        private ManualResetEvent _pauseEvent;
        private bool _useVSync;

        public AdjustableRenderTimer(uint framerate)
        {
            _targetFrameRate = framerate;
            _timer = new Stopwatch();
            _resetEvent = new AutoResetEvent(true);
            _pauseEvent = new ManualResetEvent(true);
            _intervalTicks = Stopwatch.Frequency / framerate;
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
            if (_tickThread == null)
            {
                _tickThread = new Thread(RunTick);
                _tickThread.Name = "RenderTimerTickThread";
                _tickThread.IsBackground = true;
                _isRunning = true;
                _tickThread.Start();
            }

            _isSuspended = false;
        }

        public void RunTick()
        {
            while (_isRunning)
            {
                _resetEvent.WaitOne();
                _tick?.Invoke(TimeSpan.FromMilliseconds(_timer.ElapsedTicks * 1000 / Stopwatch.Frequency));
            }
        }

        private void Run()
        {
            long lastElapsed = 0;
            while (_isRunning)
            {
                _pauseEvent.WaitOne();
                var elapsed = _timer.ElapsedTicks;
                var nextElapsed = lastElapsed + _intervalTicks;

                if ((elapsed > nextElapsed) && !_isSuspended)
                {
                    TickNow();

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

        public void TickNow()
        {
            _resetEvent.Set();
        }

        public void Stop()
        {
            _timer.Stop();
            _isSuspended = true;
        }

        public void SwitchToVSyncTiming()
        {
            if (_vsyncThread != null && _vsyncThread.IsAlive)
            {
                return;
            }

            _pauseEvent.Reset();
            _vsyncThread = new Thread(VSyncRunner);
            _vsyncThread.IsBackground = true;
            _vsyncThread.Name = "VSyncRenderThread";
            _vsyncThread.Start();
        }

        public void VSyncRunner()
        {
            var vsyncWindow = PlatformHelper.CreateOpenGLWindow(FramebufferFormat.Default, 0, 0, 100, 100);
            vsyncWindow.Hide();

            var vsyncContext = PlatformHelper.CreateOpenGLContext(FramebufferFormat.Default, 3, 0, SPB.Graphics.OpenGL.OpenGLContextFlags.OffScreen);
            vsyncContext.Initialize(vsyncWindow);

            vsyncContext.MakeCurrent(vsyncWindow);

            _useVSync = true;

            while (_useVSync)
            {
                TickNow();
                vsyncWindow.SwapBuffers();
            }

            vsyncContext.MakeCurrent(null);
            vsyncContext.Dispose();
            vsyncWindow.Dispose();
        }

        public void SwitchToEventTiming()
        {
            _useVSync = false;
            _pauseEvent.Set();
        }

        public void Dispose()
        {
            _pauseEvent.Set();
            _useVSync = false;
            _timer.Stop();
            _isRunning = false;
            _timingThread.Join();
            _resetEvent.Set();
            _vsyncThread?.Join();
            _tickThread.Join();
            _resetEvent.Dispose();
        }
    }
}
