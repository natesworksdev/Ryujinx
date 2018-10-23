using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    class ValueRangeSet<T>
    {
        private List<ValueRange<T>> _ranges;

        public ValueRangeSet()
        {
            _ranges = new List<ValueRange<T>>();
        }

        public void Add(ValueRange<T> range)
        {
            if (range.End <= range.Start)
            {
                //Empty or invalid range, do nothing.
                return;
            }

            int first = BinarySearchFirstIntersection(range);

            if (first == -1)
            {
                //No intersections case.
                //Find first greater than range (after the current one).
                //If found, add before, otherwise add to the end of the list.
                int gtIndex = BinarySearchGt(range);

                if (gtIndex != -1)
                {
                    _ranges.Insert(gtIndex, range);
                }
                else
                {
                    _ranges.Add(range);
                }

                return;
            }

            (int start, int end) = GetAllIntersectionRanges(range, first);

            ValueRange<T> prev = _ranges[start];
            ValueRange<T> next = _ranges[end];

            _ranges.RemoveRange(start, (end - start) + 1);

            InsertNextNeighbour(start, range, next);

            int newIndex = start;

            _ranges.Insert(start, range);

            InsertPrevNeighbour(start, range, prev);

            //Try merging neighbours if the value is equal.
            if (newIndex > 0)
            {
                prev = _ranges[newIndex - 1];

                if (prev.End == range.Start && CompareValues(prev, range))
                {
                    _ranges.RemoveAt(--newIndex);

                    _ranges[newIndex] = new ValueRange<T>(prev.Start, range.End, range.Value);
                }
            }

            if (newIndex < _ranges.Count - 1)
            {
                next = _ranges[newIndex + 1];

                if (next.Start == range.End && CompareValues(next, range))
                {
                    _ranges.RemoveAt(newIndex + 1);

                    _ranges[newIndex] = new ValueRange<T>(range.Start, next.End, range.Value);
                }
            }
        }

        private bool CompareValues(ValueRange<T> lhs, ValueRange<T> rhs)
        {
            return lhs.Value?.Equals(rhs.Value) ?? rhs.Value == null;
        }

        public void Remove(ValueRange<T> range)
        {
            int first = BinarySearchFirstIntersection(range);

            if (first == -1)
            {
                //Nothing to remove.
                return;
            }

            (int start, int end) = GetAllIntersectionRanges(range, first);

            ValueRange<T> prev = _ranges[start];
            ValueRange<T> next = _ranges[end];

            _ranges.RemoveRange(start, (end - start) + 1);

            InsertNextNeighbour(start, range, next);
            InsertPrevNeighbour(start, range, prev);
        }

        private void InsertNextNeighbour(int index, ValueRange<T> range, ValueRange<T> next)
        {
            //Split last intersection (ordered by Start) if necessary.
            if (range.End < next.End)
            {
                InsertNewRange(index, range.End, next.End, next.Value);
            }
        }

        private void InsertPrevNeighbour(int index, ValueRange<T> range, ValueRange<T> prev)
        {
            //Split first intersection (ordered by Start) if necessary.
            if (range.Start > prev.Start)
            {
                InsertNewRange(index, prev.Start, range.Start, prev.Value);
            }
        }

        private void InsertNewRange(int index, long start, long end, T value)
        {
            _ranges.Insert(index, new ValueRange<T>(start, end, value));
        }

        public ValueRange<T>[] GetAllIntersections(ValueRange<T> range)
        {
            int first = BinarySearchFirstIntersection(range);

            if (first == -1)
            {
                return new ValueRange<T>[0];
            }

            (int start, int end) = GetAllIntersectionRanges(range, first);

            return _ranges.GetRange(start, (end - start) + 1).ToArray();
        }

        private (int Start, int End) GetAllIntersectionRanges(ValueRange<T> range, int baseIndex)
        {
            int start = baseIndex;
            int end   = baseIndex;

            while (start > 0 && Intersects(range, _ranges[start - 1]))
            {
                start--;
            }

            while (end < _ranges.Count - 1 && Intersects(range, _ranges[end + 1]))
            {
                end++;
            }

            return (start, end);
        }

        private int BinarySearchFirstIntersection(ValueRange<T> range)
        {
            int left  = 0;
            int right = _ranges.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                ValueRange<T> current = _ranges[middle];

                if (Intersects(range, current))
                {
                    return middle;
                }

                if (range.Start < current.Start)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return -1;
        }

        private int BinarySearchGt(ValueRange<T> range)
        {
            int gtIndex = -1;

            int left  = 0;
            int right = _ranges.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                ValueRange<T> current = _ranges[middle];

                if (range.Start < current.Start)
                {
                    right = middle - 1;

                    if (gtIndex == -1 || current.Start < _ranges[gtIndex].Start)
                    {
                        gtIndex = middle;
                    }
                }
                else
                {
                    left = middle + 1;
                }
            }

            return gtIndex;
        }

        private bool Intersects(ValueRange<T> lhs, ValueRange<T> rhs)
        {
            return lhs.Start < rhs.End && rhs.Start < lhs.End;
        }
    }
}