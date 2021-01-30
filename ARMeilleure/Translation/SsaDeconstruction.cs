using ARMeilleure.IntermediateRepresentation;

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

                    Operand local = cfg.AllocateLocal(phi.Destination.Type);

                    for (int index = 0; index < phi.SourcesCount / 2; index++)
                    {
                        BasicBlock predecessor = phi.GetPhiIncomingBlock(cfg, index);

                        Operand source = phi.GetPhiIncomingValue(index);

                        predecessor.Append(Operation(Instruction.Copy, local, source));
                    }

                    Operation copyOp = Operation(Instruction.Copy, phi.Destination, local);

                    block.Operations.AddBefore(node, copyOp);

                    block.Operations.Remove(node);

                    node = nextNode;
                }
            }
        }
    }
}