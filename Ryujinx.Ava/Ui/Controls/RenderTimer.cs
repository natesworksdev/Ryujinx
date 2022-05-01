using Avalonia.Rendering;
using System;
using System.Threading;
using System.Timers;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class RenderTimer : IRenderTimer, IDisposable
    {
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
                --_subscriberCount;

                _tick -= value;
            }
        }

        private Thread _tickThread;

        private Action<TimeSpan> _tick;
        private int _subscriberCount;

        private bool _isRunning;
        private bool _tickNow;

        public void Start()
        {
            if (_tickThread == null)
            {
                _tickThread = new Thread(RunTick);
                _tickThread.Name = "RenderTimerTickThread";
                _tickThread.IsBackground = true;
                _isRunning = true;
                _tickThread.Start();
            }
        }

        public void RunTick()
        {
            while (_isRunning)
            {
                Thread.Sleep(1);
                _tick?.Invoke(TimeSpan.FromMilliseconds(Environment.TickCount));
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _tickThread.Join();
        }
    }
}
