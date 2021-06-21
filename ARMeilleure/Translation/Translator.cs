using ARMeilleure.Common;
using ARMeilleure.Decoders;
using ARMeilleure.Diagnostics;
using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.Signal;
using ARMeilleure.State;
using ARMeilleure.Translation.Cache;
using ARMeilleure.Translation.PTC;
using ARMeilleure.Translation.TTC;
using Ryujinx.Common;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

using static ARMeilleure.Common.BitMapPool;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using static ARMeilleure.IntermediateRepresentation.OperationHelper;

namespace ARMeilleure.Translation
{
    public class Translator
    {
        internal const int MinsCallForRejit = 100;

        private static readonly AddressTable<ulong>.Level[] Levels64Bit =
            new AddressTable<ulong>.Level[]
            {
                new(31, 17),
                new(23,  8),
                new(15,  8),
                new( 7,  8),
                new( 2,  5)
            };

        private static readonly AddressTable<ulong>.Level[] Levels32Bit =
            new AddressTable<ulong>.Level[]
            {
                new(31, 17),
                new(23,  8),
                new(15,  8),
                new( 7,  8),
                new( 1,  6)
            };

        private readonly IJitMemoryAllocator _allocator;

        private readonly ConcurrentDictionary<ulong, TranslatedFunction> _oldFuncs;

        private readonly ConcurrentDictionary<ulong, object> _backgroundSet;
        private readonly ConcurrentStack<RejitRequest> _backgroundStack;
        private readonly AutoResetEvent _backgroundTranslatorEvent;
        private readonly ReaderWriterLock _backgroundTranslatorLock;

        internal ConcurrentDictionary<ulong, TranslatedFunction> Functions { get; }
        internal ConcurrentDictionary<Hash128, TtcInfo> TtcInfos { get; }
        internal EntryTable<uint> CountTable { get; }
        internal AddressTable<ulong> FunctionTable { get; }
        internal TranslatorStubs Stubs { get; }
        internal IMemoryManager Memory { get; }

        private volatile int _threadCount;

        public static ulong StaticCodeStart { internal get; set; }
        public static ulong StaticCodeSize  { internal get; set; }

        // FIXME: Remove this once the init logic of the emulator will be redone.
        public static readonly ManualResetEvent IsReadyForTranslation = new(false);

        public Translator(IJitMemoryAllocator allocator, IMemoryManager memory, bool for64Bits)
        {
            _allocator = allocator;
            Memory = memory;

            _oldFuncs = new ConcurrentDictionary<ulong, TranslatedFunction>();

            _backgroundSet = new ConcurrentDictionary<ulong, object>();
            _backgroundStack = new ConcurrentStack<RejitRequest>();
            _backgroundTranslatorEvent = new AutoResetEvent(false);
            _backgroundTranslatorLock = new ReaderWriterLock();

            JitCache.Initialize(allocator);

            Functions = new ConcurrentDictionary<ulong, TranslatedFunction>();
            TtcInfos = new ConcurrentDictionary<Hash128, TtcInfo>();
            CountTable = new EntryTable<uint>();
            FunctionTable = new AddressTable<ulong>(for64Bits ? Levels64Bit : Levels32Bit);
            Stubs = new TranslatorStubs(this);

            FunctionTable.Fill = (ulong)Stubs.SlowDispatchStub;

            if (memory.Type.IsHostMapped())
            {
                NativeSignalHandler.InitializeSignalHandler();
            }
        }

        private void TranslateStackedSubs()
        {
            while (_threadCount != 0)
            {
                _backgroundTranslatorLock.AcquireReaderLock(Timeout.Infinite);

                if (_backgroundStack.TryPop(out RejitRequest request) &&
                    _backgroundSet.TryRemove(request.Address, out _))
                {
                    TranslatedFunction func = Translate(request.Address, request.Mode, highCq: true);

                    Functions.AddOrUpdate(request.Address, func, (key, oldFunc) =>
                    {
                        _oldFuncs.TryAdd(key, oldFunc);

                        return func;
                    });

                    if (PtcProfiler.Enabled)
                    {
                        PtcProfiler.UpdateEntry(request.Address, request.Mode, highCq: true);
                    }

                    RegisterFunction(request.Address, func);

                    _backgroundTranslatorLock.ReleaseReaderLock();
                }
                else
                {
                    _backgroundTranslatorLock.ReleaseReaderLock();
                    _backgroundTranslatorEvent.WaitOne();
                }
            }

             // Wake up any other background translator threads, to encourage them to exit.
            _backgroundTranslatorEvent.Set();
        }

