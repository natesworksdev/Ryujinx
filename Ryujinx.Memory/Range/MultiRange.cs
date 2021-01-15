using System;

namespace Ryujinx.Memory.Range
{
    public struct Range : IEquatable<Range>
    {
        public static Range Empty => new Range(0UL, 0);

        public ulong Address { get; }
        public ulong Size { get; }
        public ulong EndAddress => Address + Size;

        public Range(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }

        public bool OverlapsWith(Range other)
        {
            ulong thisAddress = Address;
            ulong thisEndAddress = EndAddress;
            ulong otherAddress = other.Address;
            ulong otherEndAddress = other.EndAddress;

            return thisAddress < otherEndAddress && otherAddress < thisEndAddress;
        }

        public override bool Equals(object obj)
        {
            return obj is Range other && Equals(other);
        }

        public bool Equals(Range other)
        {
            return Address == other.Address && Size == other.Size;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Size);
        }
    }

    public struct MultiRange
    {
        private readonly Range _singleRange;
        private readonly Range[] _ranges;

        private bool HasSingleRange => _ranges == null;
        public int Count => HasSingleRange ? 1 : _ranges.Length;

        public MultiRange(ulong address, ulong size)
        {
            _singleRange = new Range(address, size);
            _ranges = null;
        }

        public MultiRange(Range[] ranges)
        {
            _singleRange = Range.Empty;
            _ranges = ranges ?? throw new ArgumentNullException(nameof(ranges));
        }

        public Range GetSubRange(int index)
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

        private Range GetSubRangeUnchecked(int index)
        {
            return HasSingleRange ? _singleRange : _ranges[index];
        }

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
                    Range currentRange = GetSubRangeUnchecked(i);

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

        public bool Contains(MultiRange other)
        {
            return FindOffset(other) >= 0;
        }

        public int FindOffset(MultiRange other)
        {
            int thisCount = Count;
            int otherCount = other.Count;

            if (thisCount == 1 && otherCount == 1)
            {
                Range otherFirstRange = other.GetSubRangeUnchecked(0);
                Range currentFirstRange = GetSubRangeUnchecked(0);

                if (otherFirstRange.Address >= currentFirstRange.Address &&
                    otherFirstRange.EndAddress <= currentFirstRange.EndAddress)
                {
                    return (int)(otherFirstRange.Address - currentFirstRange.Address);
                }
            }
            else if (thisCount >= otherCount)
            {
                ulong baseOffset = 0;

                Range otherFirstRange = other.GetSubRangeUnchecked(0);
                Range otherLastRange = other.GetSubRangeUnchecked(otherCount - 1);

                for (int i = 0; i < (thisCount - otherCount) + 1; baseOffset += GetSubRangeUnchecked(i).Size, i++)
                {
                    Range currentFirstRange = GetSubRangeUnchecked(i);
                    Range currentLastRange = GetSubRangeUnchecked(i + otherCount - 1);

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

        public ulong GetTotalSize()
        {
            ulong sum = 0;

            foreach (Range range in _ranges)
            {
                sum += range.Size;
            }

            return sum;
        }
    }
}
