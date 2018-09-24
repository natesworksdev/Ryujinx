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
        struct Entry
        {
            public TKey               Params;
            public LinkedList<TValue> Resources;

            public Entry(TKey Params, LinkedList<TValue> Resources)
            {
                this.Params    = Params;
                this.Resources = Resources;
            }
        }

        struct DeletionEntry
        {
            public TValue             Resource;
            public LinkedList<TValue> Siblings;

            public DeletionEntry(TValue Resource, LinkedList<TValue> Siblings)
            {
                this.Resource = Resource;
                this.Siblings = Siblings;
            }
        }

        private const int MaxTimeDelta      = 5 * 60000;
        private const int MaxRemovalsPerRun = 10;

        private LinkedList<Entry> Entries;

        private Queue<DeletionEntry> SortedCache;

        private Func<TKey, TValue> CreateValueCallback;
        private Action<TValue>     DeleteValueCallback;

        public ResourcePool(
            Func<TKey, TValue> CreateValueCallback,
            Action<TValue>     DeleteValueCallback)
        {
            this.CreateValueCallback = CreateValueCallback;
            this.DeleteValueCallback = DeleteValueCallback;

            Entries = new LinkedList<Entry>();

            SortedCache = new Queue<DeletionEntry>();
        }

        public TValue CreateOrRecycle(TKey Params)
        {
            LinkedList<TValue> Siblings = GetOrAddSiblings(Params);

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

            SortedCache.Enqueue(new DeletionEntry(Resource, Siblings));

            return Resource;
        }

        public void ReleaseMemory()
        {
            int Timestamp = Environment.TickCount;

            for (int Count = 0; Count < MaxRemovalsPerRun; Count++)
            {
                if (!SortedCache.TryDequeue(out DeletionEntry DeletionEntry))
                {
                    break;
                }

                TValue              Resource = DeletionEntry.Resource;
                LinkedList <TValue> Siblings = DeletionEntry.Siblings;

                if (!Resource.IsUsed)
                {
                    int TimeDelta = RingDelta(Resource.Timestamp, Timestamp);

                    if ((uint)TimeDelta > MaxTimeDelta)
                    {
                        if (!Siblings.Remove(Resource))
                        {
                            throw new InvalidOperationException();
                        }

                        DeleteValueCallback.Invoke(Resource);

                        continue;
                    }
                }

                SortedCache.Enqueue(DeletionEntry);
            }
        }

        private LinkedList<TValue> GetOrAddSiblings(TKey Params)
        {
            LinkedListNode<Entry> Node = Entries.First;

            while (Node != null)
            {
                Entry Entry = Node.Value;

                if (Entry.Params.IsCompatible(Params))
                {
                    Entries.Remove(Node);

                    Entries.AddFirst(Entry);

                    return Entry.Resources;
                }

                Node = Node.Next;
            }

            LinkedList<TValue> Siblings = new LinkedList<TValue>();

            Entries.AddFirst(new Entry(Params, Siblings));

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