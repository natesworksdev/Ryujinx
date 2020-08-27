using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using Ryujinx.Memory;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result CreateThread(
            ulong entrypoint,
            ulong argsPtr,
            ulong stackTop,
            int priority,
            int cpuCore,
            out int handle)
        {
            handle = 0;

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (cpuCore == -2)
            {
                cpuCore = currentProcess.DefaultCpuCore;
            }

            if ((uint)cpuCore >= KScheduler.CpuCoresCount || !currentProcess.IsCpuCoreAllowed(cpuCore))
            {
                return CheckResult(KernelResult.InvalidCpuCore);
            }

            if ((uint)priority >= KScheduler.PrioritiesCount || !currentProcess.IsPriorityAllowed(priority))
            {
                return CheckResult(KernelResult.InvalidPriority);
            }

            long timeout = KTimeManager.ConvertMillisecondsToNanoseconds(100);

            if (currentProcess.ResourceLimit != null &&
               !currentProcess.ResourceLimit.Reserve(LimitableResource.Thread, 1, timeout))
            {
                return CheckResult(KernelResult.ResLimitExceeded);
            }

            KThread thread = new KThread(_context);

            Result result = currentProcess.InitializeThread(
                thread,
                entrypoint,
                argsPtr,
                stackTop,
                priority,
                cpuCore);

            if (result == Result.Success)
            {
                KProcess process = _context.Scheduler.GetCurrentProcess();

                result = process.HandleTable.GenerateHandle(thread, out handle);
            }
            else
            {
                currentProcess.ResourceLimit?.Release(LimitableResource.Thread, 1);
            }

            thread.DecrementReferenceCount();

            return CheckResult(result);
        }

        public Result StartThread(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                thread.IncrementReferenceCount();

                Result result = thread.Start();

                if (result == Result.Success)
                {
                    thread.IncrementReferenceCount();
                }

                thread.DecrementReferenceCount();

                return CheckResult(result);
            }
            else
            {
                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public void ExitThread()
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            _context.Scheduler.ExitThread(currentThread);

            currentThread.Exit();

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);
        }

        public void SleepThread(long timeout)
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            if (timeout < 1)
            {
                switch (timeout)
                {
                    case 0: currentThread.Yield(); break;
                    case -1: currentThread.YieldWithLoadBalancing(); break;
                    case -2: currentThread.YieldAndWaitForLoadBalancing(); break;
                }
            }
            else
            {
                currentThread.Sleep(timeout);
            }

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);
        }

        public Result GetThreadPriority(int handle, out int priority)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                priority = thread.DynamicPriority;

                return CheckResult(Result.Success);
            }
            else
            {
                priority = 0;

                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public Result SetThreadPriority(int handle, int priority)
        {
            // TODO: NPDM check.

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            thread.SetPriority(priority);

            return CheckResult(Result.Success);
        }

        public Result GetThreadCoreMask(int handle, out int preferredCore, out long affinityMask)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                preferredCore = thread.PreferredCore;
                affinityMask = thread.AffinityMask;

                return CheckResult(Result.Success);
            }
            else
            {
                preferredCore = 0;
                affinityMask = 0;

                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public Result SetThreadCoreMask(int handle, int preferredCore, long affinityMask)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (preferredCore == -2)
            {
                preferredCore = currentProcess.DefaultCpuCore;

                affinityMask = 1 << preferredCore;
            }
            else
            {
                if ((currentProcess.Capabilities.AllowedCpuCoresMask | affinityMask) !=
                     currentProcess.Capabilities.AllowedCpuCoresMask)
                {
                    return CheckResult(KernelResult.InvalidCpuCore);
                }

                if (affinityMask == 0)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }

                if ((uint)preferredCore > 3)
                {
                    if ((preferredCore | 2) != -1)
                    {
                        return CheckResult(KernelResult.InvalidCpuCore);
                    }
                }
                else if ((affinityMask & (1 << preferredCore)) == 0)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            return CheckResult(thread.SetCoreAndAffinityMask(preferredCore, affinityMask));
        }

        public int GetCurrentProcessorNumber()
        {
            int currentCore = _context.Scheduler.GetCurrentThread().CurrentCore;

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);

            return currentCore;
        }

        public Result GetThreadId(int handle, out long threadUid)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                threadUid = thread.ThreadUid;

                return CheckResult(Result.Success);
            }
            else
            {
                threadUid = 0;

                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public Result SetThreadActivity(int handle, bool pause)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetObject<KThread>(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (thread.Owner != process)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (thread == _context.Scheduler.GetCurrentThread())
            {
                return CheckResult(KernelResult.InvalidThread);
            }

            return CheckResult(thread.SetActivity(pause));
        }

        public Result GetThreadContext3(ulong address, int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            KThread thread = currentProcess.HandleTable.GetObject<KThread>(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (thread.Owner != currentProcess)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (currentThread == thread)
            {
                return CheckResult(KernelResult.InvalidThread);
            }

            IAddressSpaceManager memory = currentProcess.CpuMemory;

            memory.Write(address + 0x0, thread.Context.GetX(0));
            memory.Write(address + 0x8, thread.Context.GetX(1));
            memory.Write(address + 0x10, thread.Context.GetX(2));
            memory.Write(address + 0x18, thread.Context.GetX(3));
            memory.Write(address + 0x20, thread.Context.GetX(4));
            memory.Write(address + 0x28, thread.Context.GetX(5));
            memory.Write(address + 0x30, thread.Context.GetX(6));
            memory.Write(address + 0x38, thread.Context.GetX(7));
            memory.Write(address + 0x40, thread.Context.GetX(8));
            memory.Write(address + 0x48, thread.Context.GetX(9));
            memory.Write(address + 0x50, thread.Context.GetX(10));
            memory.Write(address + 0x58, thread.Context.GetX(11));
            memory.Write(address + 0x60, thread.Context.GetX(12));
            memory.Write(address + 0x68, thread.Context.GetX(13));
            memory.Write(address + 0x70, thread.Context.GetX(14));
            memory.Write(address + 0x78, thread.Context.GetX(15));
            memory.Write(address + 0x80, thread.Context.GetX(16));
            memory.Write(address + 0x88, thread.Context.GetX(17));
            memory.Write(address + 0x90, thread.Context.GetX(18));
            memory.Write(address + 0x98, thread.Context.GetX(19));
            memory.Write(address + 0xa0, thread.Context.GetX(20));
            memory.Write(address + 0xa8, thread.Context.GetX(21));
            memory.Write(address + 0xb0, thread.Context.GetX(22));
            memory.Write(address + 0xb8, thread.Context.GetX(23));
            memory.Write(address + 0xc0, thread.Context.GetX(24));
            memory.Write(address + 0xc8, thread.Context.GetX(25));
            memory.Write(address + 0xd0, thread.Context.GetX(26));
            memory.Write(address + 0xd8, thread.Context.GetX(27));
            memory.Write(address + 0xe0, thread.Context.GetX(28));
            memory.Write(address + 0xe8, thread.Context.GetX(29));
            memory.Write(address + 0xf0, thread.Context.GetX(30));
            memory.Write(address + 0xf8, thread.Context.GetX(31));

            memory.Write(address + 0x100, thread.LastPc);

            memory.Write(address + 0x108, thread.Context.Cpsr);

            memory.Write(address + 0x110, thread.Context.GetV(0));
            memory.Write(address + 0x120, thread.Context.GetV(1));
            memory.Write(address + 0x130, thread.Context.GetV(2));
            memory.Write(address + 0x140, thread.Context.GetV(3));
            memory.Write(address + 0x150, thread.Context.GetV(4));
            memory.Write(address + 0x160, thread.Context.GetV(5));
            memory.Write(address + 0x170, thread.Context.GetV(6));
            memory.Write(address + 0x180, thread.Context.GetV(7));
            memory.Write(address + 0x190, thread.Context.GetV(8));
            memory.Write(address + 0x1a0, thread.Context.GetV(9));
            memory.Write(address + 0x1b0, thread.Context.GetV(10));
            memory.Write(address + 0x1c0, thread.Context.GetV(11));
            memory.Write(address + 0x1d0, thread.Context.GetV(12));
            memory.Write(address + 0x1e0, thread.Context.GetV(13));
            memory.Write(address + 0x1f0, thread.Context.GetV(14));
            memory.Write(address + 0x200, thread.Context.GetV(15));
            memory.Write(address + 0x210, thread.Context.GetV(16));
            memory.Write(address + 0x220, thread.Context.GetV(17));
            memory.Write(address + 0x230, thread.Context.GetV(18));
            memory.Write(address + 0x240, thread.Context.GetV(19));
            memory.Write(address + 0x250, thread.Context.GetV(20));
            memory.Write(address + 0x260, thread.Context.GetV(21));
            memory.Write(address + 0x270, thread.Context.GetV(22));
            memory.Write(address + 0x280, thread.Context.GetV(23));
            memory.Write(address + 0x290, thread.Context.GetV(24));
            memory.Write(address + 0x2a0, thread.Context.GetV(25));
            memory.Write(address + 0x2b0, thread.Context.GetV(26));
            memory.Write(address + 0x2c0, thread.Context.GetV(27));
            memory.Write(address + 0x2d0, thread.Context.GetV(28));
            memory.Write(address + 0x2e0, thread.Context.GetV(29));
            memory.Write(address + 0x2f0, thread.Context.GetV(30));
            memory.Write(address + 0x300, thread.Context.GetV(31));

            memory.Write(address + 0x310, thread.Context.Fpcr);
            memory.Write(address + 0x314, thread.Context.Fpsr);
            memory.Write(address + 0x318, thread.Context.TlsAddress);

            return CheckResult(Result.Success);
        }
    }
}
