using System;
using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Operation : IIntrusiveListNode<Operation>
    {
        public Intrinsic Intrinsic { get; private set; }
        public Instruction Instruction { get; private set; }

        public Operation ListPrevious { get; set; }
        public Operation ListNext { get; set; }

        public Operand Destination
        {
            get => _destinations.Count != 0 ? GetDestination(0) : default;
            set => SetDestination(value);
        }

        private readonly List<Operand> _destinations;
        private readonly List<Operand> _sources;
        private bool _clearedDest;

        public int DestinationsCount => _destinations.Count;
        public int SourcesCount      => _sources.Count;

        public Operation()
        {
            _destinations = new List<Operand>();
            _sources = new List<Operand>();
        }

        public Operation(Operand dest, int srcCount) : this()
        {
            Destination = dest;

            Resize(_sources, srcCount);
        }

        public Operation(Instruction instruction, Operand dest, Operand[] src) : this(dest, src.Length)
        {
            Instruction = instruction;

            for (int index = 0; index < src.Length; index++)
            {
                SetSource(index, src[index]);
            }
        }

        public Operation(Instruction instruction, Operand dest, int srcCount) : this(dest, srcCount)
        {
            Instruction = instruction;
        }

        public Operation(Intrinsic intrin, Operand dest, params Operand[] sources) : this(Instruction.Extended, dest, sources)
        {
            Intrinsic = intrin;
        }

        private void Reset(int sourcesCount)
        {
            _clearedDest = true;
            _sources.Clear();
            ListPrevious = null;
            ListNext = null;

            Resize(_sources, sourcesCount);
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
            return _destinations[index];
        }

        public Operand GetSource(int index)
        {
            return _sources[index];
        }

        public void SetDestination(int index, Operand destination)
        {
            if (!_clearedDest) 
            {
                RemoveAssignment(_destinations[index]);
            }

            AddAssignment(destination);

            _clearedDest = false;

            _destinations[index] = destination;
        }

        public void SetSource(int index, Operand source)
        {
            RemoveUse(_sources[index]);

            AddUse(source);

            _sources[index] = source;
        }

        private void RemoveOldDestinations()
        {
            if (!_clearedDest)
            {
                for (int index = 0; index < _destinations.Count; index++)
                {
                    RemoveAssignment(_destinations[index]);
                }
            }

            _clearedDest = false;
        }

        public void SetDestination(Operand destination)
        {
            RemoveOldDestinations();

            if (destination == default)
            {
                _destinations.Clear();
                _clearedDest = true;
            }
            else
            {
                Resize(_destinations, 1);

                _destinations[0] = destination;

                AddAssignment(destination);
            }
        }

        public void SetDestinations(Operand[] destinations)
        {
            RemoveOldDestinations();

            Resize(_destinations, destinations.Length);

            for (int index = 0; index < destinations.Length; index++)
            {
                Operand newOp = destinations[index];

                _destinations[index] = newOp;

                AddAssignment(newOp);
            }
        }

        private void RemoveOldSources()
        {
            for (int index = 0; index < _sources.Count; index++)
            {
                RemoveUse(_sources[index]);
            }
        }

        public void SetSource(Operand source)
        {
            RemoveOldSources();

            if (source == default)
            {
                _sources.Clear();
            }
            else
            {
                Resize(_sources, 1);

                _sources[0] = source;

                AddUse(source);
            }
        }

        public void SetSources(Operand[] sources)
        {
            RemoveOldSources();

            Resize(_sources, sources.Length);

            for (int index = 0; index < sources.Length; index++)
            {
                Operand newOp = sources[index];

                _sources[index] = newOp;

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

        private void Resize(List<Operand> list, int size)
        {
            if (list.Count > size)
            {
                list.RemoveRange(size, list.Count - size);
            } 
            else
            {
                while (list.Count < size)
                {
                    list.Add(default);
                }
            }
        }
    }
}