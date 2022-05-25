using ARMeilleure.Memory;
using ARMeilleure.State;

namespace Ryujinx.Cpu.Jit
{
    class JitExecutionContext : IExecutionContext
    {
        private readonly ExecutionContext _impl;
        internal ExecutionContext Impl => _impl;

        public ulong Pc => _impl.Pc;

        public long TpidrEl0
        {
            get => _impl.TpidrEl0;
            set => _impl.TpidrEl0 = value;
        }

        public long TpidrroEl0
        {
            get => _impl.TpidrroEl0;
            set => _impl.TpidrroEl0 = value;
        }

        public uint Pstate
        {
            get => _impl.Pstate;
            set => _impl.Pstate = value;
        }

        public uint Fpcr
        {
            get => (uint)_impl.Fpcr;
            set => _impl.Fpcr = (FPCR)value;
        }

        public uint Fpsr
        {
            get => (uint)_impl.Fpsr;
            set => _impl.Fpsr = (FPSR)value;
        }

        public bool IsAarch32
        {
            get => _impl.IsAarch32;
            set => _impl.IsAarch32 = value;
        }

        public bool Running => _impl.Running;

        private readonly ExceptionCallbacks _exceptionCallbacks;

        public JitExecutionContext(IJitMemoryAllocator allocator, ICounter counter, ExceptionCallbacks exceptionCallbacks)
        {
            _impl = new ExecutionContext(
                allocator,
                counter,
                InterruptHandler,
                BreakHandler,
                SupervisorCallHandler,
                UndefinedHandler);

            _exceptionCallbacks = exceptionCallbacks;
        }

        public ulong GetX(int index) => _impl.GetX(index);
        public void SetX(int index, ulong value) => _impl.SetX(index, value);

        public V128 GetV(int index) => _impl.GetV(index);
        public void SetV(int index, V128 value) => _impl.SetV(index, value);

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

        public void RequestInterrupt()
        {
            _impl.RequestInterrupt();
        }

        public void StopRunning()
        {
            _impl.StopRunning();
        }

        public void Dispose()
        {
            _impl.Dispose();
        }
    }
}