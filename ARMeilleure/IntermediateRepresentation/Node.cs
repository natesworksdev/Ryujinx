using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ARMeilleure.IntermediateRepresentation
{
    class Node : IIntrusiveListNode<Node>
    {
        private const int InlinedDestinationsCount = 1;
        private const int InlinedSourcesCount = 3;

        private int _srcCount;
        private int _destCount;
        private Operand _dest0;
        private Operand _src0;
#pragma warning disable CS0414
        private Operand _src1;
        private Operand _src2;
#pragma warning restore CS0414
        private Operand[] _operands;

        public Node ListPrevious { get; set; }
        public Node ListNext { get; set; }

        public Operand Destination
        {
            get => _destCount != 0 ? GetDestination(0) : null;
            set => SetDestination(value);
        }

        public int DestinationsCount => _destCount;
        public int SourcesCount => _srcCount;

        public Node() { }

        public Node(Operand dest, int srcCount) : this()
        {
            Destination = dest;

            EnsureSourcesCount(srcCount);
        }

        private void Reset(int destCount, int srcCount)
        {
            ListPrevious = null;
            ListNext = null;

            _dest0 = null;
            _src0 = null;
            _src1 = null;
            _src2 = null;

            int extraDest = Math.Max(0, destCount - InlinedDestinationsCount);
            int extraSrc = Math.Max(0, srcCount - InlinedSourcesCount);
            int extra = extraDest + extraSrc;

            EnsureExtra(extra);

            if (extra == 0)
            {
                _srcCount = srcCount;
                _destCount = destCount;

                return;
            }

            EnsureDestinationsCount(destCount);
            EnsureSourcesCount(srcCount);

            // Store _operands in a local variable to avoid bound checks below.
            var operands = _operands;

            for (int i = 0; i < operands.Length; i++)
            {
                operands[i] = null;
            }
        }

        public Node With(Operand dest, int srcCount)
        {
            Reset(destCount: 1, srcCount);
            Destination = dest;

            return this;
        }

        public Node With(Operand[] dest, int srcCount)
        {
            Reset(dest.Length, srcCount);
            SetDestinations(dest ?? throw new ArgumentNullException(nameof(dest)));

            return this;
        }

        public Operand GetDestination(int index)
        {
            return GetDestinationRef(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Operand GetDestinationRef(int index)
        {
            if ((uint)index >= _destCount)
            {
                ThrowIndexOutOfRange(nameof(index));
            }

            if (index < InlinedDestinationsCount)
            {
                return ref Unsafe.Add(ref _dest0, index);
            }

            ref Operand extra = ref MemoryMarshal.GetArrayDataReference(_operands);

            return ref Unsafe.Add(ref extra, Math.Max(0, _srcCount - InlinedSourcesCount) + index - InlinedDestinationsCount);
        }

        public Operand GetSource(int index)
        {
            return GetSourceRef(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Operand GetSourceRef(int index)
        {
            if ((uint)index >= _srcCount)
            {
                ThrowIndexOutOfRange(nameof(index));
            }

            if (index < InlinedSourcesCount)
            {
                return ref Unsafe.Add(ref _src0, index);
            }

            ref Operand extra = ref MemoryMarshal.GetArrayDataReference(_operands);

            return ref Unsafe.Add(ref extra, index - InlinedSourcesCount);
        }

        public void SetDestination(int index, Operand dest)
        {
            // No need to worry about curDest going stale because _operands is not resized.
            ref Operand curDest = ref GetDestinationRef(index);

            RemoveAssignment(curDest);
            AddAssignment(dest);

            curDest = dest;
        }

        public void SetSource(int index, Operand src)
        {
            // No need to worry about curSrc going stale because _operands is not resized.
            ref Operand curSrc = ref GetSourceRef(index);

            RemoveUse(curSrc);
            AddUse(src);

            curSrc = src;
        }

        public void SetDestination(Operand dest)
        {
            RemoveOldDestinations();

            if (dest == null)
            {
                EnsureDestinationsCount(0);
            }
            else
            {
                EnsureDestinationsCount(1);

                GetDestinationRef(0) = dest;
                AddAssignment(dest);
            }
        }

        public void SetDestinations(Operand[] dest)
        {
            RemoveOldDestinations();
            EnsureDestinationsCount(dest.Length);

            for (int index = 0; index < dest.Length; index++)
            {
                Operand newOp = dest[index];

                GetDestinationRef(index) = newOp;
                AddAssignment(newOp);
            }
        }

        public void SetSource(Operand src)
        {
            RemoveOldSources();

            if (src == null)
            {
                EnsureSourcesCount(0);
            }
            else
            {
                EnsureSourcesCount(1);

                GetSourceRef(0) = src;
                AddUse(src);
            }
        }

        public void SetSources(Operand[] src)
        {
            RemoveOldSources();
            EnsureSourcesCount(src.Length);

            for (int index = 0; index < src.Length; index++)
            {
                Operand newOp = src[index];

                GetSourceRef(index) = newOp;
                AddUse(newOp);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveOldSources()
        {
            for (int index = 0; index < _srcCount; index++)
            {
                RemoveUse(GetSource(index));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveOldDestinations()
        {
            for (int index = 0; index < _destCount; index++)
            {
                RemoveAssignment(GetDestination(index));
            }
        }

        private void EnsureDestinationsCount(int count)
        {
            int destExtra = Math.Max(0, count - InlinedDestinationsCount);
            int oldDestExtra = Math.Max(0, _destCount - InlinedDestinationsCount);

            // Extra space for destinations has not changed, we can exit early.
            if (destExtra == oldDestExtra)
            {
                _destCount = count;

                return;
            }

            int srcExtra = Math.Max(0, _srcCount - InlinedSourcesCount);
            int extraCount = srcExtra + destExtra;

            var oldOperands = _operands;
            var operands = EnsureExtra(extraCount);

            if (oldOperands != null && operands != null && _srcCount > InlinedSourcesCount)
            {
                // Copy old sources into the operands array. No need to worry about destinations, they are considered
                // thrashed after this.
                Array.Copy(oldOperands, 0, operands, 0, _srcCount - InlinedSourcesCount);
            }

            _destCount = count;
        }

        private void EnsureSourcesCount(int count)
        {
            int srcExtra = Math.Max(0, count - InlinedSourcesCount);
            int oldSrcExtra = Math.Max(0, _srcCount - InlinedSourcesCount);

            // Extra space for sources has not changed, we can exit early.
            if (srcExtra == oldSrcExtra)
            {
                _srcCount = count;

                return;
            }

            int destExtra = Math.Max(0, _destCount - InlinedDestinationsCount);
            int extraCount = destExtra + srcExtra;

            var oldOperands = _operands;
            var operands = EnsureExtra(extraCount);

            if (oldOperands != null && operands != null && _destCount > InlinedDestinationsCount)
            {
                // Copy old destinations into the operands array. No need to worry about sources, they are considered
                // thrashed after this.
                Array.Copy(oldOperands, oldSrcExtra, operands, srcExtra, _destCount - InlinedDestinationsCount);
            }

            _srcCount = count;
        }

        private Operand[] EnsureExtra(int count)
        {
            int n = _operands != null ? _operands.Length : 0;

            // If no extra operands are needed, allow the array to be garbaged collected if its large enough.
            if (count == 0)
            {
                _operands = null;
            }
            else if (count > n)
            {
                _operands = new Operand[count];
            }

            return _operands;
        }

        private void AddAssignment(Operand op)
        {
            if (op == null)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Assignments.Add(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = (MemoryOperand)op;

                if (memOp.BaseAddress != null)
                {
                    memOp.BaseAddress.Assignments.Add(this);
                }
                
                if (memOp.Index != null)
                {
                    memOp.Index.Assignments.Add(this);
                }
            }
        }

        private void RemoveAssignment(Operand op)
        {
            if (op == null)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Assignments.Remove(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = (MemoryOperand)op;

                if (memOp.BaseAddress != null)
                {
                    memOp.BaseAddress.Assignments.Remove(this);
                }

                if (memOp.Index != null)
                {
                    memOp.Index.Assignments.Remove(this);
                }
            }
        }

        private void AddUse(Operand op)
        {
            if (op == null)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Uses.Add(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = (MemoryOperand)op;

                if (memOp.BaseAddress != null)
                {
                    memOp.BaseAddress.Uses.Add(this);
                }

                if (memOp.Index != null)
                {
                    memOp.Index.Uses.Add(this);
                }
            }
        }

        private void RemoveUse(Operand op)
        {
            if (op == null)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Uses.Remove(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = (MemoryOperand)op;

                if (memOp.BaseAddress != null)
                {
                    memOp.BaseAddress.Uses.Remove(this);
                }

                if (memOp.Index != null)
                {
                    memOp.Index.Uses.Remove(this);
                }
            }
        }

        private static void ThrowIndexOutOfRange(string param) => throw new ArgumentOutOfRangeException(param);
    }
}