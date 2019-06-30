using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    static class PhiFunctions
    {
        public static void Remove(ControlFlowGraph cfg)
        {
            foreach (BasicBlock block in cfg.Blocks)
            {
                LinkedListNode<Node> node = block.Operations.First;

                while (node?.Value is PhiNode phi)
                {
                    LinkedListNode<Node> nextNode = node.Next;

                    Operand local = Local(phi.Dest.Type);

                    for (int index = 0; index < phi.SourcesCount; index++)
                    {
                        BasicBlock predecessor = phi.GetBlock(index);

                        Operand source = phi.GetSource(index);

                        predecessor.Append(new Operation(Instruction.Copy, local, source));

                        phi.SetSource(index, null);
                    }

                    Operation copyOp = new Operation(Instruction.Copy, phi.Dest, local);

                    block.Operations.AddBefore(node, copyOp);

                    phi.Dest = null;

                    block.Operations.Remove(node);

                    node = nextNode;
                }
            }
        }
    }
}