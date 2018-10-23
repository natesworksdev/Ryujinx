using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OglCachedResource<T>
    {
        public delegate void DeleteValue(T value);

        private const int MaxTimeDelta      = 5 * 60000;
        private const int MaxRemovalsPerRun = 10;

        private struct CacheBucket
        {
            public T Value { get; private set; }

            public LinkedListNode<long> Node { get; private set; }

            public long DataSize { get; private set; }

            public int Timestamp { get; private set; }

            public CacheBucket(T value, long dataSize, LinkedListNode<long> node)
            {
                this.Value    = value;
                this.DataSize = dataSize;
                this.Node     = node;

                Timestamp = Environment.TickCount;
            }
        }

        private Dictionary<long, CacheBucket> _cache;

        private LinkedList<long> _sortedCache;

        private DeleteValue _deleteValueCallback;

        private Queue<T> _deletePending;

        private bool _locked;

        public OglCachedResource(DeleteValue deleteValueCallback)
        {
            if (deleteValueCallback == null)
            {
                throw new ArgumentNullException(nameof(deleteValueCallback));
            }

            this._deleteValueCallback = deleteValueCallback;

            _cache = new Dictionary<long, CacheBucket>();

            _sortedCache = new LinkedList<long>();

            _deletePending = new Queue<T>();
        }

        public void Lock()
        {
            _locked = true;
        }

        public void Unlock()
        {
            _locked = false;

            while (_deletePending.TryDequeue(out T value))
            {
                _deleteValueCallback(value);
            }

            ClearCacheIfNeeded();
        }

        public void AddOrUpdate(long key, T value, long size)
        {
            if (!_locked)
            {
                ClearCacheIfNeeded();
            }

            LinkedListNode<long> node = _sortedCache.AddLast(key);

            CacheBucket newBucket = new CacheBucket(value, size, node);

            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                if (_locked)
                {
                    _deletePending.Enqueue(bucket.Value);
                }
                else
                {
                    _deleteValueCallback(bucket.Value);
                }

                _sortedCache.Remove(bucket.Node);

                _cache[key] = newBucket;
            }
            else
            {
                _cache.Add(key, newBucket);
            }
        }

        public bool TryGetValue(long key, out T value)
        {
            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                value = bucket.Value;

                _sortedCache.Remove(bucket.Node);

                LinkedListNode<long> node = _sortedCache.AddLast(key);

                _cache[key] = new CacheBucket(value, bucket.DataSize, node);

                return true;
            }

            value = default(T);

            return false;
        }

        public bool TryGetSize(long key, out long size)
        {
            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                size = bucket.DataSize;

                return true;
            }

            size = 0;

            return false;
        }

        private void ClearCacheIfNeeded()
        {
            int timestamp = Environment.TickCount;

            int count = 0;

            while (count++ < MaxRemovalsPerRun)
            {
                LinkedListNode<long> node = _sortedCache.First;

                if (node == null)
                {
                    break;
                }

                CacheBucket bucket = _cache[node.Value];

                int timeDelta = RingDelta(bucket.Timestamp, timestamp);

                if ((uint)timeDelta <= (uint)MaxTimeDelta)
                {
                    break;
                }

                _sortedCache.Remove(node);

                _cache.Remove(node.Value);

                _deleteValueCallback(bucket.Value);
            }
        }

        private int RingDelta(int old, int New)
        {
            if ((uint)New < (uint)old)
            {
                return New + (~old + 1);
            }
            else
            {
                return New - old;
            }
        }
    }
}