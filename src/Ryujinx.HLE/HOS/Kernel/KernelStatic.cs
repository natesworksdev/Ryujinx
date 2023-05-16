using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel
{
    static class KernelStatic
    {
        private static AsyncLocal<KernelContext> _context = new AsyncLocal<KernelContext>();
        private static KernelContext Context => _context.Value;
        private static AsyncLocal<KThread> _currentThread = new AsyncLocal<KThread>();
        private static KThread CurrentThread => _currentThread.Value;

        public static Result StartInitialProcess(
            KernelContext context,
            ProcessCreationInfo creationInfo,
            ReadOnlySpan<uint> capabilities,
            int mainThreadPriority,
            KThread.ThreadMainFn customThreadStart)
        {
            KProcess process = new KProcess(context);

            Result result = process.Initialize(
                creationInfo,
                capabilities,
                context.ResourceLimit,
                MemoryRegion.Service,
                null,
                customThreadStart);

            if (result != Result.Success)
            {
                return result;
            }

            process.DefaultCpuCore = 3;

            context.Processes.TryAdd(process.Pid, process);

            return process.Start(mainThreadPriority, 0x1000UL);
        }

        internal static void SetKernelContext(KernelContext context, KThread thread)
        {
            _context.Value = context;
            _currentThread.Value = thread;
        }

        internal static KThread GetCurrentThread()
        {
            return CurrentThread;
        }

        internal static KProcess GetCurrentProcess()
        {
            return GetCurrentThread().Owner;
        }

        internal static KProcess GetProcessByPid(ulong pid)
        {
            if (Context.Processes.TryGetValue(pid, out KProcess process))
            {
                return process;
            }

            return null;
        }
    }
}