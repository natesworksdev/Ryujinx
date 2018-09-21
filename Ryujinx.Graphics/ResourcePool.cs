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

        public void UpdateStamp()
        {
            Timestamp = Environment.TickCount;
        }

        public void MarkAsUsed()
        {
            UpdateStamp();

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

        private List<(TKey, List<TValue>)> Entries;

        private Queue<(TValue, List<TValue>)> SortedCache;

        private CreateValue CreateValueCallback;
        private DeleteValue DeleteValueCallback;

        public ResourcePool(CreateValue CreateValueCallback, DeleteValue DeleteValueCallback)
        {
            this.CreateValueCallback = CreateValueCallback;
            this.DeleteValueCallback = DeleteValueCallback;

            Entries = new List<(TKey, List<TValue>)>();

            SortedCache = new Queue<(TValue, List<TValue>)>();
        }

        public TValue CreateOrRecycle(TKey Params)
        {
            List<TValue> Family = GetOrAddEntry(Params);

            foreach (TValue RecycledValue in Family)
            {
                if (!RecycledValue.IsUsed)
                {
                    RecycledValue.MarkAsUsed();

                    return RecycledValue;
                }
            }

            TValue Resource = CreateValueCallback(Params);

            Resource.MarkAsUsed();

            Family.Add(Resource);

            SortedCache.Enqueue((Resource, Family));

            return Resource;
        }

        public void ReleaseMemory()
        {
            int Timestamp = Environment.TickCount;

            for (int Count = 0; Count < MaxRemovalsPerRun; Count++)
            {
                if (!SortedCache.TryDequeue(out (TValue Resource, List<TValue> Family) Tuple))
                {
                    break;
                }

                TValue Resource = Tuple.Resource;

                List<TValue> Family = Tuple.Family;

                if (!Resource.IsUsed)
                {
                    int TimeDelta = RingDelta(Resource.Timestamp, Timestamp);

                    if ((uint)TimeDelta > MaxTimeDelta)
                    {
                        if (!Family.Remove(Resource))
                        {
                            throw new InvalidOperationException();
                        }

                        DeleteValueCallback(Resource);

                        continue;
                    }
                }

                SortedCache.Enqueue((Resource, Family));
            }
        }

        private List<TValue> GetOrAddEntry(TKey Params)
        {
            foreach ((TKey MyParams, List<TValue> Resources) in Entries)
            {
                if (MyParams.IsCompatible(Params))
                {
                    return Resources;
                }
            }

            List<TValue> Family = new List<TValue>();

            Entries.Add((Params, Family));

            return Family;
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