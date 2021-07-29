using ARMeilleure.IntermediateRepresentation;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.Translation
{
    static partial class Ssa
    {
        public static void Deconstruct(ControlFlowGraph cfg)
        {
            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                Operation phi = block.Operations.First;

                while (phi != default && phi.Instruction == Instruction.Phi)
                {
                    Operation nextNode = phi.ListNext;

                    Operand local = Local(phi.Destination.Type);

                    for (int index = 0; index < phi.SourcesCount / 2; index++)
                    {
                        BasicBlock predecessor = cfg.PostOrderBlocks[cfg.PostOrderMap[phi.GetSource(index * 2).AsInt32()]];

                        Operand source = phi.GetSource(index * 2 + 1);

                        predecessor.Append(Operation(Instruction.Copy, local, source));

                        phi.SetSource(index * 2 + 1, default);
                    }

                    Operation copyOp = Operation(Instruction.Copy, phi.Destination, local);

                    block.Operations.AddBefore(phi, copyOp);

                    phi.Destination = default;

                    block.Operations.Remove(phi);

                    phi = nextNode;
                }
            }
        }
    }
}