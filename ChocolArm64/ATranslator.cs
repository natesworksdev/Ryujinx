using ChocolArm64.Decoder;
using ChocolArm64.Events;
using ChocolArm64.Memory;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;

namespace ChocolArm64
{
    public class ATranslator
    {
        private ATranslatorCache _cache;

        public event EventHandler<ACpuTraceEventArgs> CpuTrace;

        public bool EnableCpuTrace { get; set; }

        public ATranslator()
        {
            _cache = new ATranslatorCache();
        }

        internal void ExecuteSubroutine(AThread thread, long position)
        {
            //TODO: Both the execute A32/A64 methods should be merged on the future,
            //when both ISAs are implemented with the interpreter and JIT.
            //As of now, A32 only has a interpreter and A64 a JIT.
            AThreadState state  = thread.ThreadState;
            AMemory      memory = thread.Memory;

            if (state.ExecutionMode == AExecutionMode.AArch32)
            {
                ExecuteSubroutineA32(state, memory);
            }
            else
            {
                ExecuteSubroutineA64(state, memory, position);
            }
        }

        private void ExecuteSubroutineA32(AThreadState state, AMemory memory)
        {
            do
            {
                AOpCode opCode = ADecoder.DecodeOpCode(state, memory, state.R15);

                opCode.Interpreter(state, memory, opCode);
            }
            while (state.R15 != 0 && state.Running);
        }

        private void ExecuteSubroutineA64(AThreadState state, AMemory memory, long position)
        {
            do
            {
                if (EnableCpuTrace)
                {
                    CpuTrace?.Invoke(this, new ACpuTraceEventArgs(position));
                }

                if (!_cache.TryGetSubroutine(position, out ATranslatedSub sub))
                {
                    sub = TranslateTier0(state, memory, position);
                }

                if (sub.ShouldReJit())
                {
                    TranslateTier1(state, memory, position);
                }

                position = sub.Execute(state, memory);
            }
            while (position != 0 && state.Running);
        }

        internal bool HasCachedSub(long position)
        {
            return _cache.HasSubroutine(position);
        }

        private ATranslatedSub TranslateTier0(AThreadState state, AMemory memory, long position)
        {
            ABlock block = ADecoder.DecodeBasicBlock(state, memory, position);

            ABlock[] graph = new ABlock[] { block };

            string subName = GetSubroutineName(position);

            AilEmitterCtx context = new AilEmitterCtx(_cache, graph, block, subName);

            do
            {
                context.EmitOpCode();
            }
            while (context.AdvanceOpCode());

            ATranslatedSub subroutine = context.GetSubroutine();

            subroutine.SetType(ATranslatedSubType.SubTier0);

            _cache.AddOrUpdate(position, subroutine, block.OpCodes.Count);

            AOpCode lastOp = block.GetLastOp();

            return subroutine;
        }

        private void TranslateTier1(AThreadState state, AMemory memory, long position)
        {
            (ABlock[] graph, ABlock root) = ADecoder.DecodeSubroutine(_cache, state, memory, position);

            string subName = GetSubroutineName(position);

            AilEmitterCtx context = new AilEmitterCtx(_cache, graph, root, subName);

            if (context.CurrBlock.Position != position)
            {
                context.Emit(OpCodes.Br, context.GetLabel(position));
            }

            do
            {
                context.EmitOpCode();
            }
            while (context.AdvanceOpCode());

            //Mark all methods that calls this method for ReJiting,
            //since we can now call it directly which is faster.
            if (_cache.TryGetSubroutine(position, out ATranslatedSub oldSub))
            {
                foreach (long callerPos in oldSub.GetCallerPositions())
                {
                    if (_cache.TryGetSubroutine(position, out ATranslatedSub callerSub))
                    {
                        callerSub.MarkForReJit();
                    }
                }
            }

            ATranslatedSub subroutine = context.GetSubroutine();

            subroutine.SetType(ATranslatedSubType.SubTier1);

            _cache.AddOrUpdate(position, subroutine, GetGraphInstCount(graph));
        }

        private string GetSubroutineName(long position)
        {
            return $"Sub{position:x16}";
        }

        private int GetGraphInstCount(ABlock[] graph)
        {
            int size = 0;

            foreach (ABlock block in graph)
            {
                size += block.OpCodes.Count;
            }

            return size;
        }
    }
}