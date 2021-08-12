using System.Collections.Generic;

namespace Ryujinx.Graphics.Vulkan
{
    struct BufferRangeList
    {
        private struct Range
        {
            public int Offset { get; }
            public int Size { get; }

            public Range(int offset, int size)
            {
                Offset = offset;
                Size = size;
            }

            public bool OverlapsWith(int offset, int size)
            {
                return Offset < offset + size && offset < Offset + Size;
            }
        }

        private List<Range>[] _ranges;

        public void Initialize()
        {
            _ranges = new List<Range>[CommandBufferPool.MaxCommandBuffers];
        }

        public void Add(int cbIndex, int offset, int size)
        {
            var list = _ranges[cbIndex];
            if (list != null)
            {
                int overlapIndex = BinarySearch(list, offset, size);
                if (overlapIndex >= 0)
                {
                    while (overlapIndex > 0 && list[overlapIndex - 1].OverlapsWith(offset, size))
                    {
                        overlapIndex--;
                    }

                    int endOffset = offset + size;
                    int startIndex = overlapIndex;

                    while (overlapIndex < list.Count && list[overlapIndex].OverlapsWith(offset, size))
                    {
                        var currentOverlap = list[overlapIndex];
                        var currentOverlapEndOffset = currentOverlap.Offset + currentOverlap.Size;

                        if (offset > currentOverlap.Offset)
                        {
                            offset = currentOverlap.Offset;
                        }

                        if (endOffset < currentOverlapEndOffset)
                        {
                            endOffset = currentOverlapEndOffset;
                        }

                        overlapIndex++;
                    }

                    int count = overlapIndex - startIndex;

                    list.RemoveRange(startIndex, count);

                    size = endOffset - offset;
                    overlapIndex = startIndex;
                }
                else
                {
                    overlapIndex = ~overlapIndex;
                }

                list.Insert(overlapIndex, new Range(offset, size));

                int last = 0;
                foreach (var rg in list)
                {
                    if (rg.Offset < last)
                    {
                        throw new System.Exception("list not properly sorted");
                    }
                    last = rg.Offset;
                }
            }
            else
            {
                list = new List<Range>
                {
                    new Range(offset, size)
                };

                _ranges[cbIndex] = list;
            }
        }

        public bool OverlapsWith(int cbIndex, int offset, int size)
        {
            var list = _ranges[cbIndex];
            if (list == null)
            {
                return false;
            }

            return BinarySearch(list, offset, size) >= 0;
        }

        private static int BinarySearch(List<Range> list, int offset, int size)
        {
            int left = 0;
            int right = list.Count - 1;

            while (left <= right)
            {
                int range = right - left;

                int middle = left + (range >> 1);

                var item = list[middle];

                if (item.OverlapsWith(offset, size))
                {
                    return middle;
                }

                if (offset < item.Offset)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return ~left;
        }

        public void Clear(int cbIndex)
        {
            _ranges[cbIndex] = null;
        }
    }
}