        public void Execute(State.ExecutionContext context, ulong address)
        {
            if (Interlocked.Increment(ref _threadCount) == 1)
            {
                IsReadyForTranslation.WaitOne();

                if (Ptc.State == PtcState.Enabled)
                {
                    Debug.Assert(Functions.Count == 0);
                    Ptc.LoadTranslations(this);
                    Ptc.MakeAndSaveTranslations(this);
                }

                PtcProfiler.Start();

                Ptc.Disable();

                // Simple heuristic, should be user configurable in future. (1 for 4 core/ht or less, 2 for 6 core + ht
                // etc). All threads are normal priority except from the last, which just fills as much of the last core
                // as the os lets it with a low priority. If we only have one rejit thread, it should be normal priority
                // as highCq code is performance critical.
                //
                // TODO: Use physical cores rather than logical. This only really makes sense for processors with
                // hyperthreading. Requires OS specific code.
                int unboundedThreadCount = Math.Max(1, (Environment.ProcessorCount - 6) / 3);
                int threadCount          = Math.Min(4, unboundedThreadCount);

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

            NativeInterface.RegisterThread(context, Memory, this);

            if (Optimizations.UseUnmanagedDispatchLoop)
            {
                Stubs.DispatchLoop(context.NativeContextPtr, address);
            }
            else
            {
                do
                {
                    address = ExecuteSingle(context, address);
                }
                while (context.Running && address != 0);
            }

            NativeInterface.UnregisterThread();

            if (Interlocked.Decrement(ref _threadCount) == 0)
            {
                _backgroundTranslatorEvent.Set();

                ClearJitCache();

                DisposePools();

                Stubs.Dispose();
                FunctionTable.Dispose();
                CountTable.Dispose();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            }
        }

        private ulong ExecuteSingle(State.ExecutionContext context, ulong address)
        {
            TranslatedFunction func = GetOrTranslate(address, context.ExecutionMode);

            Statistics.StartTimer();

            ulong nextAddr = func.Execute(context);

            Statistics.StopTimer(address);

            return nextAddr;
        }

        internal TranslatedFunction GetOrTranslate(ulong address, ExecutionMode mode)
        {
            if (!Functions.TryGetValue(address, out TranslatedFunction func))
            {
                func = Translate(address, mode, highCq: false);

                TranslatedFunction oldFunc = Functions.GetOrAdd(address, func);

                if (oldFunc != func)
                {
                    JitCache.Unmap(func.FuncPtr);
                    func = oldFunc;
                }

                if (PtcProfiler.Enabled)
                {
                    PtcProfiler.AddEntry(address, mode, highCq: false);
                }

                RegisterFunction(address, func);
            }

            return func;
        }

        internal void RegisterFunction(ulong guestAddress, TranslatedFunction func)
        {
            if (FunctionTable.IsValid(guestAddress) && (Optimizations.AllowLcqInFunctionTable || func.HighCq))
            {
                Volatile.Write(ref FunctionTable.GetValue(guestAddress), (ulong)func.FuncPtr);
            }
        }

        internal TranslatedFunction Translate(ulong address, ExecutionMode mode, bool highCq)
        {
            Logger.StartPass(PassName.Decoding);

            Block[] blocks = Decoder.Decode(Memory, address, mode, highCq, singleBlock: false);

            Logger.EndPass(PassName.Decoding);

            Range funcRange = GetFuncRange(blocks);

            ulong funcSize = funcRange.End - funcRange.Start;

            TtcInfo ttcInfo = null;

            if (Optimizations.EnableTtc &&
                mode == ExecutionMode.Aarch64 && // TODO: Aarch32.
                Ttc.TryFastTranslateDyn(
                this,
                address,
                funcSize,
                highCq,
                ref ttcInfo,
                out TranslatedFunction translatedFuncDyn))
            {
                return translatedFuncDyn;
            }

            ArmEmitterContext context = new(
                Memory,
                CountTable,
                FunctionTable,
                Stubs,
                address,
                highCq,
                mode: Aarch32Mode.User,
                hasTtc: ttcInfo != null);

            PreparePool(highCq ? 1 : 0);

            Logger.StartPass(PassName.Translation);

            EmitSynchronization(context);

            if (blocks[0].Address != address)
            {
                context.Branch(context.GetLabel(address));
            }

            ControlFlowGraph cfg = EmitAndGetCFG(context, blocks, out Counter<uint> counter);

            Logger.EndPass(PassName.Translation);

            Logger.StartPass(PassName.RegisterUsage);

            RegisterUsage.RunPass(cfg, mode);

            Logger.EndPass(PassName.RegisterUsage);

            OperandType[] argTypes = new OperandType[] { OperandType.I64 };

            CompilerOptions options = highCq ? CompilerOptions.HighCq : CompilerOptions.None;

            GuestFunction func;

            if (!context.HasPtc)
            {
                func = Compiler.Compile<GuestFunction>(cfg, argTypes, OperandType.I64, options, ttcInfo);

                ResetPool(highCq ? 1 : 0);
            }
            else
            {
                using PtcInfo ptcInfo = new PtcInfo();

                func = Compiler.Compile<GuestFunction>(cfg, argTypes, OperandType.I64, options, ptcInfo);

                ResetPool(highCq ? 1 : 0);

                Hash128 hash = ComputeHash(address, funcSize);

                Ptc.WriteInfoCodeRelocUnwindInfo(address, funcSize, hash, highCq, ptcInfo);
            }

            TranslatedFunction translatedFunc = new(func, counter, funcSize, highCq);

            if (context.HasTtc)
            {
                ttcInfo.TranslatedFunc = translatedFunc;
            }

            return translatedFunc;
        }

        internal static void PreparePool(int groupId = 0)
        {
            PrepareOperandPool(groupId);
            PrepareOperationPool(groupId);
        }

        internal static void ResetPool(int groupId = 0)
        {
            ResetOperationPool(groupId);
            ResetOperandPool(groupId);
        }

        internal static void DisposePools()
        {
            DisposeOperandPools();
            DisposeOperationPools();
            DisposeBitMapPools();
        }

        private struct Range
        {
            public ulong Start { get; }
            public ulong End { get; }

            public Range(ulong start, ulong end)
            {
                Start = start;
                End = end;
            }
        }

        private static Range GetFuncRange(Block[] blocks)
        {
            ulong rangeStart = ulong.MaxValue;
            ulong rangeEnd = 0;

            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                Block block = blocks[blkIndex];

                if (!block.Exit)
                {
                    if (rangeStart > block.Address)
                    {
                        rangeStart = block.Address;
                    }

                    if (rangeEnd < block.EndAddress)
                    {
                        rangeEnd = block.EndAddress;
                    }
                }
            }

            return new Range(rangeStart, rangeEnd);
        }

