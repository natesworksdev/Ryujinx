using ChocolArm64.Decoders;
using ChocolArm64.Events;
using ChocolArm64.Memory;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Threading;

namespace ChocolArm64
{
    public class Translator
    {
        private MemoryManager _memory;

        private CpuThreadState _dummyThreadState;

        private TranslatorCache _cache;
        private TranslatorQueue _queue;

        private Thread _backgroundTranslator;

        public event EventHandler<CpuTraceEventArgs> CpuTrace;

        public bool EnableCpuTrace { get; set; }

        private volatile int _threadCount;

        public Translator(MemoryManager memory)
        {
            _memory = memory;

            _dummyThreadState = new CpuThreadState();

            _dummyThreadState.Running = false;

            _cache = new TranslatorCache();
            _queue = new TranslatorQueue();
        }

        internal void ExecuteSubroutine(CpuThread thread, long position)
        {
            if (Interlocked.Increment(ref _threadCount) == 1)
            {
                _backgroundTranslator = new Thread(TranslateQueuedSubs);
                _backgroundTranslator.Start();
            }

            ExecuteSubroutine(thread.ThreadState, thread.Memory, position);

            if (Interlocked.Decrement(ref _threadCount) == 0)
            {
                _queue.ForceSignal();
            }
        }

        private void ExecuteSubroutine(CpuThreadState state, MemoryManager memory, long position)
        {
            state.CurrentTranslator = this;

            do
            {
                if (EnableCpuTrace)
                {
                    CpuTrace?.Invoke(this, new CpuTraceEventArgs(position));
                }

                TranslatedSub subroutine = GetOrTranslateSubroutine(state, memory, position);

                position = subroutine.Execute(state, memory);
            }
            while (position != 0 && state.Running);

            state.CurrentTranslator = null;
        }

        internal TranslatedSub GetOrTranslateVirtualSubroutine(CpuThreadState state, MemoryManager memory, long position)
        {
            if (!_cache.TryGetSubroutine(position, out TranslatedSub subroutine))
            {
                _queue.Enqueue(new TranslatorQueueItem(position, state.GetExecutionMode(), TranslationTier.Tier2));

                subroutine = TranslateLowCq(memory, position, state.GetExecutionMode());
            }

            return subroutine;
        }

        internal TranslatedSub GetOrTranslateSubroutine(CpuThreadState state, MemoryManager memory, long position)
        {
            if (!_cache.TryGetSubroutine(position, out TranslatedSub subroutine))
            {
                subroutine = TranslateLowCq(memory, position, state.GetExecutionMode());
            }

            return subroutine;
        }

        private void TranslateQueuedSubs()
        {
            while (_threadCount != 0)
            {
                if (_queue.TryDequeue(out TranslatorQueueItem item))
                {
                    bool isCached = _cache.TryGetSubroutine(item.Position, out TranslatedSub sub);

                    if (isCached && item.Tier <= sub.Tier)
                    {
                        continue;
                    }

                    if (item.Tier == TranslationTier.Tier0)
                    {
                        TranslateLowCq(_memory, item.Position, item.Mode);
                    }
                    else
                    {
                        TranslateHighCq(_memory, item.Position, item.Mode);
                    }
                }
                else
                {
                    _queue.WaitForItems();
                }
            }
        }

        private TranslatedSub TranslateLowCq(MemoryManager memory, long position, ExecutionMode mode)
        {
            Block block = Decoder.DecodeBasicBlock(memory, position, mode);

            ILEmitterCtx context = new ILEmitterCtx(_cache, _queue, block);

            string subName = GetSubroutineName(position);

            ILMethodBuilder ilMthdBuilder = new ILMethodBuilder(context.GetILBlocks(), subName);

            TranslatedSub subroutine = ilMthdBuilder.GetSubroutine(TranslationTier.Tier0);

            TranslatedSub cacheSub = _cache.GetOrAdd(position, subroutine, block.OpCodes.Count);

            return cacheSub;
        }

        private void TranslateHighCq(MemoryManager memory, long position, ExecutionMode mode)
        {
            Block graph = Decoder.DecodeSubroutine(memory, position, mode);

            ILEmitterCtx context = new ILEmitterCtx(_cache, _queue, graph);

            ILBlock[] ilBlocks = context.GetILBlocks();

            string subName = GetSubroutineName(position);

            ILMethodBuilder ilMthdBuilder = new ILMethodBuilder(ilBlocks, subName);

            TranslatedSub subroutine = ilMthdBuilder.GetSubroutine(TranslationTier.Tier1);

            int ilOpCount = 0;

            foreach (ILBlock ilBlock in ilBlocks)
            {
                ilOpCount += ilBlock.Count;
            }

            _cache.AddOrUpdate(position, subroutine, ilOpCount);

            ForceAheadOfTimeCompilation(subroutine);
        }

        private string GetSubroutineName(long position)
        {
            return $"Sub{position:x16}";
        }

        private void ForceAheadOfTimeCompilation(TranslatedSub subroutine)
        {
            subroutine.Execute(_dummyThreadState, null);
        }
    }
}