using Ryujinx.HLE.HOS.Kernel.SupervisorCall;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Kernel
{
    static class KernelStatic
    {
        [ThreadStatic]
        private static KernelContext Context;

        public static IAddressSpaceManager AddressSpace => Context.Scheduler.GetCurrentProcess().CpuMemory;
        public static Syscall Syscall => Context.Syscall;

        public static SignalableEvent GetSignalableEvent(int writableHandle)
        {
            var wEvent = Context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KWritableEvent>(writableHandle);

            return new SignalableEvent(wEvent ?? throw new ArgumentException("Invalid handle."));
        }

        internal static void SetKernelContext(KernelContext context) => Context = context;
    }
}
