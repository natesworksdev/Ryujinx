using ARMeilleure.Decoders;
using ARMeilleure.Diagnostics;
using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    using PTC;

    public class Translator
    {
        private const ulong CallFlag = InstEmitFlowHelper.CallFlag;

        private readonly MemoryManager _memory;

        private readonly object _locker;

        private readonly Dictionary<ulong, TranslatedFunction> _funcs;
        private readonly ConcurrentDictionary<ulong, TranslatedFunction> _funcsHighCq;

        private readonly ConcurrentStack<RejitRequest> _backgroundStack;

        private readonly AutoResetEvent _backgroundTranslatorEvent;

        private readonly JumpTable _jumpTable;

        private volatile int _threadCount;

        public Translator(MemoryManager memory)
        {
            _memory = memory;

            _locker = new object();

            _funcs       = new Dictionary<ulong, TranslatedFunction>();
            _funcsHighCq = new ConcurrentDictionary<ulong, TranslatedFunction>();

            _backgroundStack = new ConcurrentStack<RejitRequest>();

            _backgroundTranslatorEvent = new AutoResetEvent(false);

            _jumpTable = new JumpTable();

            JitCache.Initialize();

            DirectCallStubs.InitializeStubs();

            if (Ptc.State == PtcState.Enabled)
            {
                Ptc.LoadTranslations(_funcsHighCq, memory.PageTable, _jumpTable);
            }
        }

        private void TranslateStackedSubs()
        {
            while (_threadCount != 0)
            {
                if (_backgroundStack.TryPop(out RejitRequest request))
                {
                    TranslatedFunction func = Translate(_memory, _jumpTable, request.Address, request.Mode, highCq: true);

                    bool isAddressUnique = _funcsHighCq.TryAdd(request.Address, func);

                    Debug.Assert(isAddressUnique, $"The address 0x{request.Address:X16} is not unique.");

                    _jumpTable.RegisterFunction(request.Address, func);
                }
                else
                {
                    _backgroundTranslatorEvent.WaitOne();
                }
            }

            _backgroundTranslatorEvent.Set(); // Wake up any other background translator threads, to encourage them to exit.
        }

        public void Execute(State.ExecutionContext context, ulong address)
        {
            if (Interlocked.Increment(ref _threadCount) == 1)
            {
                if (Ptc.State == PtcState.Enabled)
                {
                    Ptc.MakeAndSaveTranslations(_funcsHighCq, _memory, _jumpTable);
                }

                PtcProfiler.Start();

                Ptc.Disable();

                // Simple heuristic, should be user configurable in future. (1 for 4 core/ht or less, 2 for 6 core+ht etc).
                // All threads are normal priority except from the last, which just fills as much of the last core as the os lets it with a low priority.
                // If we only have one rejit thread, it should be normal priority as highCq code is performance critical.
                // TODO: Use physical cores rather than logical. This only really makes sense for processors with hyperthreading. Requires OS specific code.
                int unboundedThreadCount = Math.Max(1, (Environment.ProcessorCount - 6) / 3);
                int threadCount = Math.Min(3, unboundedThreadCount);

                for (int i = 0; i < threadCount; i++)
                {
                    bool last = i != 0 && i == unboundedThreadCount - 1;

                    Thread backgroundTranslatorThread = new Thread(TranslateStackedSubs)
                    {
                        Name = "CPU.BackgroundTranslatorThread." + i,
                        Priority = last ? ThreadPriority.Lowest : ThreadPriority.Normal
                    };

                    backgroundTranslatorThread.Start();
                }
            }

            Statistics.InitializeTimer();

            NativeInterface.RegisterThread(context, _memory, this);

            do
            {
                address = ExecuteSingle(context, address);
            }
            while (context.Running && (address & ~1UL) != 0);

            NativeInterface.UnregisterThread();

            if (Interlocked.Decrement(ref _threadCount) == 0)
            {
                _backgroundTranslatorEvent.Set();
            }
        }

        public ulong ExecuteSingle(State.ExecutionContext context, ulong address)
        {
            TranslatedFunction func = GetOrTranslate(address, context.ExecutionMode);

            Statistics.StartTimer();

            ulong nextAddr = func.Execute(context);

            Statistics.StopTimer(address);

            return nextAddr;
        }

        internal TranslatedFunction GetOrTranslate(ulong address, ExecutionMode mode)
        {
            const int MinCallsForRejit = 100;

            // TODO: Investigate how we should handle code at unaligned addresses.
            // Currently, those low bits are used to store special flags.
            bool isCallTarget = (address & CallFlag) != 0;

            address &= ~CallFlag;

            TranslatedFunction func;

            if (!_funcsHighCq.TryGetValue(address, out func))
            {
                lock (_locker)
                {
                    if (!_funcs.TryGetValue(address, out func))
                    {
                        func = Translate(_memory, _jumpTable, address, mode, highCq: false);

                        bool isAddressUnique = _funcs.TryAdd(address, func);

                        Debug.Assert(isAddressUnique, $"The address 0x{address:X16} is not unique.");
                    }
                }

                if (isCallTarget)
                {
                    int callCount = func.GetCallCount();

                    if (PtcProfiler.Enabled && callCount == 1)
                    {
                        PtcProfiler.AddEntry(address, mode);
                    }
                    else if (callCount == MinCallsForRejit)
                    {
                        _backgroundStack.Push(new RejitRequest(address, mode));

                        _backgroundTranslatorEvent.Set();
                    }
                }
            }

            return func;
        }

        internal static TranslatedFunction Translate(MemoryManager memory, JumpTable jumpTable, ulong address, ExecutionMode mode, bool highCq)
        {
            const bool AlwaysTranslateFunctions = true; // If false, only translates a single block for lowCq.

            ArmEmitterContext context = new ArmEmitterContext(memory, jumpTable, (long)address, highCq, Aarch32Mode.User);

            Logger.StartPass(PassName.Decoding);

            Block[] blocks = AlwaysTranslateFunctions
                ? Decoder.DecodeFunction  (memory, address, mode, highCq)
                : Decoder.DecodeBasicBlock(memory, address, mode);

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

            RegisterUsage.RunPass(cfg, mode, isCompleteFunction: false);

            Logger.EndPass(PassName.RegisterUsage);

            OperandType[] argTypes = new OperandType[] { OperandType.I64 };

            CompilerOptions options = highCq ? CompilerOptions.HighCq : CompilerOptions.None;

            GuestFunction func;

            if (Ptc.State == PtcState.Disabled)
            {
                func = Compiler.Compile<GuestFunction>(cfg, argTypes, OperandType.I64, options);
            }
            else
            {
                using (PtcInfo ptcInfo = new PtcInfo())
                {
                    func = Compiler.Compile<GuestFunction>(cfg, argTypes, OperandType.I64, options, ptcInfo);

                    if ((int)ptcInfo.CodeStream.Length >= Ptc.MinCodeLengthToSave)
                    {
                        Ptc.WriteInfoCodeReloc((long)address, ptcInfo);
                    }
                }
            }

            return new TranslatedFunction(func, highCq);
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

                        // If this is the last op on the block, and there's no "next" block
                        // after this one, then we have to return right now, with the address
                        // of the next instruction to be executed (in the case that the condition
                        // is false, and the branch was not taken, as all basic blocks should end
                        // with some kind of branch).
                        if (isLastOp && block.Next == null)
                        {
                            InstEmitFlowHelper.EmitTailContinue(context, Const(opCode.Address + (ulong)opCode.OpCodeSizeInBytes));
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

            Operand running = context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.CheckSynchronization)));

            context.BranchIfTrue(lblExit, running);

            context.Return(Const(0L));

            context.Branch(lblExit);

            context.MarkLabel(lblNonZero);

            count = context.Subtract(count, Const(1));

            context.Store(countAddr, count);

            context.MarkLabel(lblExit);
        }
    }
}
