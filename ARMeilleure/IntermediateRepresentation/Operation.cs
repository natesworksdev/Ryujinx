using System;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe struct Operation : IIntrusiveListNode<Operation>
    {
        private struct Data
        {
            public Intrinsic Intrinsic;
            public Instruction Instruction;
            public NativeList<Operand> Destinations;
            public NativeList<Operand> Sources;
            public Operation ListPrevious;
            public Operation ListNext;
        }

        private Data* _data;

        public Intrinsic Intrinsic
        {
            get => _data->Intrinsic;
            private set => _data->Intrinsic = value;
        }

        public Instruction Instruction
        {
            get => _data->Instruction;
            private set => _data->Instruction = value;
        }

        public Operation ListPrevious
        {
            get => _data->ListPrevious;
            set => _data->ListPrevious = value;
        }

        public Operation ListNext
        {
            get => _data->ListNext;
            set => _data->ListNext = value;
        }

        public Operand Destination
        {
            get => _data->Destinations.Count != 0 ? GetDestination(0) : default;
            set => SetDestination(value);
        }

        public int DestinationsCount => _data->Destinations.Count;
        public int SourcesCount => _data->Sources.Count;

        public void TurnIntoCopy(Operand source)
        {
            Instruction = Instruction.Copy;

            SetSource(source);
        }

        public Operand GetDestination(int index)
        {
            return _data->Destinations[index];
        }

        public Operand GetSource(int index)
        {
            return _data->Sources[index];
        }

        public void SetDestination(int index, Operand destination)
        {
            ref Operand curDest = ref _data->Destinations[index];

            RemoveAssignment(curDest);
            AddAssignment(destination);

            curDest = destination;
        }

        public void SetSource(int index, Operand source)
        {
            ref Operand curSrc = ref _data->Sources[index];

            RemoveUse(curSrc);
            AddUse(source);

            curSrc = source;
        }

        private void RemoveOldDestinations()
        {
            foreach (ref Operand dest in _data->Destinations.Span)
            {
                RemoveAssignment(dest);
            }
        }

        public void SetDestination(Operand destination)
        {
            RemoveOldDestinations();

            if (destination == default)
            {
                _data->Destinations.Clear();
            }
            else
            {
                Resize(ref _data->Destinations, 1);

                _data->Destinations[0] = destination;

                AddAssignment(destination);
            }
        }

        public void SetDestinations(Operand[] destinations)
        {
            RemoveOldDestinations();

            Resize(ref _data->Destinations, destinations.Length);

            for (int index = 0; index < destinations.Length; index++)
            {
                Operand newOp = destinations[index];

                _data->Destinations[index] = newOp;

                AddAssignment(newOp);
            }
        }

        private void RemoveOldSources()
        {
            foreach (ref Operand src in _data->Sources.Span)
            {
                RemoveUse(src);
            }
        }

        public void SetSource(Operand source)
        {
            RemoveOldSources();

            if (source == default)
            {
                _data->Sources.Clear();
            }
            else
            {
                Resize(ref _data->Sources, 1);

                _data->Sources[0] = source;

                AddUse(source);
            }
        }

        public void SetSources(Operand[] sources)
        {
            RemoveOldSources();

            Resize(ref _data->Sources, sources.Length);

            for (int index = 0; index < sources.Length; index++)
            {
                Operand newOp = sources[index];

                _data->Sources[index] = newOp;

                AddUse(newOp);
            }
        }

        private void AddAssignment(Operand op)
        {
            if (op == default)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Assignments.Add(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = op.GetMemory();

                if (memOp.BaseAddress != default)
                {
                    memOp.BaseAddress.Assignments.Add(this);
                }
                
                if (memOp.Index != default)
                {
                    memOp.Index.Assignments.Add(this);
                }
            }
        }

        private void RemoveAssignment(Operand op)
        {
            if (op == default)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Assignments.Remove(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = op.GetMemory();

                if (memOp.BaseAddress != default)
                {
                    memOp.BaseAddress.Assignments.Remove(this);
                }

                if (memOp.Index != default)
                {
                    memOp.Index.Assignments.Remove(this);
                }
            }
        }

        private void AddUse(Operand op)
        {
            if (op == default)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Uses.Add(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = op.GetMemory();

                if (memOp.BaseAddress != default)
                {
                    memOp.BaseAddress.Uses.Add(this);
                }

                if (memOp.Index != default)
                {
                    memOp.Index.Uses.Add(this);
                }
            }
        }

        private void RemoveUse(Operand op)
        {
            if (op == default)
            {
                return;
            }

            if (op.Kind == OperandKind.LocalVariable)
            {
                op.Uses.Remove(this);
            }
            else if (op.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = op.GetMemory();

                if (memOp.BaseAddress != default)
                {
                    memOp.BaseAddress.Uses.Remove(this);
                }

                if (memOp.Index != default)
                {
                    memOp.Index.Uses.Remove(this);
                }
            }
        }

        private static void Resize(ref NativeList<Operand> list, int size)
        {
            if (list.Count > size)
            {
                while (list.Count > size)
                {
                    list.RemoveAt(list.Count - 1);
                }
            } 
            else
            {
                while (list.Count < size)
                {
                    list.Add(default);
                }
            }
        }

        public bool Equals(Operation operation)
        {
            return operation._data == _data;
        }

        public override bool Equals(object obj)
        {
            return obj is Operation operation && Equals(operation);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((IntPtr)_data);
        }

        public static bool operator ==(Operation a, Operation b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Operation a, Operation b)
        {
            return !a.Equals(b);
        }

        public static class Factory
        {
            private static Operation Make(Instruction inst, int destCount, int srcCount)
            {
                Data* data = Arena<Data>.Alloc();
                *data = default;

                Operation result = new();
                result._data = data;
                result._data->Instruction = inst;
                result._data->Destinations = NativeList<Operand>.New(destCount);
                result._data->Sources = NativeList<Operand>.New(srcCount);

                Resize(ref result._data->Destinations, destCount);
                Resize(ref result._data->Sources, srcCount);

                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest)
            {
                Operation result = Make(inst, 0, 0);
                result.SetDestination(dest);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand src0)
            {
                Operation result = Make(inst, 0, 1);
                result.SetDestination(dest);
                result.SetSource(0, src0);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand src0, Operand src1)
            {
                Operation result = Make(inst, 0, 2);
                result.SetDestination(dest);
                result.SetSource(0, src0);
                result.SetSource(1, src1);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand src0, Operand src1, Operand src2)
            {
                Operation result = Make(inst, 0, 3);
                result.SetDestination(dest);
                result.SetSource(0, src0);
                result.SetSource(1, src1);
                result.SetSource(2, src2);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, int srcCount)
            {
                Operation result = Make(inst, 0, srcCount);
                result.SetDestination(dest);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand[] srcs)
            {
                Operation result = Make(inst, 0, srcs.Length);

                result.SetDestination(dest);

                for (int index = 0; index < srcs.Length; index++)
                {
                    result.SetSource(index, srcs[index]);
                }

                return result;
            }

            public static Operation Operation(Intrinsic intrin, Operand dest, params Operand[] srcs)
            {
                Operation result = Make(Instruction.Extended, 0, srcs.Length);

                result.Intrinsic = intrin;
                result.SetDestination(dest);

                for (int index = 0; index < srcs.Length; index++)
                {
                    result.SetSource(index, srcs[index]);
                }

                return result;
            }

            public static Operation Operation(Instruction inst, Operand[] dests, Operand[] srcs)
            {
                Operation result = Make(inst, dests.Length, srcs.Length);

                for (int index = 0; index < dests.Length; index++)
                {
                    result.SetDestination(index, dests[index]);
                }

                for (int index = 0; index < srcs.Length; index++)
                {
                    result.SetSource(index, srcs[index]);
                }

                return result;
            }
        }
    }
}