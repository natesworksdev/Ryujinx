using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Operation : Node
    {
        public Instruction Inst { get; private set; }

        public Operation(Instruction inst, Operand dest, params Operand[] sources) : base(sources.Length)
        {
            Inst = inst;
            Dest = dest;

            //The array may be modified externally, so we store a copy.
            Sources = (Operand[])sources.Clone();

            for (int index = 0; index < Sources.Length; index++)
            {
                Operand source = Sources[index];

                if (source.Kind == OperandKind.LocalVariable)
                {
                    SrcUseNodes[index] = source.Uses.AddLast(this);
                }
            }
        }

        public void TurnIntoCopy(Operand source)
        {
            Inst = Instruction.Copy;

            for (int index = 0; index < Sources.Length; index++)
            {
                if (Sources[index].Kind != OperandKind.LocalVariable)
                {
                    continue;
                }

                Sources[index].Uses.Remove(SrcUseNodes[index]);
            }

            Sources = new Operand[] { source };

            SrcUseNodes = new LinkedListNode<Node>[1];

            if (source.Kind == OperandKind.LocalVariable)
            {
                SrcUseNodes[0] = source.Uses.AddLast(this);
            }
        }
    }
}