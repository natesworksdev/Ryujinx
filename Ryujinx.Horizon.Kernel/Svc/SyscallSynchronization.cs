using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using System;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result WaitSynchronization(out int handleIndex, ulong handlesPtr, int handlesCount, long timeout)
        {
            handleIndex = 0;

            if ((uint)handlesCount > KThread.MaxWaitSyncObjects)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KThread currentThread = _context.Scheduler.GetCurrentThread();

            if (handlesCount != 0)
            {
                KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                if (currentProcess.MemoryManager.AddrSpaceStart > handlesPtr)
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                long handlesSize = handlesCount * 4;

                if (handlesPtr + (ulong)handlesSize <= handlesPtr)
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                if (handlesPtr + (ulong)handlesSize - 1 > currentProcess.MemoryManager.AddrSpaceEnd - 1)
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                Span<int> handles = new Span<int>(currentThread.WaitSyncHandles).Slice(0, handlesCount);

                if (!KernelTransfer.UserToKernelInt32Array(_context, handlesPtr, handles))
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                return CheckResult(WaitSynchronization(out handleIndex, handles, timeout));
            }
            else
            {
                return CheckResult(WaitSynchronization(out handleIndex, Span<int>.Empty, timeout));
            }
        }

        public Result WaitSynchronization(out int handleIndex, ReadOnlySpan<int> handles, long timeout)
        {
            handleIndex = 0;

            if (handles.Length > KThread.MaxWaitSyncObjects)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KThread currentThread = _context.Scheduler.GetCurrentThread();

            var syncObjs = new Span<KSynchronizationObject>(currentThread.WaitSyncObjects).Slice(0, handles.Length);

            if (handles.Length != 0)
            {
                KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                int processedHandles = 0;

                for (; processedHandles < handles.Length; processedHandles++)
                {
                    KSynchronizationObject syncObj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[processedHandles]);

                    if (syncObj == null)
                    {
                        break;
                    }

                    syncObjs[processedHandles] = syncObj;

                    syncObj.IncrementReferenceCount();
                }

                if (processedHandles != handles.Length)
                {
                    // One or more handles are invalid.
                    for (int index = 0; index < processedHandles; index++)
                    {
                        currentThread.WaitSyncObjects[index].DecrementReferenceCount();
                    }

                    return CheckResult(KernelResult.InvalidHandle);
                }
            }

            Result result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex);

            if (result == KernelResult.PortRemoteClosed)
            {
                result = Result.Success;
            }

            for (int index = 0; index < handles.Length; index++)
            {
                currentThread.WaitSyncObjects[index].DecrementReferenceCount();
            }

            return CheckResult(result);
        }

        public Result CancelSynchronization(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            thread.CancelSynchronization();

            return CheckResult(Result.Success);
        }

        public Result ArbitrateLock(int ownerHandle, ulong mutexAddress, int requesterHandle)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return CheckResult(currentProcess.AddressArbiter.ArbitrateLock(ownerHandle, mutexAddress, requesterHandle));
        }

        public Result ArbitrateUnlock(ulong mutexAddress)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return CheckResult(currentProcess.AddressArbiter.ArbitrateUnlock(mutexAddress));
        }

        public Result WaitProcessWideKeyAtomic(
            ulong mutexAddress,
            ulong condVarAddress,
            int handle,
            long timeout)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return currentProcess.AddressArbiter.WaitProcessWideKeyAtomic(
                mutexAddress,
                condVarAddress,
                handle,
                timeout);
        }

        public Result SignalProcessWideKey(ulong address, int count)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            currentProcess.AddressArbiter.SignalProcessWideKey(address, count);

            return CheckResult(Result.Success);
        }

        public Result WaitForAddress(ulong address, ArbitrationType type, int value, long timeout)
        {
            if (IsPointingInsideKernel(address))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return type switch
            {
                ArbitrationType.WaitIfLessThan
                    => currentProcess.AddressArbiter.WaitForAddressIfLessThan(address, value, false, timeout),
                ArbitrationType.DecrementAndWaitIfLessThan
                    => currentProcess.AddressArbiter.WaitForAddressIfLessThan(address, value, true, timeout),
                ArbitrationType.WaitIfEqual
                    => currentProcess.AddressArbiter.WaitForAddressIfEqual(address, value, timeout),
                _ => KernelResult.InvalidEnumValue,
            };
        }

        public Result SignalToAddress(ulong address, SignalType type, int value, int count)
        {
            if (IsPointingInsideKernel(address))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return type switch
            {
                SignalType.Signal
                    => currentProcess.AddressArbiter.Signal(address, count),
                SignalType.SignalAndIncrementIfEqual
                    => currentProcess.AddressArbiter.SignalAndIncrementIfEqual(address, value, count),
                SignalType.SignalAndModifyIfEqual
                    => currentProcess.AddressArbiter.SignalAndModifyIfEqual(address, value, count),
                _ => KernelResult.InvalidEnumValue
            };
        }

        private static bool IsPointingInsideKernel(ulong address)
        {
            return (address + 0x1000000000) < 0xffffff000;
        }

        private static bool IsAddressNotWordAligned(ulong address)
        {
            return (address & 3) != 0;
        }
    }
}
