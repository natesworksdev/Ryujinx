using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLCachedResource<TKey, TValue>
        where TKey   : ICompatible<TKey>
        where TValue : Resource
    {
        private struct CacheBucket
        {
            public TValue Value { get; private set; }

            public long DataSize { get; private set; }

            public CacheBucket(TValue Value, long DataSize)
            {
                this.Value    = Value;
                this.DataSize = DataSize;
            }
        }

        private Dictionary<long, CacheBucket> Cache;

        private ResourcePool<TKey, TValue> Pool;

        private bool Locked;

        public OGLCachedResource(
            ResourcePool<TKey, TValue>.CreateValue CreateValueCallback,
            ResourcePool<TKey, TValue>.DeleteValue DeleteValueCallback)
        {
            Cache = new Dictionary<long, CacheBucket>();

            Pool = new ResourcePool<TKey, TValue>(
                CreateValueCallback ?? throw new ArgumentNullException(nameof(CreateValueCallback)),
                DeleteValueCallback ?? throw new ArgumentNullException(nameof(DeleteValueCallback)));
        }

        public void Lock()
        {
            Locked = true;
        }

        public void Unlock()
        {
            Locked = false;

            Pool.ReleaseMemory();
        }

        public TValue CreateOrRecycle(long Key, TKey Parameters, long Size)
        {
            if (!Locked)
            {
                Pool.ReleaseMemory();
            }

            if (Cache.TryGetValue(Key, out CacheBucket Bucket))
            {
                Bucket.Value.MarkAsUnused();
            }

            TValue Value = Pool.CreateOrRecycle(Parameters);

            Cache[Key] = new CacheBucket(Value, Size);

            return Value;
        }

        public bool TryGetValue(long Key, out TValue Value)
        {
            if (Cache.TryGetValue(Key, out CacheBucket Bucket))
            {
                Value = Bucket.Value;

                Value.UpdateTimestamp();

                return true;
            }

            Value = default(TValue);

            return false;
        }

        public bool TryGetSize(long Key, out long Size)
        {
            if (Cache.TryGetValue(Key, out CacheBucket Bucket))
            {
                Size = Bucket.DataSize;

                return true;
            }

            Size = 0;

            return false;
        }
    }
}