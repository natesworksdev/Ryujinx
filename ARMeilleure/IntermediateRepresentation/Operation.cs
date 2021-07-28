using System;
using System.Runtime.InteropServices;

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

        private void Reset(int sourcesCount)
        {
            _data->Sources.Clear();
            ListPrevious = default;
            ListNext = default;

            Resize(ref _data->Sources, sourcesCount);
        }

        public Operation With(Instruction instruction, Operand destination)
        {
            With(destination, 0);
            Instruction = instruction;
            return this;
        }

        public Operation With(Instruction instruction, Operand destination, Operand[] sources)
        {
            With(destination, sources.Length);
            Instruction = instruction;

            for (int index = 0; index < sources.Length; index++)
            {
                SetSource(index, sources[index]);
            }
            return this;
        }

        public Operation With(Instruction instruction, Operand destination, 
            Operand source0)
        {
            With(destination, 1);
            Instruction = instruction;

            SetSource(0, source0);
            return this;
        }

        public Operation With(Instruction instruction, Operand destination,
            Operand source0, Operand source1)
        {
            With(destination, 2);
            Instruction = instruction;

            SetSource(0, source0);
            SetSource(1, source1);
            return this;
        }

        public Operation With(Instruction instruction, Operand destination, 
            Operand source0, Operand source1, Operand source2)
        {
            With(destination, 3);
            Instruction = instruction;

            SetSource(0, source0);
            SetSource(1, source1);
            SetSource(2, source2);
            return this;
        }

        public Operation With(
            Instruction instruction,
            Operand[] destinations,
            Operand[] sources)
        {
            With(destinations, sources.Length);
            Instruction = instruction;

            for (int index = 0; index < sources.Length; index++)
            {
                SetSource(index, sources[index]);
            }
            return this;
        }

        public Operation With(Operand destination, int sourcesCount)
        {
            Reset(sourcesCount);
            Destination = destination;

            return this;
        }

        public Operation With(Operand[] destinations, int sourcesCount)
        {
            Reset(sourcesCount);
            SetDestinations(destinations ?? throw new ArgumentNullException(nameof(destinations)));

            return this;
        }

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
            RemoveAssignment(_data->Destinations[index]);

            AddAssignment(destination);

            _data->Destinations[index] = destination;
        }

        public void SetSource(int index, Operand source)
        {
            RemoveUse(_data->Sources[index]);

            AddUse(source);

            _data->Sources[index] = source;
        }

        private void RemoveOldDestinations()
        {
            for (int index = 0; index < _data->Destinations.Count; index++)
            {
                RemoveAssignment(_data->Destinations[index]);
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
            for (int index = 0; index < _data->Sources.Count; index++)
            {
                RemoveUse(_data->Sources[index]);
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

        public static Operation New()
        {
            var result = new Operation();

            result._data = (Data*)Marshal.AllocHGlobal(sizeof(Data));

            if (result._data == null)
            {
                throw new OutOfMemoryException();
            }

            *result._data = default;
            result._data->Sources = NativeList<Operand>.New();
            result._data->Destinations = NativeList<Operand>.New();

            return result;
        }

        public static Operation New(Operand dest, int srcCount)
        {
            Operation result = New();

            result.Destination = dest;

            Resize(ref result._data->Sources, srcCount);

            return result;
        }

        public static Operation New(Instruction instruction, Operand dest, int srcCount)
        {
            Operation result = New(dest, srcCount);

            result._data->Instruction = instruction;

            return result;
        }

        public static Operation New(Instruction instruction, Operand dest, Operand[] src)
        {
            Operation result = New(instruction, dest, src.Length);

            for (int index = 0; index < src.Length; index++)
            {
                result.SetSource(index, src[index]);
            }

            return result;
        }

        public static Operation New(Intrinsic intrin, Operand dest, params Operand[] src)
        {
            Operation result = New(Instruction.Extended, dest, src);

            result._data->Intrinsic = intrin;

            return result;
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
    }
}