using ChocolArm64.Memory;
using System;

namespace Ryujinx.Graphics.Memory
{
    internal class NvGpuVmmCache
    {
        private ValueRangeSet<int> _cachedRanges;

        public NvGpuVmmCache()
        {
            _cachedRanges = new ValueRangeSet<int>();
        }

        public bool IsRegionModified(AMemory memory, NvGpuBufferType bufferType, long pa, long size)
        {
            (bool[] modified, long modifiedCount) = memory.IsRegionModified(pa, size);

            //Remove all modified ranges.
            int index = 0;

            long position = pa & ~NvGpuVmm.PageMask;

            while (modifiedCount > 0)
            {
                if (modified[index++])
                {
                    _cachedRanges.Remove(new ValueRange<int>(position, position + NvGpuVmm.PageSize));

                    modifiedCount--;
                }

                position += NvGpuVmm.PageSize;
            }

            //Mask has the bit set for the current resource type.
            //If the region is not yet present on the list, then a new ValueRange
            //is directly added with the current resource type as the only bit set.
            //Otherwise, it just sets the bit for this new resource type on the current mask.
            int mask = 1 << (int)bufferType;

            ValueRange<int> newCached = new ValueRange<int>(pa, pa + size);

            ValueRange<int>[] ranges = _cachedRanges.GetAllIntersections(newCached);

            long lastEnd = newCached.Start;

            long coverage = 0;

            for (index = 0; index < ranges.Length; index++)
            {
                ValueRange<int> current = ranges[index];

                long rgStart = Math.Max(current.Start, newCached.Start);
                long rgEnd   = Math.Min(current.End,   newCached.End);

                if ((current.Value & mask) == 0)
                    _cachedRanges.Add(new ValueRange<int>(rgStart, rgEnd, current.Value | mask));
                else
                    coverage += rgEnd - rgStart;

                if (rgStart > lastEnd) _cachedRanges.Add(new ValueRange<int>(lastEnd, rgStart, mask));

                lastEnd = rgEnd;
            }

            if (lastEnd < newCached.End) _cachedRanges.Add(new ValueRange<int>(lastEnd, newCached.End, mask));

            return coverage != size;
        }
    }
}