using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Memory;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using System;
using System.Threading;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public void ExitProcess()
        {
            _context.Scheduler.GetCurrentProcess().TerminateCurrentProcess();

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);
        }

        public Result GetProcessId(int handle, out long pid)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess process = currentProcess.HandleTable.GetKProcess(handle);

            if (process == null)
            {
                KThread thread = currentProcess.HandleTable.GetKThread(handle);

                if (thread != null)
                {
                    process = thread.Owner;
                }

                // TODO: KDebugEvent.
            }

            pid = process?.Pid ?? 0;

            return CheckResult(process != null ? Result.Success : KernelResult.InvalidHandle);
        }

        public Result GetProcessList(ulong address, int maxCount, out int count)
        {
            count = 0;

            if ((maxCount >> 28) != 0)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            if (maxCount != 0)
            {
                KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                ulong copySize = (ulong)maxCount * 8;

                if (address + copySize <= address)
                {
                    return CheckResult(KernelResult.InvalidMemState);
                }

                if (currentProcess.MemoryManager.OutsideAddrSpace(address, copySize))
                {
                    return CheckResult(KernelResult.InvalidMemState);
                }
            }

            int copyCount = 0;

            lock (_context.Processes)
            {
                foreach (KProcess process in _context.Processes.Values)
                {
                    if (copyCount < maxCount)
                    {
                        if (!KernelTransfer.KernelToUserInt64(_context, address + (ulong)copyCount * 8, process.Pid))
                        {
                            return CheckResult(KernelResult.UserCopyFailed);
                        }
                    }

                    copyCount++;
                }
            }

            count = copyCount;

            return CheckResult(Result.Success);
        }

        public Result CreateProcess(
            ProcessCreationInfo info,
            ReadOnlySpan<int> capabilities,
            out int handle,
            IProcessContextFactory contextFactory,
            ThreadStart customThreadStart = null)
        {
            handle = 0;

            if ((info.Flags & ~ProcessCreationFlags.All) != 0)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            // TODO: Address space check.

            if ((info.Flags & ProcessCreationFlags.PoolPartitionMask) > ProcessCreationFlags.PoolPartitionSystemNonSecure)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            if ((info.CodeAddress & 0x1fffff) != 0)
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (info.CodePagesCount < 0 || info.SystemResourcePagesCount < 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (info.Flags.HasFlag(ProcessCreationFlags.OptimizeMemoryAllocation) &&
                !info.Flags.HasFlag(ProcessCreationFlags.IsApplication))
            {
                return CheckResult(KernelResult.InvalidThread);
            }

            KHandleTable handleTable = _context.Scheduler.GetCurrentProcess().HandleTable;

            KProcess process = new KProcess(_context);

            using var _ = new OnScopeExit(process.DecrementReferenceCount);

            KResourceLimit resourceLimit;

            if (info.ResourceLimitHandle != 0)
            {
                resourceLimit = handleTable.GetObject<KResourceLimit>(info.ResourceLimitHandle);

                if (resourceLimit == null)
                {
                    return CheckResult(KernelResult.InvalidHandle);
                }
            }
            else
            {
                resourceLimit = _context.ResourceLimit;
            }

            KMemoryRegion memRegion = (info.Flags & ProcessCreationFlags.PoolPartitionMask) switch
            {
                ProcessCreationFlags.PoolPartitionApplication => KMemoryRegion.Application,
                ProcessCreationFlags.PoolPartitionApplet => KMemoryRegion.Applet,
                ProcessCreationFlags.PoolPartitionSystem => KMemoryRegion.Service,
                ProcessCreationFlags.PoolPartitionSystemNonSecure => KMemoryRegion.NvServices,
                _ => KMemoryRegion.NvServices
            };

            Result result = process.Initialize(
                info,
                capabilities,
                resourceLimit,
                memRegion,
                contextFactory,
                customThreadStart);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            _context.Processes.TryAdd(process.Pid, process);

            return CheckResult(handleTable.GenerateHandle(process, out handle));
        }

        public Result StartProcess(int handle, int priority, int cpuCore, ulong mainThreadStackSize)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KProcess>(handle);

            if (process == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if ((uint)cpuCore >= KScheduler.CpuCoresCount || !process.IsCpuCoreAllowed(cpuCore))
            {
                return CheckResult(KernelResult.InvalidCpuCore);
            }

            if ((uint)priority >= KScheduler.PrioritiesCount || !process.IsPriorityAllowed(priority))
            {
                return CheckResult(KernelResult.InvalidPriority);
            }

            process.DefaultCpuCore = cpuCore;

            Result result = process.Start(priority, mainThreadStackSize);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            process.IncrementReferenceCount();

            return CheckResult(Result.Success);
        }

        public Result TerminateProcess(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            process = process.HandleTable.GetObject<KProcess>(handle);

            Result result;

            if (process != null)
            {
                if (process == _context.Scheduler.GetCurrentProcess())
                {
                    result = Result.Success;
                    process.DecrementToZeroWhileTerminatingCurrent();
                }
                else
                {
                    result = process.Terminate();
                    process.DecrementReferenceCount();
                }
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            return CheckResult(result);
        }
    }
}
