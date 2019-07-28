using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Node
    {
        public Operand Dest
        {
            get
            {
                return _destinations.Length != 0 ? GetDestination(0) : null;
            }
            set
            {
                if (value != null)
                {
                    SetDestinations(new Operand[] { value });
                }
                else
                {
                    SetDestinations(new Operand[0]);
                }
            }
        }

        private Operand[] _destinations;
        private Operand[] _sources;

        private LinkedListNode<Node>[] _asgUseNodes;
        private LinkedListNode<Node>[] _srcUseNodes;

        public int DestinationsCount => _destinations.Length;
        public int SourcesCount      => _sources.Length;

        public Node(Operand destination, int sourcesCount)
        {
            Dest = destination;

            _sources = new Operand[sourcesCount];

            _srcUseNodes = new LinkedListNode<Node>[sourcesCount];
        }

        public Node(Operand[] destinations, int sourcesCount)
        {
            SetDestinations(destinations);

            _sources = new Operand[sourcesCount];

            _srcUseNodes = new LinkedListNode<Node>[sourcesCount];
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
            Set(_destinations, _asgUseNodes, index, destination);
        }

        public void SetSource(int index, Operand source)
        {
            Set(_sources, _srcUseNodes, index, source);
        }

        private void Set(Operand[] ops, LinkedListNode<Node>[] uses, int index, Operand newOp)
        {
            Operand oldOp = ops[index];

            if (oldOp != null && oldOp.Kind == OperandKind.LocalVariable)
            {
                oldOp.Uses.Remove(uses[index]);
            }

            if (newOp != null && newOp.Kind == OperandKind.LocalVariable)
            {
                uses[index] = newOp.Uses.AddLast(this);
            }

            ops[index] = newOp;
        }

        public void SetDestinations(Operand[] destinations)
        {
            Set(ref _destinations, ref _asgUseNodes, destinations);
        }

        public void SetSources(Operand[] sources)
        {
            Set(ref _sources, ref _srcUseNodes, sources);
        }

        private void Set(ref Operand[] ops, ref LinkedListNode<Node>[] uses, Operand[] newOps)
        {
            if (ops != null)
            {
                for (int index = 0; index < ops.Length; index++)
                {
                    Operand oldOp = ops[index];

                    if (oldOp != null && oldOp.Kind == OperandKind.LocalVariable)
                    {
                        oldOp.Uses.Remove(uses[index]);
                    }
                }
            }

            ops = new Operand[newOps.Length];

            uses = new LinkedListNode<Node>[newOps.Length];

            for (int index = 0; index < newOps.Length; index++)
            {
                Operand newOp = newOps[index];

                ops[index] = newOp;

                if (newOp.Kind == OperandKind.LocalVariable)
                {
                    uses[index] = newOp.Uses.AddLast(this);
                }
            }
        }
    }
}