        private static ControlFlowGraph EmitAndGetCFG(ArmEmitterContext context, Block[] blocks, out Counter<uint> counter)
        {
            counter = null;

            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                Block block = blocks[blkIndex];

                if (block.Address == context.EntryAddress && !context.HighCq)
                {
                    EmitRejitCheck(context, out counter);
                }

                context.CurrBlock = block;

                context.MarkLabel(context.GetLabel(block.Address));

                if (block.Exit)
                {
                    // Left option here as it may be useful if we need to return to managed rather than tail call in
                    // future. (eg. for debug)
                    bool useReturns = false;

                    Operand address = useReturns || !context.HasTtc
                        ? Const(block.Address)
                        : Const(block.Address, new Symbol(SymbolType.DynFunc, context.GetOffset(block.Address)));

                    InstEmitFlowHelper.EmitVirtualJump(context, address, isReturn: useReturns);
                }
                else
                {
                    for (int opcIndex = 0; opcIndex < block.OpCodes.Count; opcIndex++)
                    {
                        OpCode opCode = block.OpCodes[opcIndex];

                        context.CurrOp = opCode;

                        bool isLastOp = opcIndex == block.OpCodes.Count - 1;

                        if (isLastOp && block.Branch != null && !block.Branch.Exit && block.Branch.Address <= block.Address)
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
                        }
                    }
                }
            }

