using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Core.OsHle.Services.Nv
{
    class NvSyncPt
    {
        private int m_CounterMin;
        private int m_CounterMax;

        public int CounterMin => m_CounterMin;
        public int CounterMax => m_CounterMax;

        public bool IsEvent { get; set; }

        private ConcurrentDictionary<EventWaitHandle, int> Waiters;

        public NvSyncPt()
        {
            Waiters = new ConcurrentDictionary<EventWaitHandle, int>();
        }

        public int Increment()
        {
            Interlocked.Increment(ref m_CounterMax);

            return IncrementMin();
        }

        public int IncrementMin()
        {
            int Value = Interlocked.Increment(ref m_CounterMin);

            WakeUpWaiters(Value);

            return Value;
        }

        public int IncrementMax()
        {
            return Interlocked.Increment(ref m_CounterMax);
        }

        public void AddWaiter(int Threshold, EventWaitHandle WaitEvent)
        {
            if (!Waiters.TryAdd(WaitEvent, Threshold))
            {
                throw new InvalidOperationException();
            }
        }

        public bool RemoveWaiter(EventWaitHandle WaitEvent)
        {
            return Waiters.TryRemove(WaitEvent, out _);
        }

        private void WakeUpWaiters(int NewValue)
        {
            foreach (KeyValuePair<EventWaitHandle, int> KV in Waiters)
            {
                if (MinCompare(NewValue, m_CounterMax, KV.Value))
                {
                    KV.Key.Set();

                    Waiters.TryRemove(KV.Key, out _);
                }
            }
        }

        public bool MinCompare(int Threshold)
        {
            return MinCompare(m_CounterMin, m_CounterMax, Threshold);
        }

        private bool MinCompare(int Min, int Max, int Threshold)
        {
            int MinDiff = Min - Threshold;
            int MaxDiff = Max - Threshold;

            if (IsEvent)
            {
                return MinDiff >= 0;
            }
            else
            {
                return (uint)MaxDiff >= (uint)MinDiff;
            }
        }
    }
}