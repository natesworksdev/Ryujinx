using ARMeilleure.Decoders;
using ARMeilleure.Diagnostics;
using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.State;
using System;
using System.Collections.Concurrent;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    public class Translator
    {
        private MemoryManager _memory;

        private ConcurrentDictionary<ulong, TranslatedFunction> _funcs;

        public Translator(MemoryManager memory)
        {
            _memory = memory;

            _funcs = new ConcurrentDictionary<ulong, TranslatedFunction>();
        }

        public void Execute(ExecutionContext context, ulong address)
        {
            Statistics.InitializeTimer();

            NativeInterface.RegisterThread(context, _memory);

            do
            {
                address = ExecuteSingle(context, address);
            }
            while (context.Running && address != 0);

            NativeInterface.UnregisterThread();
        }

        public ulong ExecuteSingle(ExecutionContext context, ulong address)
        {
            TranslatedFunction func = GetOrTranslate(address, ExecutionMode.Aarch64);

            Statistics.StartTimer();

            ulong nextAddr = func.Execute(context);

            Statistics.StopTimer(address);

            return nextAddr;
        }

        private TranslatedFunction GetOrTranslate(ulong address, ExecutionMode mode)
        {
            if (!_funcs.TryGetValue(address, out TranslatedFunction func))
            {
                func = Translate(address, mode);

                _funcs.TryAdd(address, func);
            }

            return func;
        }

        private TranslatedFunction Translate(ulong address, ExecutionMode mode)
        {
            ArmEmitterContext context = new ArmEmitterContext(_memory, Aarch32Mode.User);

            Logger.StartPass(PassName.Decoding);

            Block[] blocks = Decoder.DecodeFunction(_memory, address, ExecutionMode.Aarch64);

            Logger.EndPass(PassName.Decoding);

            Logger.StartPass(PassName.Translation);

            EmitSynchronization(context);

            if (blocks[0].Address != address)
            {
                context.Branch(context.GetLabel(address));
            }

            ControlFlowGraph cfg = EmitAndGetCFG(context, blocks);

            Logger.EndPass(PassName.Translation);

            Logger.StartPass(PassName.RegisterUsage);

            RegisterUsage.RunPass(cfg);

            Logger.EndPass(PassName.RegisterUsage);

            OperandType[] argTypes = new OperandType[] { OperandType.I64 };

            GuestFunction func = Compiler.Compile<GuestFunction>(cfg, argTypes, OperandType.I64);

            return new TranslatedFunction(func);
        }

        private static ControlFlowGraph EmitAndGetCFG(ArmEmitterContext context, Block[] blocks)
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
                        EmitSynchronization(context);
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

        private static void EmitSynchronization(EmitterContext context)
        {
            long countOffs = NativeContext.GetCounterOffset();

            Operand countAddr = context.Add(context.LoadArgument(OperandType.I64, 0), Const(countOffs));

            Operand count = context.Load(OperandType.I32, countAddr);

            Operand lblNonZero = Label();
            Operand lblExit    = Label();

            context.BranchIfTrue(lblNonZero, count);

            context.Call(new _Void(NativeInterface.CheckSynchronization));

            context.Branch(lblExit);

            context.MarkLabel(lblNonZero);

            count = context.Subtract(count, Const(1));

            context.Store(countAddr, count);

            context.MarkLabel(lblExit);
        }
    }
}