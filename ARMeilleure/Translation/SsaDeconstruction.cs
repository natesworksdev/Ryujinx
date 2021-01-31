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
                var operation = block.Operations.First;

                while (operation != null && operation.Instruction == Instruction.Phi)
                {
                    var nextNode = operation.ListNext;

                    Operand local = cfg.AllocateLocal(operation.Destination.Type);

                    for (int index = 0; index < operation.SourcesCount / 2; index++)
                    {
                        BasicBlock predecessor = operation.GetPhiIncomingBlock(cfg, index);

                        Operand source = operation.GetPhiIncomingValue(index);

                        predecessor.Append(Operation(Instruction.Copy, local, source));
                    }

                    Operation copyOp = Operation(Instruction.Copy, operation.Destination, local);

                    block.Operations.AddBefore(operation, copyOp);

                    block.Operations.Remove(operation);

                    operation = nextNode;
                }
            }
        }
    }
}