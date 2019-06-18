using ARMeilleure.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class LiveInterval : IComparable<LiveInterval>
    {
        private const int NotFound = -1;

        private LiveInterval _parent;

        private SortedSet<int> _usePositions;

        public int UsesCount => _usePositions.Count;

        private List<LiveRange> _ranges;

        private SortedList<int, LiveInterval> _childs;

        public bool IsSplit => _childs.Count != 0;

        public Operand Local { get; }

        private Register _register;

        public bool HasRegister { get; private set; }

        public Register Register
        {
            get
            {
                return _register;
            }
            set
            {
                _register = value;

                HasRegister = true;
            }
        }

        public int SpillOffset { get; private set; }

        public bool IsSpilled => SpillOffset != -1;
        public bool IsFixed { get; }

        public bool IsEmpty => _ranges.Count == 0;

        public LiveInterval(Operand local = null, LiveInterval parent = null)
        {
            Local   = local;
            _parent = parent ?? this;

            _usePositions = new SortedSet<int>();

            _ranges = new List<LiveRange>();

            _childs = new SortedList<int, LiveInterval>();

            SpillOffset = -1;
        }

        public LiveInterval(Register register) : this()
        {
            IsFixed  = true;
            Register = register;
        }

        public void SetStart(int position)
        {
            if (_ranges.Count != 0)
            {
                _ranges[0] = new LiveRange(position, _ranges[0].End);
            }
            else
            {
                _ranges.Add(new LiveRange(position, position));
            }
        }

        public int GetStart()
        {
            if (_ranges.Count == 0)
            {
                throw new InvalidOperationException("Empty interval.");
            }

            return _ranges[0].Start;
        }

        public int GetEnd()
        {
            if (_ranges.Count == 0)
            {
                throw new InvalidOperationException("Empty interval.");
            }

            return _ranges[_ranges.Count - 1].End;
        }

        public void AddRange(int start, int end)
        {
            int index = _ranges.BinarySearch(new LiveRange(start, end));

            if (index >= 0)
            {
                //New range insersects with an existing range, we need to remove
                //all the intersecting ranges before adding the new one.
                //We also extend the new range as needed, based on the values of
                //the existing ranges being removed.
                int lIndex = index;
                int rIndex = index;

                while (lIndex > 0 && _ranges[lIndex - 1].End >= start)
                {
                    lIndex--;
                }

                while (rIndex + 1 < _ranges.Count && _ranges[rIndex + 1].Start <= end)
                {
                    rIndex++;
                }

                if (start > _ranges[lIndex].Start)
                {
                    start = _ranges[lIndex].Start;
                }

                if (end < _ranges[rIndex].End)
                {
                    end = _ranges[rIndex].End;
                }

                _ranges.RemoveRange(lIndex, (rIndex - lIndex) + 1);

                InsertRange(lIndex, start, end);
            }
            else
            {
                InsertRange(~index, start, end);
            }
        }

        private void InsertRange(int index, int start, int end)
        {
            //Here we insert a new range on the ranges list.
            //If possible, we extend an existing range rather than inserting a new one.
            //We can extend an existing range if any of the following conditions are true:
            //- The new range starts right after the end of the previous range on the list.
            //- The new range ends right before the start of the next range on the list.
            //If both cases are true, we can extend either one. We prefer to extend the
            //previous range, and then remove the next one, but theres no specific reason
            //for that, extending either one will do.
            int? extIndex = null;

            if (index > 0 && _ranges[index - 1].End == start)
            {
                start = _ranges[index - 1].Start;

                extIndex = index - 1;
            }

            if (index < _ranges.Count && _ranges[index].Start == end)
            {
                end = _ranges[index].End;

                if (extIndex.HasValue)
                {
                    _ranges.RemoveAt(index);
                }
                else
                {
                    extIndex = index;
                }
            }

            if (extIndex.HasValue)
            {
                _ranges[extIndex.Value] = new LiveRange(start, end);
            }
            else
            {
                _ranges.Insert(index, new LiveRange(start, end));
            }
        }

        public void AddUsePosition(int position)
        {
            _usePositions.Add(position);
        }

        public bool Overlaps(int position)
        {
            return _ranges.BinarySearch(new LiveRange(position, position + 1)) >= 0;
        }

        public bool Overlaps(LiveInterval other)
        {
            foreach (LiveRange range in other._ranges)
            {
                if (_ranges.BinarySearch(range) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<LiveInterval> SplitChilds()
        {
            return _childs.Values;
        }

        public IEnumerable<int> UsePositions()
        {
            return _usePositions;
        }

        public int FirstUse()
        {
            if (_usePositions.Count == 0)
            {
                return NotFound;
            }

            return _usePositions.First();
        }

        public int NextUseAfter(int position)
        {
            foreach (int usePosition in _usePositions)
            {
                if (usePosition >= position)
                {
                    return usePosition;
                }
            }

            return NotFound;
        }

        public int NextOverlap(LiveInterval other)
        {
            foreach (LiveRange range in other._ranges)
            {
                int overlapIndex = _ranges.BinarySearch(range);

                if (overlapIndex >= 0)
                {
                    LiveRange overlappingRange = _ranges[overlapIndex];

                    if (range.Start > overlappingRange.Start)
                    {
                        return Math.Min(range.End, overlappingRange.End);
                    }
                    else
                    {
                        return overlappingRange.Start;
                    }
                }
            }

            return NotFound;
        }

        public LiveInterval Split(int position)
        {
            LiveInterval right = new LiveInterval(Local, _parent);

            int splitIndex = 0;

            for (; splitIndex < _ranges.Count; splitIndex++)
            {
                LiveRange range = _ranges[splitIndex];

                if (position > range.Start && position <= range.End)
                {
                    right._ranges.Add(new LiveRange(position, range.End));

                    range = new LiveRange(range.Start, position);

                    _ranges[splitIndex++] = range;

                    break;
                }

                if (range.Start >= position)
                {
                    break;
                }
            }

            if (splitIndex < _ranges.Count)
            {
                int count = _ranges.Count - splitIndex;

                right._ranges.AddRange(_ranges.GetRange(splitIndex, count));

                _ranges.RemoveRange(splitIndex, count);
            }

            foreach (int usePosition in _usePositions.Where(x => x >= position))
            {
                right._usePositions.Add(usePosition);
            }

            _usePositions.RemoveWhere(x => x >= position);

            Debug.Assert(_ranges.Count != 0, "Left interval is empty after split.");

            Debug.Assert(right._ranges.Count != 0, "Right interval is empty after split.");

            AddSplitChild(right);

            return right;
        }

        private void AddSplitChild(LiveInterval child)
        {
            Debug.Assert(!child.IsEmpty, "Trying to insert a empty interval.");

            _parent._childs.Add(child.GetStart(), child);
        }

        public LiveInterval GetSplitChild(int position)
        {
            //Try to find the interval among the split intervals that
            //contains the given position. The end is technically exclusive,
            //so if we have a interval where position == start, and other
            //where position == end, we should prefer the former.
            //To achieve that, we can just check the split childs backwards,
            //as they are sorted by start/end position, and there are no overlaps.
            for (int index = _childs.Count - 1; index >= 0; index--)
            {
                LiveInterval splitChild = _childs.Values[index];

                if (position >= splitChild.GetStart() && position <= splitChild.GetEnd())
                {
                    return splitChild;
                }
            }

            if (position >= GetStart() && position <= GetEnd())
            {
                return this;
            }

            return null;
        }

        public bool TrySpillWithSiblingOffset()
        {
            foreach (LiveInterval splitChild in _parent._childs.Values)
            {
                if (splitChild.IsSpilled)
                {
                    Spill(splitChild.SpillOffset);

                    return true;
                }
            }

            return false;
        }

        public void Spill(int offset)
        {
            SpillOffset = offset;
        }

        public int CompareTo(LiveInterval other)
        {
            if (_ranges.Count == 0 || other._ranges.Count == 0)
            {
                return _ranges.Count.CompareTo(other._ranges.Count);
            }

            return _ranges[0].Start.CompareTo(other._ranges[0].Start);
        }

        public override string ToString()
        {
            return string.Join("; ", _ranges);
        }
    }
}