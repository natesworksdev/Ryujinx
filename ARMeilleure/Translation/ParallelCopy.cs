using ARMeilleure.IntermediateRepresentation;
using System.Collections.Generic;

namespace ARMeilleure.Translation
{
    class ParallelCopy
    {
        private struct Copy
        {
            public Operand Dest   { get; }
            public Operand Source { get; }

            public Copy(Operand dest, Operand source)
            {
                Dest   = dest;
                Source = source;
            }
        }

        private List<Copy> _copies;

        private Dictionary<ulong, Operand> _uniqueOperands;

        public ParallelCopy()
        {
            _copies = new List<Copy>();

            _uniqueOperands = new Dictionary<ulong, Operand>();
        }

        public void AddCopy(Operand dest, Operand source)
        {
            _copies.Add(new Copy(GetUnique(dest), GetUnique(source)));
        }

        private Operand GetUnique(Operand operand)
        {
            // Operand is supposed to be a value or reference type based on kind.
            // We differentiate local variables by reference, but everything else
            // is supposed to be considered the same, if "Value" is the same.
            if (operand.Kind != OperandKind.LocalVariable)
            {
                if (_uniqueOperands.TryGetValue(operand.Value, out Operand prevOperand))
                {
                    return prevOperand;
                }

                _uniqueOperands.Add(operand.Value, operand);
            }

            return operand;
        }

        public Operation[] Sequence(Operand temporary)
        {
            List<Operation> sequence = new List<Operation>();

            Dictionary<Operand, Operand> location    = new Dictionary<Operand, Operand>();
            Dictionary<Operand, Operand> predecessor = new Dictionary<Operand, Operand>();

            Queue<Operand> pendingQueue = new Queue<Operand>();

            Queue<Operand> readyQueue = new Queue<Operand>();

            foreach (Copy copy in _copies)
            {
                location.Add(copy.Dest, null);

                predecessor.Add(copy.Source, null);
            }

            foreach (Copy copy in _copies)
            {
                location[copy.Source] = copy.Source;

                predecessor[copy.Dest] = copy.Source;

                pendingQueue.Enqueue(copy.Dest);
            }

            while (pendingQueue.TryDequeue(out Operand current))
            {
                Operand b;

                while (readyQueue.TryDequeue(out b))
                {
                    Operand a = predecessor[b];
                    Operand c = location[a];

                    if (b == null || c == null)
                    {
                        throw new System.Exception("huh?");
                    }

                    sequence.Add(new Operation(Instruction.Copy, b, c));

                    location[a] = b;

                    if (a == c && predecessor[a] != null)
                    {
                        readyQueue.Enqueue(a);
                    }
                }

                b = current;

                if (b != location[predecessor[b]])
                {
                    if (temporary == null || b == null)
                    {
                        throw new System.Exception("huh?");
                    }

                    sequence.Add(new Operation(Instruction.Copy, temporary, b));

                    location[b] = temporary;

                    readyQueue.Enqueue(b);
                }
            }

            return sequence.ToArray();
        }
    }
}