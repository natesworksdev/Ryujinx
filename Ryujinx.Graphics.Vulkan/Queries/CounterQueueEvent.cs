using Ryujinx.Graphics.GAL;
using System;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan.Queries
{
    class CounterQueueEvent : ICounterEvent
    {
        public event EventHandler<ulong> OnResult;

        public CounterType Type { get; }
        public bool ClearCounter { get; private set; }

        public bool Disposed { get; private set; }
        public bool Invalid { get; set; }

        public ulong DrawIndex { get; }

        private CounterQueue _queue;
        private BufferedQuery _counter;

        private readonly object _lock = new object();

        public CounterQueueEvent(CounterQueue queue, CounterType type, ulong drawIndex)
        {
            _queue = queue;

            _counter = queue.GetQueryObject();
            Type = type;

            DrawIndex = drawIndex;

            _counter.Begin();
        }

        public Auto<DisposableBuffer> GetBuffer()
        {
            return _counter.GetBuffer();
        }

        internal void Clear()
        {
            _counter.Reset();
            ClearCounter = true;
        }

        internal void Complete(bool withResult)
        {
            _counter.End(withResult);
        }

        internal bool TryConsume(ref ulong result, bool block, AutoResetEvent wakeSignal = null)
        {
            lock (_lock)
            {
                if (Disposed)
                {
                    return true;
                }

                if (ClearCounter)
                {
                    result = 0;
                }

                long queryResult;

                if (block)
                {
                    queryResult = _counter.AwaitResult(wakeSignal);
                }
                else
                {
                    if (!_counter.TryGetResult(out queryResult))
                    {
                        return false;
                    }
                }

                result += (ulong)queryResult;

                OnResult?.Invoke(this, result);

                Dispose(); // Return the our resources to the pool.

                return true;
            }
        }

        public void Flush()
        {
            if (Disposed)
            {
                return;
            }

            // Tell the queue to process all events up to this one.
            _queue.FlushTo(this);
        }

        public void Dispose()
        {
            Disposed = true;
            _queue.ReturnQueryObject(_counter);
        }
    }
}
