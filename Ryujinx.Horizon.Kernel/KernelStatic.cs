using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Svc;
using Ryujinx.Horizon.Kernel.Threading;
using Ryujinx.Memory;
using System;
using System.Threading;

namespace Ryujinx.Horizon.Kernel
{
    public static class KernelStatic
    {
        [ThreadStatic]
        private static KernelContextInternal Context;

        public static IAddressSpaceManager AddressSpace => Context.Scheduler.GetCurrentProcess().CpuMemory;
        public static Syscall Syscall => Context.Syscall;

        public static SignalableEvent GetSignalableEvent(int writableHandle)
        {
            var wEvent = Context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KWritableEvent>(writableHandle);

            return new SignalableEvent(wEvent ?? throw new ArgumentException("Invalid handle."));
        }

        public static IAddressSpaceManager GetAddressSpace(int processHandle)
        {
            return Context.Scheduler.GetCurrentProcess().HandleTable.GetKProcess(processHandle)?.CpuMemory;
        }

        public static ulong GetTlsAddress()
        {
            return Context.Scheduler.GetCurrentThread().TlsAddress;
        }

        public static string GetGuestStackTrace()
        {
            return Context.Scheduler.GetCurrentThread().GetGuestStackTrace();
        }

        public static void CallSvc(IThreadContext context, int id)
        {
            Context.SyscallHandler.CallSvc(context, id);
        }

        public static void InterruptServiceRoutine()
        {
            Context.Scheduler.ContextSwitch();
            Context.Scheduler.GetCurrentThread().HandlePostSyscall();
        }

        public static void TerminateAllProcesses(KernelContext context)
        {
            KernelContextInternal internalCtx = context.GetInternal();

            KProcess terminationProcess = new KProcess(internalCtx);
            KThread terminationThread = new KThread(internalCtx);

            terminationThread.Initialize(0, 0, 0, 3, 0, terminationProcess, ThreadType.Kernel, () =>
            {
                // Force all threads to exit.
                lock (internalCtx.Processes)
                {
                    foreach (KProcess process in internalCtx.Processes.Values)
                    {
                        process.Terminate();
                    }
                }

                // Exit ourself now!
                internalCtx.Scheduler.ExitThread(terminationThread);
                internalCtx.Scheduler.GetCurrentThread().Exit();
                internalCtx.Scheduler.RemoveThread(terminationThread);
            });

            terminationThread.Start();

            // Wait until the thread is actually started.
            while (terminationThread.HostThread.ThreadState == ThreadState.Unstarted)
            {
                Thread.Sleep(10);
            }

            // Wait until the termination thread is done terminating all the other threads.
            terminationThread.HostThread.Join();
        }

        public static void SetKernelContext(KernelContext context)
        {
            Context = context.GetInternal();
        }

        internal static void SetKernelContext(KernelContextInternal context)
        {
            Context = context;
        }
    }
}
