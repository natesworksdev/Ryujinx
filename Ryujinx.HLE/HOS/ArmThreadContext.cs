using ARMeilleure.State;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.Exceptions;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Svc;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Ryujinx.HLE.HOS
{
    class ArmThreadContext : IThreadContext
    {
        public ExecutionContext Internal { get; }

        public ulong Frequency => Internal.CntfrqEl0;
        public ulong Counter => Internal.CntpctEl0;

        public ulong TlsAddress => (ulong)Internal.Tpidr;

        public int Fpcr => (int)Internal.Fpcr;
        public int Fpsr => (int)Internal.Fpsr;
        public int Cpsr => GetPsr(Internal);

        private static int GetPsr(ExecutionContext context)
        {
            return (context.GetPstateFlag(PState.NFlag) ? (1 << (int)PState.NFlag) : 0) |
                   (context.GetPstateFlag(PState.ZFlag) ? (1 << (int)PState.ZFlag) : 0) |
                   (context.GetPstateFlag(PState.CFlag) ? (1 << (int)PState.CFlag) : 0) |
                   (context.GetPstateFlag(PState.VFlag) ? (1 << (int)PState.VFlag) : 0);
        }

        public bool Is32Bit => Internal.IsAarch32;

        public ArmThreadContext(ulong frequency, ulong tlsAddress, bool is32Bit)
        {
            Internal = CpuContext.CreateExecutionContext();
            Internal.CntfrqEl0 = frequency;
            Internal.Tpidr = (long)tlsAddress;
            Internal.IsAarch32 = is32Bit;
            SubscribeEventHandlers(Internal);
        }

        public void SubscribeEventHandlers(ExecutionContext context)
        {
            context.Interrupt += InterruptHandler;
            context.SupervisorCall += SvcHandler;
            context.Undefined += UndefinedInstructionHandler;
        }

        private void InterruptHandler(object sender, EventArgs e)
        {
            KernelStatic.InterruptServiceRoutine();
        }

        private void SvcHandler(object sender, InstExceptionEventArgs e)
        {
            KernelStatic.CallSvc(this, e.Id);
        }

        private void UndefinedInstructionHandler(object sender, InstUndefinedEventArgs e)
        {
            Logger.Info?.Print(LogClass.Cpu, $"Guest stack trace:\n{KernelStatic.GetGuestStackTrace()}\n");

            throw new UndefinedInstructionException(e.Address, e.OpCode);
        }

        public ulong GetX(int index)
        {
            return Internal.GetX(index);
        }

        public void SetX(int index, ulong value)
        {
            Internal.SetX(index, value);
        }

        public Vector128<byte> GetV(int index)
        {
            var v = Internal.GetV(index);

            return Unsafe.As<V128, Vector128<byte>>(ref v);
        }

        public void SetV(int index, Vector128<byte> value)
        {
            Internal.SetV(index, Unsafe.As<Vector128<byte>, V128>(ref value));
        }

        public void RequestInterrupt()
        {
            Internal.RequestInterrupt();
        }

        public void Stop()
        {
            Internal.StopRunning();
        }

        public void Dispose()
        {
            Internal.Dispose();
        }
    }
}
