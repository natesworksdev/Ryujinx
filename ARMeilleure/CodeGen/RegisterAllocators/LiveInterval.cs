using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class LiveInterval : IComparable<LiveInterval>
    {
        public const int NotFound = -1;

        private int _end;
        private LiveRange _firstRange;
        private LiveRange _prevRange;
        private LiveRange _currRange;

        private readonly LiveInterval _parent;
        private readonly SortedIntegerList _usePositions;
        private readonly SortedList<int, LiveInterval> _childs;

        public Operand Local { get; }
        public Register Register { get; set; }

        public int SpillOffset { get; private set; }

        public bool IsFixed { get; }
        public bool IsEmpty => _firstRange == default;
        public bool IsSplit => _childs != null && _childs.Count != 0;
        public bool IsSpilled => SpillOffset != -1;

        public int UsesCount => _usePositions.Count;

        public LiveInterval(Operand local = default, LiveInterval parent = null)
        {
            Local = local;

            _firstRange = default;
            _currRange = default;
            _usePositions = new SortedIntegerList();

            // Only parent intervals can have child splits.
            if (parent != null)
            {
                _parent = parent;
                _childs = null;
            }
            else
            {
                _parent = this;
                _childs = new SortedList<int, LiveInterval>();
            }

            SpillOffset = -1;
        }

        public LiveInterval(Register register) : this()
        {
            IsFixed = true;
            Register = register;
        }

        public void Reset()
        {
            _prevRange = default;
            _currRange = _firstRange;
        }

        public void Forward(int position)
        {
            LiveRange prev = _prevRange;
            LiveRange curr = _currRange;

            while (curr != default && curr.Start < position && !curr.Overlaps(position))
            {
                prev = curr;
                curr = curr.Next;
            }

            _prevRange = prev;
            _currRange = curr;
        }

        public int GetStart()
        {
            Debug.Assert(!IsEmpty, "Empty LiveInterval cannot have a start position.");

            return _firstRange.Start;
        }

        public void SetStart(int position)
        {
            if (_firstRange != default)
            {
                Debug.Assert(position != _firstRange.End);

                _firstRange.Start = position;
            }
            else
            {
                _firstRange = new LiveRange(position, position + 1);
                _end = position + 1;
            }
        }

        public int GetEnd()
        {
            Debug.Assert(!IsEmpty, "Empty LiveInterval cannot have an end position.");

            return _end;
        }

        public void AddRange(int start, int end)
        {
            Debug.Assert(start < end, $"Invalid range start position {start}, {end}");

            if (_firstRange != default)
            {
                // If the new range ends exactly where the first range start, then coalesce together.
                if (end == _firstRange.Start)
                {
                    _firstRange.Start = start;

                    return;
                }
                // If the new range is already contained, then coalesce together.
                else if (start >= _firstRange.Start)
                {
                    if (end > _firstRange.End)
                    {
                        _firstRange.End = end;
                    }

                    return;
                }
            }

            _firstRange = new LiveRange(start, end, _firstRange);
            _end = Math.Max(_end, end);
        }

        public void AddUsePosition(int position)
        {
            // Inserts are in descending order, but ascending is faster for SortedIntegerList<>. We flip the ordering,
            // then iterate backwards when using the final list.
            _usePositions.Add(-position);
        }

        public bool Overlaps(int position)
        {
            LiveRange curr = _currRange;

            while (curr != default && curr.Start <= position)
            {
                if (curr.Overlaps(position))
                {
                    return true;
                }

                curr = curr.Next;
            }

            return false;
        }

        public bool Overlaps(LiveInterval other)
        {
            return GetOverlapPosition(other) != NotFound;
        }

        public int GetOverlapPosition(LiveInterval other)
        {
            LiveRange a = _currRange;
            LiveRange b = other._currRange;

            while (a != default)
            {
                while (b != default && b.Start < a.Start)
                {
                    if (a.Overlaps(b))
                    {
                        return a.Start;
                    }

                    b = b.Next;
                }

                if (b == default)
                {
                    break;
                }
                else if (a.Overlaps(b))
                {
                    return a.Start;
                }

                a = a.Next;
            }

            return NotFound;
        }

        public IEnumerable<LiveInterval> SplitChilds()
        {
            return _parent._childs.Values;
        }

        public IList<int> UsePositions()
        {
            return _usePositions.GetList();
        }

        public int FirstUse()
        {
            if (_usePositions.Count == 0)
            {
                return NotFound;
            }

            return -_usePositions.Last();
        }

        public int NextUseAfter(int position)
        {
            int index = _usePositions.FindLessEqualIndex(-position);

            return (index >= 0) ? -_usePositions[index] : NotFound;
        }

        public void RemoveAfter(int position)
        {
            int index = _usePositions.FindLessEqualIndex(-position);

            _usePositions.RemoveRange(0, index + 1);
        }

        public LiveInterval Split(int position)
        {
            LiveInterval result = new(Local, _parent);
            result._end = _end;

            LiveRange prev = _prevRange;
            LiveRange curr = _currRange;

            while (curr != default && curr.Start < position && !curr.Overlaps(position))
            {
                prev = curr;
                curr = curr.Next;
            }

            if (curr.Start >= position)
            {
                prev.Next = default;

                result._firstRange = curr;

                _end = prev.End;
            }
            else
            {
                result._firstRange = new LiveRange(position, curr.End, curr.Next);

                curr.End = position;
                curr.Next = default;

                _end = curr.End;
            }

            int addAfter = _usePositions.FindLessEqualIndex(-position);

            for (int index = addAfter; index >= 0; index--)
            {
                int usePosition = _usePositions[index];

                result._usePositions.Add(usePosition);
            }

            _usePositions.RemoveRange(0, addAfter + 1);

            AddSplitChild(result);

            Debug.Assert(!IsEmpty, "Left interval is empty after split.");
            Debug.Assert(!result.IsEmpty, "Right interval is empty after split.");

            // Make sure the iterator in the new split is pointing to the start.
            result.Reset();

            return result;
        }

        private void AddSplitChild(LiveInterval child)
        {
            Debug.Assert(!child.IsEmpty, "Trying to insert a empty interval.");

            _parent._childs.Add(child.GetStart(), child);
        }

        public LiveInterval GetSplitChild(int position)
        {
            if (Overlaps(position))
            {
                return this;
            }

            foreach (LiveInterval splitChild in _parent._childs.Values)
            {
                if (splitChild.Overlaps(position))
                {
                    return splitChild;
                }
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

        public int CompareTo(LiveInterval interval)
        {
            if (_firstRange == default || interval._firstRange == default)
            {
                return 0;
            }

            return _firstRange.Start.CompareTo(interval._firstRange.Start);
        }

        public override string ToString()
        {
            IEnumerable<LiveRange> GetRanges()
            {
                LiveRange curr = _firstRange;

                while (curr != default)
                {
                    yield return curr;

                    curr = curr.Next;
                }
            }

            return string.Join("; ", GetRanges());
        }
    }
}