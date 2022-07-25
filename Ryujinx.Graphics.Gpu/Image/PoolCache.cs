using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Resource pool interface.
    /// </summary>
    /// <typeparam name="T">Resource pool type</typeparam>
    interface IPool<T>
    {
        /// <summary>
        /// Start address of the pool in memory.
        /// </summary>
        ulong Address { get; }

        /// <summary>
        /// Linked list node used on the texture pool cache.
        /// </summary>
        LinkedListNode<T> CacheNode { get; set; }

        /// <summary>
        /// Timestamp set on the last use of the pool by the cache.
        /// </summary>
        ulong CacheTimestamp { get; set; }
    }

    /// <summary>
    /// Pool cache.
    /// This can keep multiple pools, and return the current one as needed.
    /// </summary>
    class PoolCache<T> : IDisposable where T : IPool<T>, IDisposable
    {
        private const int MaxCapacity = 2;
        private const ulong MinDeltaForRemoval = 20000;

        private readonly GpuContext _context;
        private readonly Func<GpuContext, GpuChannel, ulong, int, T> _factory;
        private readonly LinkedList<T> _pools;
        private ulong _currentTimestamp;

        /// <summary>
        /// Constructs a new instance of the pool.
        /// </summary>
        /// <param name="context">GPU context that the texture pool belongs to</param>
        /// <param name="factory">Method used to create a new pool instance</param>
        public PoolCache(GpuContext context, Func<GpuContext, GpuChannel, ulong, int, T> factory)
        {
            _context = context;
            _factory = factory;
            _pools = new LinkedList<T>();
        }

        /// <summary>
        /// Increments the internal timestamp of the cache that is used to decide when old resources will be deleted.
        /// </summary>
        public void Tick()
        {
            _currentTimestamp++;
        }

        /// <summary>
        /// Finds a cache texture pool, or creates a new one if not found.
        /// </summary>
        /// <param name="channel">GPU channel that the texture pool cache belongs to</param>
        /// <param name="address">Start address of the texture pool</param>
        /// <param name="maximumId">Maximum ID of the texture pool</param>
        /// <returns>The found or newly created texture pool</returns>
        public T FindOrCreate(GpuChannel channel, ulong address, int maximumId)
        {
            // Remove old entries from the cache, if possible.
            while (_pools.Count > MaxCapacity && (_currentTimestamp - _pools.First.Value.CacheTimestamp) >= MinDeltaForRemoval)
            {
                T oldestPool = _pools.First.Value;

                _pools.RemoveFirst();
                oldestPool.Dispose();
                oldestPool.CacheNode = null;
            }

            T pool;

            // Try to find the pool on the cache.
            for (LinkedListNode<T> node = _pools.First; node != null; node = node.Next)
            {
                pool = node.Value;

                if (pool.Address == address)
                {
                    if (pool.CacheNode != _pools.Last)
                    {
                        _pools.Remove(pool.CacheNode);

                        pool.CacheNode = _pools.AddLast(pool);
                    }

                    pool.CacheTimestamp = _currentTimestamp;

                    return pool;
                }
            }

            // If not found, create a new one.
            pool = _factory(_context, channel, address, maximumId);

            pool.CacheNode = _pools.AddLast(pool);
            pool.CacheTimestamp = _currentTimestamp;

            return pool;
        }

        public void Dispose()
        {
            foreach (T pool in _pools)
            {
                pool.Dispose();
                pool.CacheNode = null;
            }

            _pools.Clear();
        }
    }
}