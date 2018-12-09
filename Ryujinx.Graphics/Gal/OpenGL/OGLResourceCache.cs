using Ryujinx.Common;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLResourceCache<TPoolKey, TValue>
    {
        private const int MinTimeDelta      = 5 * 60000;
        private const int MaxRemovalsPerRun = 10;

        private const int DefaultMinTimeForPoolTransfer = 2500;

        private class CacheBucket
        {
            public long     Key     { get; private set; }
            public TPoolKey PoolKey { get; private set; }
            public TValue   Value   { get; private set; }

            public LinkedListNode<CacheBucket> CacheNode { get; private set; }
            public LinkedListNode<CacheBucket> PoolNode  { get; private set; }

            private Queue<Action> _deleteDeps;

            public int Size { get; private set; }

            public long Timestamp { get; private set; }

            public bool Orphan { get; private set; }

            public CacheBucket(long key, TPoolKey poolKey, TValue value, int size)
            {
                Key     = key;
                PoolKey = poolKey;
                Value   = value;
                Size    = size;

                _deleteDeps = new Queue<Action>();
            }

            public void UpdateCacheNode(LinkedListNode<CacheBucket> newNode)
            {
                Timestamp = PerformanceCounter.ElapsedMilliseconds;

                CacheNode = newNode;
            }

            public void UpdatePoolNode(LinkedListNode<CacheBucket> newNode)
            {
                PoolNode = newNode;
            }

            public void MarkAsOrphan()
            {
                Orphan = true;
            }

            public void AddDependency(Action deleteDep)
            {
                _deleteDeps.Enqueue(deleteDep);
            }

            public void DeleteAllDependencies()
            {
                while (_deleteDeps.TryDequeue(out Action deleteDep))
                {
                    deleteDep();
                }
            }
        }

        private Dictionary<long, CacheBucket> _cache;

        private Dictionary<TPoolKey, LinkedList<CacheBucket>> _pool;

        private LinkedList<CacheBucket> _sortedCache;

        private LinkedListNode<CacheBucket> _poolTransferNode;

        private Action<TValue> _deleteValueCallback;

        private Queue<TValue> _deletionPending;

        private bool _locked;

        private int _maxSize;
        private int _totalSize;
        private int _minTimeForPoolTransfer;

        public OGLResourceCache(
            Action<TValue> deleteValueCallback,
            int            maxSize,
            int            minTimeForPoolTransfer = DefaultMinTimeForPoolTransfer)
        {
            _maxSize = maxSize;

            _deleteValueCallback = deleteValueCallback ?? throw new ArgumentNullException(nameof(deleteValueCallback));

            _cache = new Dictionary<long, CacheBucket>();

            _pool = new Dictionary<TPoolKey, LinkedList<CacheBucket>>();

            _sortedCache = new LinkedList<CacheBucket>();

            _deletionPending = new Queue<TValue>();
        }

        public void Lock()
        {
            //Locking ensure that no resources are deleted while
            //the cache is locked, this prevent resources from
            //being deleted or modified while in use.
            _locked = true;
        }

        public void Unlock()
        {
            _locked = false;

            while (_deletionPending.TryDequeue(out TValue Value))
            {
                _deleteValueCallback(Value);
            }

            ClearCacheIfNeeded();
        }

        public void AddOrUpdate(long key, TPoolKey poolKey, TValue value, int size)
        {
            if (!_locked)
            {
                ClearCacheIfNeeded();
            }

            CacheBucket newBucket = new CacheBucket(key, poolKey, value, size);

            newBucket.UpdateCacheNode(_sortedCache.AddLast(newBucket));

            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                //A resource is considered orphan when it is no longer bound to
                //a key, and has been replaced by a newer one. It may still be
                //re-used. When the time expires, it will be deleted otherwise.
                bucket.MarkAsOrphan();

                //We need to delete all dependencies, to force them
                //to use the updated handles, since we are replacing this
                //resource on the cache with another one.
                bucket.DeleteAllDependencies();
            }

            _totalSize += size;

            _cache[key] = newBucket;
        }

        public void AddDependency<T, U>(long key, OGLResourceCache<T, U> soureCache, long sourceKey)
        {
            if (!_cache.TryGetValue(key, out CacheBucket bucket))
            {
                return;
            }

            bucket.AddDependency(() => soureCache.Delete(sourceKey));
        }

        public bool TryGetValue(long key, out TValue value)
        {
            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                value = bucket.Value;

                RemoveFromSortedCache(bucket.CacheNode);

                bucket.UpdateCacheNode(_sortedCache.AddLast(bucket.CacheNode.Value));

                RemoveFromResourcePool(bucket);

                return true;
            }

            value = default(TValue);

            return false;
        }

        public bool TryReuseValue(long key, TPoolKey poolKey, out TValue value)
        {
            if (_cache.TryGetValue(key, out CacheBucket bucket) && bucket.PoolKey.Equals(poolKey))
            {
                //Value on key is already compatible, we don't need to do anything.
                value = bucket.Value;

                return true;
            }

            if (_pool.TryGetValue(poolKey, out LinkedList<CacheBucket> queue))
            {
                LinkedListNode<CacheBucket> node = queue.First;

                bucket = node.Value;

                Remove(bucket);

                AddOrUpdate(key, poolKey, bucket.Value, bucket.Size);

                value = bucket.Value;

                return true;
            }

            value = default(TValue);

            return false;
        }

        public bool TryGetSize(long key, out int size)
        {
            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                size = bucket.Size;

                return true;
            }

            size = 0;

            return false;
        }

        public bool TryGetSizeAndValue(long key, out int size, out TValue value)
        {
            if (_cache.TryGetValue(key, out CacheBucket bucket))
            {
                size  = bucket.Size;
                value = bucket.Value;

                return true;
            }

            size  = 0;
            value = default(TValue);

            return false;
        }

        private void ClearCacheIfNeeded()
        {
            long timestamp = PerformanceCounter.ElapsedMilliseconds;

            for (int count = 0; count < MaxRemovalsPerRun; count++)
            {
                LinkedListNode<CacheBucket> node = _sortedCache.First;

                if (node == null)
                {
                    break;
                }

                CacheBucket bucket = node.Value;

                long timeDelta = timestamp - bucket.Timestamp;

                if (timeDelta <= MinTimeDelta && !UnderMemoryPressure())
                {
                    break;
                }

                Delete(bucket);
            }

            if (_poolTransferNode == null)
            {
                _poolTransferNode = _sortedCache.First;
            }

            while (_poolTransferNode != null)
            {
                CacheBucket bucket = _poolTransferNode.Value;

                long timeDelta = timestamp - bucket.Timestamp;

                if (timeDelta <= _minTimeForPoolTransfer)
                {
                    break;
                }

                AddToResourcePool(bucket);

                _poolTransferNode = _poolTransferNode.Next;
            }
        }

        private bool UnderMemoryPressure()
        {
            return _totalSize >= _maxSize;
        }

        private void Delete(long key)
        {
            if (!_cache.TryGetValue(key, out CacheBucket bucket))
            {
                return;
            }

            Delete(bucket);
        }

        private void Delete(CacheBucket bucket)
        {
            Remove(bucket);

            if (_locked)
            {
                _deletionPending.Enqueue(bucket.Value);
            }
            else
            {
                _deleteValueCallback(bucket.Value);
            }
        }

        private void Remove(CacheBucket bucket)
        {
            if (!bucket.Orphan)
            {
                _cache.Remove(bucket.Key);
            }

            RemoveFromSortedCache(bucket.CacheNode);
            RemoveFromResourcePool(bucket);

            bucket.DeleteAllDependencies();

            _totalSize -= bucket.Size;
        }

        private void RemoveFromSortedCache(LinkedListNode<CacheBucket> node)
        {
            if (_poolTransferNode == node)
            {
                _poolTransferNode = node.Next;
            }

            _sortedCache.Remove(node);
        }

        private bool AddToResourcePool(CacheBucket bucket)
        {
            if (bucket.PoolNode == null)
            {
                if (!_pool.TryGetValue(bucket.PoolKey, out LinkedList<CacheBucket> queue))
                {
                    _pool.Add(bucket.PoolKey, queue = new LinkedList<CacheBucket>());
                }

                bucket.UpdatePoolNode(queue.AddLast(bucket));

                return true;
            }

            return false;
        }

        private void RemoveFromResourcePool(CacheBucket bucket)
        {
            if (bucket.PoolNode != null)
            {
                LinkedList<CacheBucket> queue = bucket.PoolNode.List;

                queue.Remove(bucket.PoolNode);

                bucket.UpdatePoolNode(null);

                if (queue.Count == 0)
                {
                    _pool.Remove(bucket.PoolKey);
                }
            }
        }
    }
}