            return context.GetControlFlowGraph();
        }

        private static void EmitRejitCheck(ArmEmitterContext context, out Counter<uint> counter)
        {
            counter = new Counter<uint>(context.CountTable);

            Operand lblEnd = Label();

            Operand address = !context.HasPtc
                ? Const(ref counter.Value)
                : Const(ref counter.Value, Ptc.CountTableSymbol);

            Operand curCount = context.Load(OperandType.I32, address);
            Operand count = context.Add(curCount, Const(1));
            context.Store(address, count);
            context.BranchIf(lblEnd, curCount, Const(MinsCallForRejit), Comparison.NotEqual, BasicBlockFrequency.Cold);

            Operand callArg = !context.HasTtc
                ? Const(context.EntryAddress)
                : Const(context.EntryAddress, new Symbol(SymbolType.DynFunc, context.GetOffset(context.EntryAddress)));

            context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.EnqueueForRejit)), callArg);

            context.MarkLabel(lblEnd);
        }

        internal static void EmitSynchronization(EmitterContext context)
        {
            long countOffs = NativeContext.GetCounterOffset();

            Operand lblNonZero = Label();
            Operand lblExit = Label();

            Operand countAddr = context.Add(context.LoadArgument(OperandType.I64, 0), Const(countOffs));
            Operand count = context.Load(OperandType.I32, countAddr);
            context.BranchIfTrue(lblNonZero, count, BasicBlockFrequency.Cold);

            Operand running = context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.CheckSynchronization)));
            context.BranchIfTrue(lblExit, running, BasicBlockFrequency.Cold);

            context.Return(Const(0L));

            context.MarkLabel(lblNonZero);
            count = context.Subtract(count, Const(1));
            context.Store(countAddr, count);

            context.MarkLabel(lblExit);
        }

        public void InvalidateJitCacheRegion(ulong address, ulong size)
        {
            Debug.Assert(!OverlapsWith(address, size, StaticCodeStart, StaticCodeSize));

            ClearJitCacheDyn(address, size);
        }

        internal void EnqueueForRejit(ulong guestAddress, ExecutionMode mode)
        {
            if (_backgroundSet.TryAdd(guestAddress, null))
            {
                _backgroundStack.Push(new RejitRequest(guestAddress, mode));
                _backgroundTranslatorEvent.Set();
            }
        }

        private void ClearJitCache()
        {
            ClearRejitQueue(allowRequeue: false);

            foreach (var ttcInfo in TtcInfos.Values)
            {
                JitCache.Unmap(ttcInfo.TranslatedFunc.FuncPtr);

                ttcInfo.TranslatedFunc.CallCounter?.Dispose();

                Functions.TryRemove(ttcInfo.LastGuestAddress, out _);
                _oldFuncs.TryRemove(ttcInfo.LastGuestAddress, out _);

                ttcInfo.Dispose();
            }

            TtcInfos.Clear();

            foreach (var func in Functions.Values)
            {
                JitCache.Unmap(func.FuncPtr);

                func.CallCounter?.Dispose();
            }

            Functions.Clear();

            foreach (var oldFunc in _oldFuncs.Values)
            {
                JitCache.Unmap(oldFunc.FuncPtr);

                oldFunc.CallCounter?.Dispose();
            }

            _oldFuncs.Clear();
        }

        private void ClearJitCacheDyn(ulong address, ulong size)
        {
            ClearRejitQueue(allowRequeue: true);

            ThreadPool.QueueUserWorkItem((state) =>
            {
                var (address, size) = ((ulong, ulong))state;

                foreach (var ttcInfo in TtcInfos.Values)
                {
                    lock (ttcInfo)
                    {
                        if (OverlapsWith(ttcInfo.LastGuestAddress, ttcInfo.GuestSize, address, size))
                        {
                            Functions.TryRemove(ttcInfo.LastGuestAddress, out _);
                            _oldFuncs.TryRemove(ttcInfo.LastGuestAddress, out _);

                            ttcInfo.IsBusy = false;
                        }
                    }
                }
            }, (address, size));
        }

        // Ensures that functions queued for rejit are not retranslated, allowing them to be re-queued for rejit or not.
        private void ClearRejitQueue(bool allowRequeue)
        {
            _backgroundTranslatorLock.AcquireWriterLock(Timeout.Infinite);

            if (allowRequeue)
            {
                while (_backgroundStack.TryPop(out var request))
                {
                    if (Functions.TryGetValue(request.Address, out var func) && func.CallCounter != null)
                    {
                        Volatile.Write(ref func.CallCounter.Value, MinsCallForRejit);
                    }

                    _backgroundSet.TryRemove(request.Address, out _);
                }
            }
            else
            {
                _backgroundStack.Clear();
            }

            _backgroundTranslatorLock.ReleaseWriterLock();
        }

        internal static bool OverlapsWith(ulong funcAddress, ulong funcSize, ulong address, ulong size)
        {
            return funcAddress < address + size && address < funcAddress + funcSize;
        }

        internal Hash128 ComputeHash(ulong address, ulong guestSize)
        {
            return XXHash128.ComputeHash(Memory.GetSpan(address, checked((int)(guestSize))));
        }
    }
}
