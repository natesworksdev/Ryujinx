using ARMeilleure.State;
using Ryujinx.Cpu;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    class ProcessExecutionContext : IExecutionContext
    {
        public ulong Pc => 0UL;

        public ulong CntfrqEl0 { get => 0; set { } }
        public ulong CntpctEl0 => 0UL;

        public long TpidrEl0 { get => 0; set { } }
        public long TpidrroEl0 { get => 0; set { } }

        public uint Pstate { get => 0; set { } }

        public uint Fpcr { get => 0; set { } }
        public uint Fpsr { get => 0; set { } }

        public bool IsAarch32 { get => false; set { } }

        public bool Running { get; private set; } = true;

        public event EventHandler<EventArgs> Interrupt;
        public event EventHandler<InstExceptionEventArgs> Break;
        public event EventHandler<InstExceptionEventArgs> SupervisorCall;
        public event EventHandler<InstUndefinedEventArgs> Undefined;

        public ulong GetX(int index) => 0UL;
        public void SetX(int index, ulong value) { }

        public V128 GetV(int index) => default;
        public void SetV(int index, V128 value) { }

        public void RequestInterrupt()
        {
        }

        public void StopRunning()
        {
            Running = false;
        }

        public void OnInterrupt()
        {
            Interrupt?.Invoke(this, EventArgs.Empty);
        }

        public void OnBreak(ulong address, int imm)
        {
            Break?.Invoke(this, new InstExceptionEventArgs(address, imm));
        }

        public void OnSupervisorCall(ulong address, int imm)
        {
            SupervisorCall?.Invoke(this, new InstExceptionEventArgs(address, imm));
        }

        public void OnUndefined(ulong address, int opCode)
        {
            Undefined?.Invoke(this, new InstUndefinedEventArgs(address, opCode));
        }

        public void Dispose()
        {
        }
    }
}