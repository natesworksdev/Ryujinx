using ARMeilleure.Translation;
using System;
using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Operation : IIntrusiveListNode<Operation>
    {
        public Instruction Instruction { get; private set; }
        public Intrinsic Intrinsic { get; }

        public Operation ListPrevious { get; set; }
        public Operation ListNext { get; set; }

        public Operand Destination
        {
            get => _destinations.Count != 0 ? GetDestination(0) : default;
            set => SetDestination(value);
        }

        private readonly List<Operand> _destinations = new List<Operand>();
        private readonly List<Operand> _sources = new List<Operand>();

        public int DestinationsCount => _destinations.Count;
        public int SourcesCount => _sources.Count;

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

        public Operation()
        {
        }

        public Operation(Operand? destination, int sourcesCount)
        {
            if (destination != null)
            {
                Destination = destination.Value;
            }

            Resize(_sources, sourcesCount);
        }

        private void Reset(int sourcesCount)
        {
            _sources.Clear();
            ListPrevious = null;
            ListNext = null;

            Resize(_sources, sourcesCount);
        }

        public Operation With(Operand? destination, int sourcesCount)
        {
            Reset(sourcesCount);
            if (destination != null)
            {
                Destination = destination.Value;
            }
            else
            {
                _destinations.Clear();
            }

            return this;
        }

        public Operation With(Operand[] destinations, int sourcesCount)
        {
            Reset(sourcesCount);
            SetDestinations(destinations ?? throw new ArgumentNullException(nameof(destinations)));

            return this;
        }

        public Operation(Instruction instruction, Operand? destination, int sourcesCount) : this(destination, sourcesCount)
        {
            Instruction = instruction;
        }

        public Operation(Instruction instruction, Operand? destination, Operand[] sources) : this(destination, sources.Length)
        {
            Instruction = instruction;

            for (int index = 0; index < sources.Length; index++)
            {
                SetSource(index, sources[index]);
            }
        }

        public Operation(Intrinsic intrinsic, Operand? destination, Operand source) : this(destination, 1)
        {
            Instruction = Instruction.Extended;
            Intrinsic = intrinsic;
            SetSource(0, source);
        }

        public Operation(Intrinsic intrinsic, Operand? destination, Operand source0, Operand source1) : this(destination, 2)
        {
            Instruction = Instruction.Extended;
            Intrinsic = intrinsic;
            SetSource(0, source0);
            SetSource(1, source1);
        }

        public Operation(
            Intrinsic intrinsic,
            Operand? destination,
            Operand source0,
            Operand source1,
            Operand source2) : this(destination, 3)
        {
            Instruction = Instruction.Extended;
            Intrinsic = intrinsic;
            SetSource(0, source0);
            SetSource(1, source1);
            SetSource(2, source2);
        }

        public Operation With(Instruction instruction, Operand? destination)
        {
            With(destination, 0);
            Instruction = instruction;
            return this;
        }

        public Operation With(Instruction instruction, Operand? destination, Operand[] sources)
        {
            With(destination, sources.Length);
            Instruction = instruction;

            for (int index = 0; index < sources.Length; index++)
            {
                SetSource(index, sources[index]);
            }
            return this;
        }

        public Operation With(Instruction instruction, Operand? destination, 
            Operand source0)
        {
            With(destination, 1);
            Instruction = instruction;

            SetSource(0, source0);
            return this;
        }

        public Operation With(Instruction instruction, Operand? destination,
            Operand source0, Operand source1)
        {
            With(destination, 2);
            Instruction = instruction;

            SetSource(0, source0);
            SetSource(1, source1);
            return this;
        }

        public Operation With(Instruction instruction, Operand? destination, 
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
            _destinations[index] = destination;
        }

        public void SetSource(int index, Operand source)
        {
            _sources[index] = source;
        }

        public void SetDestination(Operand? destination)
        {
            if (destination == null)
            {
                _destinations.Clear();
            }
            else
            {
                Resize(_destinations, 1);

                _destinations[0] = destination.Value;
            }
        }

        public void SetDestinations(Operand[] destinations)
        {
            Resize(_destinations, destinations.Length);

            for (int index = 0; index < destinations.Length; index++)
            {
                Operand newOp = destinations[index];

                _destinations[index] = newOp;
            }
        }

        public void SetSource(Operand source)
        {
            Resize(_sources, 1);

            _sources[0] = source;
        }

        public void SetSources(Operand[] sources)
        {
            Resize(_sources, sources.Length);

            for (int index = 0; index < sources.Length; index++)
            {
                _sources[index] = sources[index];
            }
        }

        public BasicBlock GetPhiIncomingBlock(ControlFlowGraph cfg, int index)
        {
            return cfg.PostOrderBlocks[cfg.PostOrderMap[GetSource(index * 2).AsInt32()]];
        }

        public Operand GetPhiIncomingValue(int index)
        {
            return GetSource(index * 2 + 1);
        }

        public void TurnIntoCopy(Operand source)
        {
            Instruction = Instruction.Copy;

            SetSource(source);
        }
    }
}