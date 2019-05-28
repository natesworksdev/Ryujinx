using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Node
    {
        private Operand _dest;

        private LinkedListNode<Node> _asgUseNode;

        public Operand Dest
        {
            get
            {
                return _dest;
            }
            set
            {
                if (_dest != null && _dest.Kind == OperandKind.LocalVariable)
                {
                    _dest.Assignments.Remove(_asgUseNode);
                }

                if (value != null && value.Kind == OperandKind.LocalVariable)
                {
                    _asgUseNode = value.Assignments.AddLast(this);
                }

                _dest = value;
            }
        }

        protected Operand[] Sources;

        public int SourcesCount => Sources.Length;

        protected LinkedListNode<Node>[] SrcUseNodes;

        public Node(int sourcesCount)
        {
            SrcUseNodes = new LinkedListNode<Node>[sourcesCount];
        }

        public Operand GetSource(int index)
        {
            return Sources[index];
        }

        public void SetSource(int index, Operand source)
        {
            Operand oldSrc = Sources[index];

            if (oldSrc != null && oldSrc.Kind == OperandKind.LocalVariable)
            {
                oldSrc.Uses.Remove(SrcUseNodes[index]);
            }

            if (source != null && source.Kind == OperandKind.LocalVariable)
            {
                SrcUseNodes[index] = source.Uses.AddLast(this);
            }

            Sources[index] = source;
        }
    }
}