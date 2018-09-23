using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public interface ICompatible<T>
    {
        bool IsCompatible(T Other);
    }

    public class Resource
    {
        public int Timestamp { get; private set; }

        public bool IsUsed { get; private set; }

        public void UpdateTimestamp()
        {
            Timestamp = Environment.TickCount;
        }

        public void MarkAsUsed()
        {
            UpdateTimestamp();

            IsUsed = true;
        }

        public void MarkAsUnused()
        {
            IsUsed = false;
        }
    }

    class ResourcePool<TKey, TValue>
        where TKey   : ICompatible<TKey>
        where TValue : Resource
    {
        private const int MaxTimeDelta      = 5 * 60000;
        private const int MaxRemovalsPerRun = 10;

        public delegate TValue CreateValue(TKey Params);

        public delegate void DeleteValue(TValue Resource);

        private LinkedList<(TKey, LinkedList<TValue>)> Entries;

        private Queue<(TValue, LinkedList<TValue>)> SortedCache;

        private CreateValue CreateValueCallback;
        private DeleteValue DeleteValueCallback;

        public ResourcePool(CreateValue CreateValueCallback, DeleteValue DeleteValueCallback)
        {
            this.CreateValueCallback = CreateValueCallback;
            this.DeleteValueCallback = DeleteValueCallback;

            Entries = new LinkedList<(TKey, LinkedList<TValue>)>();

            SortedCache = new Queue<(TValue, LinkedList<TValue>)>();
        }

        public TValue CreateOrRecycle(TKey Params)
        {
            LinkedList<TValue> Siblings = GetOrAddEntry(Params);

            foreach (TValue RecycledValue in Siblings)
            {
                if (!RecycledValue.IsUsed)
                {
                    RecycledValue.MarkAsUsed();

                    return RecycledValue;
                }
            }

            TValue Resource = CreateValueCallback(Params);

            Resource.MarkAsUsed();

            Siblings.AddLast(Resource);

            SortedCache.Enqueue((Resource, Siblings));

            return Resource;
        }

        public void ReleaseMemory()
        {
            int Timestamp = Environment.TickCount;

            for (int Count = 0; Count < MaxRemovalsPerRun; Count++)
            {
                if (!SortedCache.TryDequeue(out (TValue Resource, LinkedList<TValue> Siblings) Tuple))
                {
                    break;
                }

                (TValue Resource, LinkedList <TValue> Siblings) = Tuple;

                if (!Resource.IsUsed)
                {
                    int TimeDelta = RingDelta(Resource.Timestamp, Timestamp);

                    if ((uint)TimeDelta > MaxTimeDelta)
                    {
                        if (!Siblings.Remove(Resource))
                        {
                            throw new InvalidOperationException();
                        }

                        DeleteValueCallback(Resource);

                        continue;
                    }
                }

                SortedCache.Enqueue((Resource, Siblings));
            }
        }

        private LinkedList<TValue> GetOrAddEntry(TKey Params)
        {
            foreach ((TKey MyParams, LinkedList<TValue> Resources) in Entries)
            {
                if (MyParams.IsCompatible(Params))
                {
                    return Resources;
                }
            }

            LinkedList<TValue> Siblings = new LinkedList<TValue>();

            Entries.AddFirst((Params, Siblings));

            return Siblings;
        }

        private static int RingDelta(int Old, int New)
        {
            if ((uint)New < (uint)Old)
            {
                return New + (~Old + 1);
            }
            else
            {
                return New - Old;
            }
        }
    }
}