using ARMeilleure.CodeGen.X86;
using ARMeilleure.Decoders;
using ARMeilleure.Diagnostics;
using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.State;
using System;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    public class Translator
    {
        private MemoryManager _memory;

        private TranslatedCache _cache;

        public Translator(MemoryManager memory)
        {
            _memory = memory;

            _cache = new TranslatedCache();
        }

        public void Execute(ExecutionContext context, ulong address)
        {
            NativeInterface.RegisterThread(context, _memory);

            do
            {
                TranslatedFunction func = Translate(address, ExecutionMode.Aarch64);

                address = func.Execute(context);
            }
            while (address != 0);

            NativeInterface.UnregisterThread();
        }

        private TranslatedFunction Translate(ulong address, ExecutionMode mode)
        {
            EmitterContext context = new EmitterContext();

            context.Memory = _memory;

            Block[] blocks = Decoder.DecodeFunction(_memory, address, ExecutionMode.Aarch64);

            ControlFlowGraph cfg = EmitAndGetCFG(context, blocks);

            RegisterUsage.InsertContext(cfg);

            Dominance.FindDominators(cfg);

            Dominance.FindDominanceFrontiers(cfg);

            Logger.StartPass(PassName.SsaConstruction);

            Ssa.Rename(cfg);

            Logger.EndPass(PassName.SsaConstruction, cfg);

            byte[] code = CodeGenerator.Generate(cfg, _memory);

            return _cache.CreateFunction(code);
        }

        private static ControlFlowGraph EmitAndGetCFG(EmitterContext context, Block[] blocks)
        {
            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                Block block = blocks[blkIndex];

                context.CurrBlock = block;

                context.MarkLabel(context.GetLabel(block.Address));

                for (int opcIndex = 0; opcIndex < block.OpCodes.Count; opcIndex++)
                {
                    OpCode opCode = block.OpCodes[opcIndex];

                    context.CurrOp = opCode;

                    bool isLastOp = opcIndex == block.OpCodes.Count - 1;

                    if (isLastOp && block.Branch != null && block.Branch.Address <= block.Address)
                    {
                        context.Synchronize();
                    }

                    Operand lblPredicateSkip = null;

                    if (opCode is OpCode32 op && op.Cond < Condition.Al)
                    {
                        lblPredicateSkip = Label();

                        InstEmitFlowHelper.EmitCondBranch(context, lblPredicateSkip, op.Cond.Invert());
                    }

                    if (opCode.Instruction.Emitter != null)
                    {
                        opCode.Instruction.Emitter(context);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid instruction \"{opCode.Instruction.Name}\".");
                    }

                    if (lblPredicateSkip != null)
                    {
                        context.MarkLabel(lblPredicateSkip);

                        //If this is the last op on the block, and there's no "next" block
                        //after this one, then we have to return right now, with the address
                        //of the next instruction to be executed (in the case that the condition
                        //is false, and the branch was not taken, as all basic blocks should end
                        //with some kind of branch).
                        if (isLastOp && block.Next == null)
                        {
                            context.Return(Const(opCode.Address + (ulong)opCode.OpCodeSizeInBytes));
                        }
                    }
                }
            }

            return context.GetControlFlowGraph();
        }
    }
}