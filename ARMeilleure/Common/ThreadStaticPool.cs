using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ARMeilleure.Common
{
    class ThreadStaticPool<T> where T : class, new()
    {
        private const int ChunkSizeLimit = 1000; // even
        private const int PoolSizeIncrement = 200; // > 0

        [ThreadStatic]
        private static ThreadStaticPool<T> _instance;

        public static ThreadStaticPool<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    PreparePool(0); // So that we can still use a pool when blindly initializing one.
                }

                return _instance;
            }
        }

        private static ConcurrentDictionary<int, Stack<ThreadStaticPool<T>>> _pools = new ConcurrentDictionary<int, Stack<ThreadStaticPool<T>>>();

        private static Stack<ThreadStaticPool<T>> GetPools(int groupId)
        {
            return _pools.GetOrAdd(groupId, x => new Stack<ThreadStaticPool<T>>());
        }

        public static void PreparePool(int groupId)
        {
            // Prepare the pool for this thread, ideally using an existing one from the specified group.

            if (_instance == null)
            {
                var pools = GetPools(groupId);
                lock (pools)
                {
                    _instance = (pools.Count != 0) ? pools.Pop() : new ThreadStaticPool<T>();
                }
            }
        }

        public static void ReturnPool(int groupId)
        {
            // Reset and return the pool for this thread to the specified group.

            var pools = GetPools(groupId);
            lock (pools)
            {
                _instance.Clear();
                _instance.ChunkSizeLimiter();
                pools.Push(_instance);

                _instance = null;
            }
        }

        public static void ResetPools()
        {
            // Resets any static references to the pools used by threads for each group, allowing them to be garbage collected.

            foreach (var pools in _pools.Values)
            {
                foreach (var pool in pools)
                {
                    pool.Dispose();
                }

                pools.Clear();
            }

            _pools.Clear();
        }

        private List<T[]> _pool;
        private int _chunkIndex = -1;
        private int _chunkSize;
        private int _poolIndex = -1;
        private int _poolSize;

        private ThreadStaticPool()
        {
            _pool = new List<T[]>(ChunkSizeLimit * 2);

            AddChunkIfNeeded();
        }

        public T Allocate()
        {
            int poolIndex = Interlocked.Increment(ref _poolIndex);

            if (poolIndex >= PoolSizeIncrement)
            {
                AddChunkIfNeeded();

                poolIndex = _poolIndex = 0;
            }

            return _pool[_chunkIndex][poolIndex];
        }

        private void AddChunkIfNeeded()
        {
            int chunkIndex = Interlocked.Increment(ref _chunkIndex);

            if (chunkIndex >= _chunkSize)
            {
                T[] pool = new T[PoolSizeIncrement];

                for (int i = 0; i < PoolSizeIncrement; i++)
                {
                    pool[i] = new T();
                }

                _pool.Add(pool);

                _chunkSize++;
                _poolSize += PoolSizeIncrement;
            }
        }

        public void Clear()
        {
            _chunkIndex = 0;
            _poolIndex = -1;
        }

        private void ChunkSizeLimiter()
        {
            if (_chunkSize >= ChunkSizeLimit)
            {
                int chunkSize = _chunkSize;

                _chunkSize = ChunkSizeLimit / 2;
                _poolSize = _chunkSize * PoolSizeIncrement;

                _pool.RemoveRange(_chunkSize, chunkSize - _chunkSize);
                _pool.Capacity = ChunkSizeLimit * 2;
            }
        }

        private void Dispose()
        {
            _pool.Clear();
        }
    }
}
