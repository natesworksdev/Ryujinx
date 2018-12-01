using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ChocolArm64
{
    class TranslatorCache
    {
        //Maximum size of the cache, the unit used is completely arbitrary.
        private const int MaxTotalSize = 0x100000;

        //Minimum time required in milliseconds for a method to be eligible for deletion.
        private const int MinTimeDelta = 2 * 60000;

        private class CacheBucket
        {
            public TranslatedSub Subroutine { get; private set; }

            public int Size { get; private set; }

            public long Timestamp { get; private set; }

            public CacheBucket(TranslatedSub subroutine, int size)
            {
                Subroutine = subroutine;
                Size       = size;
            }

            public void UpdateTimestamp()
            {
                Timestamp = GetTimestamp();
            }
        }

        private ConcurrentDictionary<long, CacheBucket> _cache;

        private int _totalSize;

        public TranslatorCache()
        {
            _cache = new ConcurrentDictionary<long, CacheBucket>();
        }

        public void AddOrUpdate(long position, TranslatedSub subroutine, int size)
        {
            ClearCacheIfNeeded();

            CacheBucket newBucket = new CacheBucket(subroutine, size);

            _cache.AddOrUpdate(position, newBucket, (key, oldBucket) =>
            {
                Interlocked.Add(ref _totalSize, -oldBucket.Size);

                return newBucket;
            });

            Interlocked.Add(ref _totalSize, size);
        }

        public bool HasSubroutine(long position)
        {
            return _cache.ContainsKey(position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSubroutine(long position, out TranslatedSub subroutine)
        {
            if (_cache.TryGetValue(position, out CacheBucket bucket))
            {
                bucket.UpdateTimestamp();

                subroutine = bucket.Subroutine;

                return true;
            }

            subroutine = default(TranslatedSub);

            return false;
        }

        private void ClearCacheIfNeeded()
        {
            if (_totalSize <= MaxTotalSize)
            {
                return;
            }

            long timestamp = GetTimestamp();

            foreach (KeyValuePair<long, CacheBucket> kv in _cache.OrderBy(x => (ulong)x.Value.Timestamp))
            {
                CacheBucket bucket = kv.Value;

                long timeDelta = timestamp - bucket.Timestamp;

                if (timeDelta <= MinTimeDelta)
                {
                    break;
                }

                if (_cache.Remove(kv.Key, out bucket))
                {
                    Interlocked.Add(ref _totalSize, -bucket.Size);
                }

                if (_totalSize <= MaxTotalSize)
                {
                    break;
                }
            }
        }

        private static long GetTimestamp()
        {
            long timestamp = Stopwatch.GetTimestamp();

            return timestamp / (Stopwatch.Frequency / 1000);
        }
    }
}