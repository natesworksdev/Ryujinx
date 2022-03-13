using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ryujinx.Common.Cache
{
    class PartitionHashTable<T>
    {
        private struct Entry
        {
            public readonly uint Hash;
            public readonly int OwnSize;
            public readonly byte[] Data;
            public T Item;

            public bool IsPartial => OwnSize != 0;

            public Entry(uint hash, byte[] ownerData, int ownSize)
            {
                Hash = hash;
                OwnSize = ownSize;
                Data = ownerData;
                Item = default;
            }

            public Entry(uint hash, byte[] data, T item)
            {
                Hash = hash;
                OwnSize = 0;
                Data = data;
                Item = item;
            }

            public ReadOnlySpan<byte> GetData()
            {
                if (OwnSize != 0)
                {
                    return new ReadOnlySpan<byte>(Data).Slice(0, OwnSize);
                }

                return Data;
            }
        }

        private struct Bucket
        {
            public Entry InlineEntry;
            public List<Entry> MoreEntries;
        }

        private Bucket[] _buckets;
        private int _count;

        public int Count => _count;

        public PartitionHashTable()
        {
            _buckets = Array.Empty<Bucket>();
        }

        public T GetOrAdd(byte[] data, uint dataHash, T item)
        {
            if (TryFindItem(dataHash, data, out T existingItem))
            {
                return existingItem;
            }

            Entry entry = new Entry(dataHash, data, item);

            AddToBucket(dataHash, ref entry);

            return item;
        }

        public bool Add(byte[] data, uint dataHash, T item)
        {
            if (TryFindItem(dataHash, data, out _))
            {
                return false;
            }

            Entry entry = new Entry(dataHash, data, item);

            AddToBucket(dataHash, ref entry);

            return true;
        }

        public bool AddPartial(byte[] ownerData, int ownSize)
        {
            ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(ownerData).Slice(0, ownSize);

            return AddPartial(ownerData, HashState.CalcHash(data), ownSize);
        }

        public bool AddPartial(byte[] ownerData, uint dataHash, int ownSize)
        {
            ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(ownerData).Slice(0, ownSize);

            if (TryFindItem(dataHash, data, out _))
            {
                return false;
            }

            Entry entry = new Entry(dataHash, ownerData, ownSize);

            AddToBucket(dataHash, ref entry);

            return true;
        }

        private void AddToBucket(uint dataHash, ref Entry entry)
        {
            int pow2Count = GetPow2Count(++_count);
            if (pow2Count != _buckets.Length)
            {
                Rebuild(pow2Count);
            }

            ref Bucket bucket = ref GetBucketForHash(dataHash);

            AddToBucket(ref bucket, ref entry);
        }

        private void AddToBucket(ref Bucket bucket, ref Entry entry)
        {
            if (bucket.InlineEntry.Data == null)
            {
                bucket.InlineEntry = entry;
            }
            else
            {
                (bucket.MoreEntries ??= new List<Entry>()).Add(entry);
            }
        }

        public void FillPartials(PartitionHashTable<T> newTable, int newEntrySize)
        {
            for (int i = 0; i < _buckets.Length; i++)
            {
                ref Bucket bucket = ref _buckets[i];
                ref Entry inlineEntry = ref bucket.InlineEntry;

                if (inlineEntry.Data != null)
                {
                    if (!inlineEntry.IsPartial)
                    {
                        newTable.AddPartial(inlineEntry.Data, newEntrySize);
                    }

                    if (bucket.MoreEntries != null)
                    {
                        foreach (Entry entry in bucket.MoreEntries)
                        {
                            if (entry.IsPartial)
                            {
                                continue;
                            }

                            newTable.AddPartial(entry.Data, newEntrySize);
                        }
                    }
                }
            }
        }

        private bool TryFindItem(uint dataHash, ReadOnlySpan<byte> data, out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            ref Bucket bucket = ref GetBucketForHash(dataHash);

            if (bucket.InlineEntry.Data != null)
            {
                if (bucket.InlineEntry.Hash == dataHash && bucket.InlineEntry.GetData().SequenceEqual(data))
                {
                    item = bucket.InlineEntry.Item;
                    return true;
                }

                if (bucket.MoreEntries != null)
                {
                    foreach (Entry entry in bucket.MoreEntries)
                    {
                        if (entry.Hash == dataHash && entry.GetData().SequenceEqual(data))
                        {
                            item = entry.Item;
                            return true;
                        }
                    }
                }
            }

            item = default;
            return false;
        }

        public enum SearchResult
        {
            NotFound,
            FoundPartial,
            FoundFull
        }

        public SearchResult TryFindItem(ref SmartDataAccessor dataAccessor, int size, ref T item, ref byte[] data)
        {
            if (_count == 0)
            {
                return SearchResult.NotFound;
            }

            ReadOnlySpan<byte> dataSpan = dataAccessor.GetSpanAndHash(size, out uint dataHash);

            if (dataSpan.Length != size)
            {
                return SearchResult.NotFound;
            }

            ref Bucket bucket = ref GetBucketForHash(dataHash);

            if (bucket.InlineEntry.Data != null)
            {
                if (bucket.InlineEntry.Hash == dataHash && bucket.InlineEntry.GetData().SequenceEqual(dataSpan))
                {
                    item = bucket.InlineEntry.Item;
                    data = bucket.InlineEntry.Data;
                    return bucket.InlineEntry.IsPartial ? SearchResult.FoundPartial : SearchResult.FoundFull;
                }

                if (bucket.MoreEntries != null)
                {
                    foreach (Entry entry in bucket.MoreEntries)
                    {
                        if (entry.Hash == dataHash && entry.GetData().SequenceEqual(dataSpan))
                        {
                            item = entry.Item;
                            data = entry.Data;
                            return entry.IsPartial ? SearchResult.FoundPartial : SearchResult.FoundFull;
                        }
                    }
                }
            }

            return SearchResult.NotFound;
        }

        private void Rebuild(int newPow2Count)
        {
            Bucket[] newBuckets = new Bucket[newPow2Count];

            uint mask = (uint)newPow2Count - 1;

            for (int i = 0; i < _buckets.Length; i++)
            {
                ref Bucket bucket = ref _buckets[i];

                if (bucket.InlineEntry.Data != null)
                {
                    AddToBucket(ref newBuckets[(int)(bucket.InlineEntry.Hash & mask)], ref bucket.InlineEntry);

                    if (bucket.MoreEntries != null)
                    {
                        foreach (Entry entry in bucket.MoreEntries)
                        {
                            Entry entryCopy = entry;
                            AddToBucket(ref newBuckets[(int)(entry.Hash & mask)], ref entryCopy);
                        }
                    }
                }
            }

            _buckets = newBuckets;
        }

        private ref Bucket GetBucketForHash(uint hash)
        {
            int index = (int)(hash & (_buckets.Length - 1));

            return ref _buckets[index];
        }

        private static int GetPow2Count(int count)
        {
            return 1 << BitOperations.Log2((uint)count);
        }
    }
}
