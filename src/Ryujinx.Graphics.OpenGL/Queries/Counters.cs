using Ryujinx.Graphics.GAL;
using Silk.NET.OpenGL;
using System;

namespace Ryujinx.Graphics.OpenGL.Queries
{
    class Counters : IDisposable
    {
        private readonly CounterQueue[] _counterQueues;
        private readonly GL _api;

        public Counters(GL api)
        {
            int count = Enum.GetNames<CounterType>().Length;

            _counterQueues = new CounterQueue[count];
            _api = api;
        }

        public void Initialize()
        {
            for (int index = 0; index < _counterQueues.Length; index++)
            {
                CounterType type = (CounterType)index;
                _counterQueues[index] = new CounterQueue(_api, type);
            }
        }

        public CounterQueueEvent QueueReport(CounterType type, EventHandler<ulong> resultHandler, float divisor, ulong lastDrawIndex, bool hostReserved)
        {
            return _counterQueues[(int)type].QueueReport(_api, resultHandler, divisor, lastDrawIndex, hostReserved);
        }

        public void QueueReset(CounterType type)
        {
            _counterQueues[(int)type].QueueReset();
        }

        public void Update()
        {
            foreach (var queue in _counterQueues)
            {
                queue.Flush(false);
            }
        }

        public void Flush(CounterType type)
        {
            _counterQueues[(int)type].Flush(true);
        }

        public void Dispose()
        {
            foreach (var queue in _counterQueues)
            {
                queue.Dispose();
            }
        }
    }
}
