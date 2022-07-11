using System;
using System.Collections.Generic;

namespace Ryujinx.Memory.Range
{
    /// <summary>
    /// Sequence of physical memory regions that a single non-contiguous virtual memory region maps to.
    /// </summary>
    public struct MultiRange : IEquatable<MultiRange>
    {
        private readonly MemoryRange _singleRange;
        private readonly MemoryRange[] _ranges;

        private bool HasSingleRange => _ranges == null;

        /// <summary>
        /// Total of physical sub-ranges on the virtual memory region.
        /// </summary>
        public int Count => HasSingleRange ? 1 : _ranges.Length;

        /// <summary>
        /// Creates a new multi-range with a single physical region.
        /// </summary>
        /// <param name="address">Start address of the region</param>
        /// <param name="size">Size of the region in bytes</param>
        public MultiRange(ulong address, ulong size)
        {
            _singleRange = new MemoryRange(address, size);
            _ranges = null;
        }

        /// <summary>
        /// Creates a new multi-range with multiple physical regions.
        /// </summary>
        /// <param name="ranges">Array of physical regions</param>
        /// <exception cref="ArgumentNullException"><paramref name="ranges"/> is null</exception>
        public MultiRange(MemoryRange[] ranges)
        {
            _singleRange = MemoryRange.Empty;
            _ranges = ranges ?? throw new ArgumentNullException(nameof(ranges));
        }

        /// <summary>
        /// Appends a multi-range at the end of this range.
        /// </summary>
        /// <param name="right">Range to append at the end</param>
        /// <returns>A new range with <paramref name="right"/> appended at the end</returns>
        public MultiRange Append(MultiRange right)
        {
            MultiRange left = this;

            if (left.Count == 0)
            {
                return right;
            }
            else if (right.Count == 0)
            {
                return left;
            }

            MemoryRange leftLastRange = left.GetSubRange(left.Count - 1);
            MemoryRange rightFirstRange = right.GetSubRange(0);

            if (leftLastRange.EndAddress == rightFirstRange.Address)
            {
                if (left.Count == 1 && right.Count == 1)
                {
                    return new MultiRange(leftLastRange.Address, rightFirstRange.EndAddress - leftLastRange.Address);
                }

                MemoryRange[] ranges = new MemoryRange[left.Count + right.Count - 1];

                for (int index = 0; index < left.Count - 1; index++)
                {
                    ranges[index] = left.GetSubRange(index);
                }

                for (int index = 1; index < right.Count; index++)
                {
                    ranges[left.Count - 1 + index] = right.GetSubRange(index);
                }

                ranges[left.Count - 1] = new MemoryRange(leftLastRange.Address, rightFirstRange.EndAddress - leftLastRange.Address);

                return new MultiRange(ranges);
            }
            else
            {
                MemoryRange[] ranges = new MemoryRange[left.Count + right.Count];

                for (int index = 0; index < left.Count; index++)
                {
                    ranges[index] = left.GetSubRange(index);
                }

                for (int index = 0; index < right.Count; index++)
                {
                    ranges[left.Count + index] = right.GetSubRange(index);
                }

                return new MultiRange(ranges);
            }
        }

        /// <summary>
        /// Gets a slice of the multi-range.
        /// </summary>
        /// <param name="offset">Offset of the slice into the multi-range in bytes</param>
        /// <param name="size">Size of the slice in bytes</param>
        /// <returns>A new multi-range representing the given slice of this one</returns>
        public MultiRange Slice(ulong offset, ulong size)
        {
            if (HasSingleRange)
            {
                if (_singleRange.Size - offset < size)
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                return new MultiRange(_singleRange.Address + offset, size);
            }
            else
            {
                var ranges = new List<MemoryRange>();

                foreach (MemoryRange range in _ranges)
                {
                    if ((long)offset <= 0)
                    {
                        ranges.Add(new MemoryRange(range.Address, Math.Min(size, range.Size)));
                        size -= range.Size;
                    }
                    else if (offset < range.Size)
                    {
                        ulong sliceSize = Math.Min(size, range.Size - offset);
                        ranges.Add(new MemoryRange(range.Address + offset, sliceSize));
                        size -= sliceSize;
                    }

                    if ((long)size <= 0)
                    {
                        break;
                    }

                    offset -= range.Size;
                }

                return new MultiRange(ranges.ToArray());
            }
        }

        /// <summary>
        /// Gets the physical region at the specified index.
        /// </summary>
        /// <param name="index">Index of the physical region</param>
        /// <returns>Region at the index specified</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is invalid</exception>
        public MemoryRange GetSubRange(int index)
        {
            if (HasSingleRange)
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _singleRange;
            }
            else
            {
                if ((uint)index >= _ranges.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _ranges[index];
            }
        }

