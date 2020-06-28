using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Common
{
    public sealed class AsyncWorkQueue<T> : IDisposable
    {
        protected Thread _workerThread;

        protected CancellationTokenSource _cts;

        protected Action<T> _workerAction;

        protected BlockingCollection<T> _queue;

        public bool IsCancellationRequested => _cts.IsCancellationRequested;

        public AsyncWorkQueue(Action<T> callback)
            : this(callback, new BlockingCollection<T>())
        { }

        public AsyncWorkQueue(Action<T> callback, BlockingCollection<T> collection)
        {
            _cts = new CancellationTokenSource();
            _queue = collection;
            _workerAction = callback;
            _workerThread = new Thread(DoWork);

            _workerThread.IsBackground = true;
            _workerThread.Start();
        }

        private void DoWork()
        {
            foreach (var item in _queue.GetConsumingEnumerable(_cts.Token))
            {
                _workerAction(item);
            }
        }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public void CancelAfter(int millisecondsDelay)
        {
            _cts.CancelAfter(millisecondsDelay);
        }

        public void CancelAfter(TimeSpan delay)
        {
            _cts.CancelAfter(delay);
        }

        public void Add(T workItem)
        {
            _queue.Add(workItem);
        }

        public void Add(T workItem, CancellationToken cancellationToken)
        {
            _queue.Add(workItem, cancellationToken);
        }

        public bool TryAdd(T workItem)
        {
            return _queue.TryAdd(workItem);
        }

        public bool TryAdd(T workItem, int millisecondsDelay)
        {
            return _queue.TryAdd(workItem, millisecondsDelay);
        }

        public bool TryAdd(T workItem, int millisecondsDelay, CancellationToken cancellationToken)
        {
            return _queue.TryAdd(workItem, millisecondsDelay, cancellationToken);
        }

        public bool TryAdd(T workItem, TimeSpan timeout)
        {
            return _queue.TryAdd(workItem, timeout);
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            _workerThread.Join();

            _queue.Dispose();
            _cts.Dispose();
        }
    }
}
