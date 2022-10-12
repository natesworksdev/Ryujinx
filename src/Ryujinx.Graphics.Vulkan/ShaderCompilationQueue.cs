using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan
{
    class ShaderCompilationQueue
    {
        private const int MaxParallelCompilations = 8;
        private const int MaxThreadStackSize = 1 * 1024 * 1024; // MB

        private struct Request
        {
            public readonly ulong Id;
            public readonly Action Callback;

            public Request(ulong id, Action callback)
            {
                Id = id;
                Callback = callback;
            }
        }

        private readonly Thread[] _workerThreads;
        private readonly CancellationTokenSource _cts;
        private readonly BlockingCollection<Request>[] _queues;
        private readonly ulong[] _finishedIds;
        private ulong _currentId;
        private int _currentQueueIndex;

        public ShaderCompilationQueue()
        {
            _workerThreads = new Thread[MaxParallelCompilations];
            _queues = new BlockingCollection<Request>[MaxParallelCompilations];
            _finishedIds = new ulong[MaxParallelCompilations];

            _cts = new CancellationTokenSource();

            for (int i = 0; i < MaxParallelCompilations; i++)
            {
                _queues[i] = new BlockingCollection<Request>();

                Thread thread = new Thread(DoWork, MaxThreadStackSize) { Name = $"BackgroundShaderCompiler.{i}" };
                thread.IsBackground = true;
                thread.Start(i);

                _workerThreads[i] = thread;
            }
        }

        private void DoWork(object threadId)
        {
            int queueIndex = (int)threadId;

            try
            {
                var queue = _queues[queueIndex];

                foreach (var request in queue.GetConsumingEnumerable(_cts.Token))
                {
                    request.Callback();

                    lock (queue)
                    {
                        _finishedIds[queueIndex] = request.Id;

                        Monitor.PulseAll(queue);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public ShaderCompilationRequest Add(Action callback)
        {
            ulong newId = Interlocked.Increment(ref _currentId);

            // Let's keep rotating between the queues to increase the chances
            // that the selected queue thread is currently idle.
            int queueIndex = Interlocked.Increment(ref _currentQueueIndex) % MaxParallelCompilations;

            _queues[queueIndex].Add(new Request(newId, callback));

            return new ShaderCompilationRequest(this, queueIndex, newId);
        }

        public void Wait(int queueIndex, ulong id)
        {
            var queue = _queues[queueIndex];

            lock (queue)
            {
                while (_finishedIds[queueIndex] < id)
                {
                    Monitor.Wait(queue);
                }
            }
        }

        public bool IsCompleted(int queueIndex, ulong id)
        {
            var queue = _queues[queueIndex];

            lock (queue)
            {
                return _finishedIds[queueIndex] >= id;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < MaxParallelCompilations; i++)
            {
                _queues[i].CompleteAdding();
            }

            _cts.Cancel();

            for (int i = 0; i < MaxParallelCompilations; i++)
            {
                _workerThreads[i].Join();

                _queues[i].Dispose();
            }

            _cts.Dispose();
        }
    }
}