using ARMeilleure.Memory;
using ARMeilleure.State;
using System;

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

        public event EventHandler<EventArgs> Interrupt
        {
            add => _impl.Interrupt += value;
            remove => _impl.Interrupt -= value;
        }

        public event EventHandler<InstExceptionEventArgs> Break
        {
            add => _impl.Break += value;
            remove => _impl.Break -= value;
        }

        public event EventHandler<InstExceptionEventArgs> SupervisorCall
        {
            add => _impl.SupervisorCall += value;
            remove => _impl.SupervisorCall -= value;
        }

        public event EventHandler<InstUndefinedEventArgs> Undefined
        {
            add => _impl.Undefined += value;
            remove => _impl.Undefined -= value;
        }

        public JitExecutionContext(IJitMemoryAllocator allocator, ICounter counter)
        {
            _impl = new ExecutionContext(allocator, counter);
        }

        public ulong GetX(int index) => _impl.GetX(index);
        public void SetX(int index, ulong value) => _impl.SetX(index, value);

        public V128 GetV(int index) => _impl.GetV(index);
        public void SetV(int index, V128 value) => _impl.SetV(index, value);

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