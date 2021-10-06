using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.Optimizations
{
    class TailMerge
    {
        public static void RunPass(in CompilerContext cctx)
        {
            if (cctx.FuncReturnType != OperandType.I64)
            {
                return;
            }

            ControlFlowGraph cfg = cctx.Cfg;

            BasicBlock mergedReturn = new(cfg.Blocks.Count);

            Operand returnValue = cfg.AllocateLocal(OperandType.I64);
            Operation returnOp = Operation(Instruction.Return, default, returnValue);

            mergedReturn.Frequency = BasicBlockFrequency.Cold;
            mergedReturn.Operations.AddLast(returnOp);

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                Operation op = block.Operations.Last;

                if (op != default && op.Instruction == Instruction.Return)
                {
                    Operation copyOp = Operation(Instruction.Copy, returnValue, op.GetSource(0));

                    block.Operations.Remove(op);

                    BasicBlock mergeBlock = PrepareMerge(block, mergedReturn);
                    mergeBlock.Append(copyOp);
                }
            }

            cfg.Blocks.AddLast(mergedReturn);
            cfg.Update();
        }

        static BasicBlock PrepareMerge(BasicBlock from, BasicBlock to)
        {
            BasicBlock fromPred = from.Predecessors.Count == 1 ? from.Predecessors[0] : null;

            // If the block is empty, we can try to append to the predecessor and avoid unnecessary jumps.
            if (from.Operations.Count == 0 && fromPred != null)
            {
                for (int i = 0; i < fromPred.SuccessorsCount; i++)
                {
                    if (fromPred.GetSuccessor(i) == from)
                    {
                        fromPred.SetSuccessor(i, to);
                    }
                }

                // NOTE: `from` becomes unreachable and the call to `cfg.Update()` will remove it.
                return fromPred;
            }
            else
            {
                from.AddSuccessor(to);

                return from;
            }
        }
    }
}