        /// <summary>
        /// Gets the physical region at the specified index, without explicit bounds checking.
        /// </summary>
        /// <param name="index">Index of the physical region</param>
        /// <returns>Region at the index specified</returns>
        private MemoryRange GetSubRangeUnchecked(int index)
        {
            return HasSingleRange ? _singleRange : _ranges[index];
        }

        /// <summary>
        /// Check if two multi-ranges overlap with each other.
        /// </summary>
        /// <param name="other">Other multi-range to check for overlap</param>
        /// <returns>True if any sub-range overlaps, false otherwise</returns>
        public bool OverlapsWith(MultiRange other)
        {
            if (HasSingleRange && other.HasSingleRange)
            {
                return _singleRange.OverlapsWith(other._singleRange);
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    MemoryRange currentRange = GetSubRangeUnchecked(i);

                    for (int j = 0; j < other.Count; j++)
                    {
                        if (currentRange.OverlapsWith(other.GetSubRangeUnchecked(j)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a given multi-range is fully contained inside another.
        /// </summary>
        /// <param name="other">Multi-range to be checked</param>
        /// <returns>True if all the sub-ranges on <paramref name="other"/> are contained inside the multi-range, with the same order, false otherwise</returns>
        public bool Contains(MultiRange other)
        {
            return FindOffset(other) >= 0;
        }

        /// <summary>
        /// Calculates the offset of a given multi-range inside another, when the multi-range is fully contained
        /// inside the other multi-range, otherwise returns -1.
        /// </summary>
        /// <param name="other">Multi-range that should be fully contained inside this one</param>
        /// <returns>Offset in bytes if fully contained, otherwise -1</returns>
        public int FindOffset(MultiRange other)
        {
            int thisCount = Count;
            int otherCount = other.Count;

            if (thisCount == 1 && otherCount == 1)
            {
                MemoryRange otherFirstRange = other.GetSubRangeUnchecked(0);
                MemoryRange currentFirstRange = GetSubRangeUnchecked(0);

                if (otherFirstRange.Address >= currentFirstRange.Address &&
                    otherFirstRange.EndAddress <= currentFirstRange.EndAddress)
                {
                    return (int)(otherFirstRange.Address - currentFirstRange.Address);
                }
            }
            else if (thisCount >= otherCount)
            {
                ulong baseOffset = 0;

                MemoryRange otherFirstRange = other.GetSubRangeUnchecked(0);
                MemoryRange otherLastRange = other.GetSubRangeUnchecked(otherCount - 1);

                for (int i = 0; i < (thisCount - otherCount) + 1; baseOffset += GetSubRangeUnchecked(i).Size, i++)
                {
                    MemoryRange currentFirstRange = GetSubRangeUnchecked(i);
                    MemoryRange currentLastRange = GetSubRangeUnchecked(i + otherCount - 1);

                    if (otherCount > 1)
                    {
                        if (otherFirstRange.Address < currentFirstRange.Address ||
                            otherFirstRange.EndAddress != currentFirstRange.EndAddress)
                        {
                            continue;
                        }

                        if (otherLastRange.Address != currentLastRange.Address ||
                            otherLastRange.EndAddress > currentLastRange.EndAddress)
                        {
                            continue;
                        }

                        bool fullMatch = true;

                        for (int j = 1; j < otherCount - 1; j++)
                        {
                            if (!GetSubRangeUnchecked(i + j).Equals(other.GetSubRangeUnchecked(j)))
                            {
                                fullMatch = false;
                                break;
                            }
                        }

                        if (!fullMatch)
                        {
                            continue;
                        }
                    }
                    else if (currentFirstRange.Address > otherFirstRange.Address ||
                             currentFirstRange.EndAddress < otherFirstRange.EndAddress)
                    {
                        continue;
                    }

                    return (int)(baseOffset + (otherFirstRange.Address - currentFirstRange.Address));
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the total size of all sub-ranges in bytes.
        /// </summary>
        /// <returns>Total size in bytes</returns>
        public ulong GetSize()
        {
            if (HasSingleRange)
            {
                return _singleRange.Size;
            }

            ulong sum = 0;

            foreach (MemoryRange range in _ranges)
            {
                sum += range.Size;
            }

            return sum;
        }

        public override bool Equals(object obj)
        {
            return obj is MultiRange other && Equals(other);
        }

        public bool Equals(MultiRange other)
        {
            if (HasSingleRange && other.HasSingleRange)
            {
                return _singleRange.Equals(other._singleRange);
            }

            int thisCount = Count;
            if (thisCount != other.Count)
            {
                return false;
            }

            for (int i = 0; i < thisCount; i++)
            {
                if (!GetSubRangeUnchecked(i).Equals(other.GetSubRangeUnchecked(i)))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (HasSingleRange)
            {
                return _singleRange.GetHashCode();
            }

            HashCode hash = new HashCode();

            foreach (MemoryRange range in _ranges)
            {
                hash.Add(range);
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return HasSingleRange ? _singleRange.ToString() : string.Join(", ", _ranges);
        }
    }
}
