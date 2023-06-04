using ARMeilleure.Memory;
using ARMeilleure.State;

namespace Ryujinx.Cpu.Jit
{
    class JitExecutionContext : IExecutionContext
    {
        internal ExecutionContext Impl { get; }

        /// <inheritdoc/>
        public ulong Pc => Impl.Pc;

        /// <inheritdoc/>
        public long TpidrEl0
        {
            get => Impl.TpidrEl0;
            set => Impl.TpidrEl0 = value;
        }

        /// <inheritdoc/>
        public long TpidrroEl0
        {
            get => Impl.TpidrroEl0;
            set => Impl.TpidrroEl0 = value;
        }

        /// <inheritdoc/>
        public uint Pstate
        {
            get => Impl.Pstate;
            set => Impl.Pstate = value;
        }

        /// <inheritdoc/>
        public uint Fpcr
        {
            get => (uint)Impl.Fpcr;
            set => Impl.Fpcr = (FPCR)value;
        }

        /// <inheritdoc/>
        public uint Fpsr
        {
            get => (uint)Impl.Fpsr;
            set => Impl.Fpsr = (FPSR)value;
        }

        /// <inheritdoc/>
        public bool IsAarch32
        {
            get => Impl.IsAarch32;
            set => Impl.IsAarch32 = value;
        }

        /// <inheritdoc/>
        public bool Running => Impl.Running;

        private readonly ExceptionCallbacks _exceptionCallbacks;

        public JitExecutionContext(IJitMemoryAllocator allocator, ICounter counter, ExceptionCallbacks exceptionCallbacks)
        {
            Impl = new ExecutionContext(
                allocator,
                counter,
                InterruptHandler,
                BreakHandler,
                SupervisorCallHandler,
                UndefinedHandler);

            _exceptionCallbacks = exceptionCallbacks;
        }

        /// <inheritdoc/>
        public ulong GetX(int index) => Impl.GetX(index);

        /// <inheritdoc/>
        public void SetX(int index, ulong value) => Impl.SetX(index, value);

        /// <inheritdoc/>
        public V128 GetV(int index) => Impl.GetV(index);

        /// <inheritdoc/>
        public void SetV(int index, V128 value) => Impl.SetV(index, value);

        private void InterruptHandler(ExecutionContext context)
        {
            _exceptionCallbacks.InterruptCallback?.Invoke(this);
        }

        private void BreakHandler(ExecutionContext context, ulong address, int imm)
        {
            _exceptionCallbacks.BreakCallback?.Invoke(this, address, imm);
        }

        private void SupervisorCallHandler(ExecutionContext context, ulong address, int imm)
        {
            _exceptionCallbacks.SupervisorCallback?.Invoke(this, address, imm);
        }

        private void UndefinedHandler(ExecutionContext context, ulong address, int opCode)
        {
            _exceptionCallbacks.UndefinedCallback?.Invoke(this, address, opCode);
        }

        /// <inheritdoc/>
        public void RequestInterrupt()
        {
            Impl.RequestInterrupt();
        }

        /// <inheritdoc/>
        public void StopRunning()
        {
            Impl.StopRunning();
        }

        public void Dispose()
        {
            Impl.Dispose();
        }
    }
}