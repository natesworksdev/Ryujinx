using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Memory;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using System;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result CloseHandle(int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return CheckResult(currentProcess.HandleTable.CloseHandle(handle) ? Result.Success : KernelResult.InvalidHandle);
        }

        public ulong GetSystemTick()
        {
            ulong sytemTick = _context.Scheduler.GetCurrentThread().Context.Counter;

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);

            return sytemTick;
        }

        public void Break(ulong reason)
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            if ((reason & (1UL << 31)) == 0)
            {
                currentThread.PrintGuestStackTrace();

                // As the process is exiting, this is probably caused by emulation termination.
                if (currentThread.Owner.State == KProcessState.Exiting)
                {
                    return;
                }

                // TODO: Debug events.
                currentThread.Owner.TerminateCurrentProcess();

                // TODO: Proper exception.
                throw new Exception("Guest program broke execution");
            }
            else
            {
                Logger.Debug?.Print(LogClass.KernelSvc, "Debugger triggered.");
            }

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);
        }

        public Result OutputDebugString(ulong strPtr, ulong size)
        {
            if (size == 0)
            {
                return CheckResult(Result.Success);
            }

            if (!KernelTransfer.UserToKernelString(_context, strPtr, size, out string debugString))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            Logger.Warning?.Print(LogClass.KernelSvc, debugString);

            return CheckResult(Result.Success);
        }

        public Result GetInfo(InfoType id, int handle, long subId, out ulong value)
        {
            value = 0;

            switch (id)
            {
                case InfoType.CoreMask:
                case InfoType.PriorityMask:
                case InfoType.AliasRegionAddress:
                case InfoType.AliasRegionSize:
                case InfoType.HeapRegionAddress:
                case InfoType.HeapRegionSize:
                case InfoType.TotalMemorySize:
                case InfoType.UsedMemorySize:
                case InfoType.AslrRegionAddress:
                case InfoType.AslrRegionSize:
                case InfoType.StackRegionAddress:
                case InfoType.StackRegionSize:
                case InfoType.SystemResourceSizeTotal:
                case InfoType.SystemResourceSizeUsed:
                case InfoType.ProgramId:
                case InfoType.UserExceptionContextAddress:
                case InfoType.TotalNonSystemMemorySize:
                case InfoType.UsedNonSystemMemorySize:
                    {
                        if (subId != 0)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                        KProcess process = currentProcess.HandleTable.GetKProcess(handle);

                        if (process == null)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        switch (id)
                        {
                            case InfoType.CoreMask:
                                value = (ulong)process.Capabilities.AllowedCpuCoresMask;
                                break;
                            case InfoType.PriorityMask:
                                value = (ulong)process.Capabilities.AllowedThreadPriosMask;
                                break;

                            case InfoType.AliasRegionAddress:
                                value = process.MemoryManager.AliasRegionStart;
                                break;
                            case InfoType.AliasRegionSize:
                                value = process.MemoryManager.AliasRegionEnd - process.MemoryManager.AliasRegionStart;
                                break;

                            case InfoType.HeapRegionAddress:
                                value = process.MemoryManager.HeapRegionStart;
                                break;
                            case InfoType.HeapRegionSize:
                                value = process.MemoryManager.HeapRegionEnd - process.MemoryManager.HeapRegionStart;
                                break;

                            case InfoType.TotalMemorySize:
                                value = process.GetMemoryCapacity();
                                break;
                            case InfoType.UsedMemorySize:
                                value = process.GetMemoryUsage();
                                break;

                            case InfoType.AslrRegionAddress:
                                value = process.MemoryManager.GetAddrSpaceBaseAddr();
                                break;
                            case InfoType.AslrRegionSize:
                                value = process.MemoryManager.GetAddrSpaceSize();
                                break;

                            case InfoType.StackRegionAddress:
                                value = process.MemoryManager.StackRegionStart;
                                break;
                            case InfoType.StackRegionSize:
                                value = process.MemoryManager.StackRegionEnd - process.MemoryManager.StackRegionStart;
                                break;

                            case InfoType.SystemResourceSizeTotal:
                                value = process.PersonalMmHeapPagesCount * KMemoryManager.PageSize;
                                break;
                            case InfoType.SystemResourceSizeUsed:
                                if (process.PersonalMmHeapPagesCount != 0)
                                {
                                    value = process.MemoryManager.GetMmUsedPages() * KMemoryManager.PageSize;
                                }
                                break;

                            case InfoType.ProgramId:
                                value = process.TitleId;
                                break;

                            case InfoType.UserExceptionContextAddress:
                                value = process.UserExceptionContextAddress;
                                break;

                            case InfoType.TotalNonSystemMemorySize:
                                value = process.GetMemoryCapacityWithoutPersonalMmHeap();
                                break;
                            case InfoType.UsedNonSystemMemorySize:
                                value = process.GetMemoryUsageWithoutPersonalMmHeap();
                                break;
                        }

                        break;
                    }

                case InfoType.DebuggerAttached:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        if (subId != 0)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        value = _context.Scheduler.GetCurrentProcess().Debug ? 1u : 0u;

                        break;
                    }

                case InfoType.ResourceLimit:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        if (subId != 0)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                        if (currentProcess.ResourceLimit != null)
                        {
                            KHandleTable handleTable = currentProcess.HandleTable;
                            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

                            Result result = handleTable.GenerateHandle(resourceLimit, out int resLimHandle);

                            if (result != Result.Success)
                            {
                                return CheckResult(result);
                            }

                            value = (uint)resLimHandle;
                        }

                        break;
                    }

                case InfoType.IdleTickCount:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        int currentCore = _context.Scheduler.GetCurrentThread().CurrentCore;

                        if (subId != -1 && subId != currentCore)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        value = (ulong)_context.Scheduler.CoreContexts[currentCore].TotalIdleTimeTicks;

                        break;
                    }

                case InfoType.RandomEntropy:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        if ((ulong)subId > 3)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                        value = currentProcess.RandomEntropy[subId];

                        break;
                    }

                case InfoType.ThreadTickCount:
                    {
                        if (subId < -1 || subId > 3)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KThread thread = _context.Scheduler.GetCurrentProcess().HandleTable.GetKThread(handle);

                        if (thread == null)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        KThread currentThread = _context.Scheduler.GetCurrentThread();

                        int currentCore = currentThread.CurrentCore;

                        if (subId != -1 && subId != currentCore)
                        {
                            return CheckResult(Result.Success);
                        }

                        KCoreContext coreContext = _context.Scheduler.CoreContexts[currentCore];

                        long timeDelta = PerformanceCounter.ElapsedMilliseconds - coreContext.LastContextSwitchTime;

                        if (subId != -1)
                        {
                            value = (ulong)KTimeManager.ConvertMillisecondsToTicks(timeDelta);
                        }
                        else
                        {
                            long totalTimeRunning = thread.TotalTimeRunning;

                            if (thread == currentThread)
                            {
                                totalTimeRunning += timeDelta;
                            }

                            value = (ulong)KTimeManager.ConvertMillisecondsToTicks(totalTimeRunning);
                        }

                        break;
                    }

                default: return CheckResult(KernelResult.InvalidEnumValue);
            }

            return CheckResult(Result.Success);
        }

        public Result GetSystemInfo(uint id, int handle, long subId, out long value)
        {
            value = 0;

            if (id > 2)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            if (handle != 0)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (id < 2)
            {
                if ((ulong)subId > 3)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }

                KMemoryRegionManager region = _context.MemoryRegions[subId];

                switch (id)
                {
                    // Memory region capacity.
                    case 0: value = (long)region.Size; break;

                    // Memory region free space.
                    case 1:
                        ulong freePagesCount = region.GetFreePages();

                        value = (long)(freePagesCount * KMemoryManager.PageSize);

                        break;
                }
            }
            else /* if (Id == 2) */
            {
                if ((ulong)subId > 1)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }

                switch (subId)
                {
                    case 0: value = _context.PrivilegedProcessLowestId; break;
                    case 1: value = _context.PrivilegedProcessHighestId; break;
                }
            }

            return CheckResult(Result.Success);
        }
    }
}
