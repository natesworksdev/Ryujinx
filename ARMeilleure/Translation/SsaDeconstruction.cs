using ARMeilleure.IntermediateRepresentation;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using static ARMeilleure.IntermediateRepresentation.OperationHelper;

namespace ARMeilleure.Translation
{
    static partial class Ssa
    {
        public static void Deconstruct(ControlFlowGraph cfg)
        {
            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                Node node = block.Operations.First;

                while (node is Operation phi && phi.Instruction == Instruction.Phi)
                {
                    Node nextNode = node.ListNext;

                    Operand local = Local(phi.Destination.Type);

                    for (int index = 0; index < phi.SourcesCount / 2; index++)
                    {
                        BasicBlock predecessor = cfg.PostOrderBlocks[cfg.PostOrderMap[phi.GetSource(index * 2).AsInt32()]];

                        Operand source = phi.GetSource(index * 2 + 1);

                        predecessor.Append(Operation(Instruction.Copy, local, source));

                        phi.SetSource(index * 2 + 1, null);
                    }

                    Operation copyOp = Operation(Instruction.Copy, phi.Destination, local);

                    block.Operations.AddBefore(node, copyOp);

                    phi.Destination = null;

                    block.Operations.Remove(node);

                    node = nextNode;
                }
            }
        }
    }
}