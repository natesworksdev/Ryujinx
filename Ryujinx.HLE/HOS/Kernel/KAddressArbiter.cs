using ChocolArm64.Memory;
using System.Collections.Generic;
using System.Linq;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KAddressArbiter
    {
        private const int HasListenersMask = 0x40000000;

        private Horizon _system;

        public List<KThread> CondVarThreads;
        public List<KThread> ArbiterThreads;

        public KAddressArbiter(Horizon system)
        {
            this._system = system;

            CondVarThreads = new List<KThread>();
            ArbiterThreads = new List<KThread>();
        }

        public long ArbitrateLock(
            Process process,
            AMemory memory,
            int     ownerHandle,
            long    mutexAddress,
            int     requesterHandle)
        {
            _system.CriticalSectionLock.Lock();

            KThread currentThread = _system.Scheduler.GetCurrentThread();

            currentThread.SignaledObj   = null;
            currentThread.ObjSyncResult = 0;

            if (!UserToKernelInt32(memory, mutexAddress, out int mutexValue))
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);;
            }

            if (mutexValue != (ownerHandle | HasListenersMask))
            {
                _system.CriticalSectionLock.Unlock();

                return 0;
            }

            KThread mutexOwner = process.HandleTable.GetObject<KThread>(ownerHandle);

            if (mutexOwner == null)
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);
            }

            currentThread.MutexAddress             = mutexAddress;
            currentThread.ThreadHandleForUserMutex = requesterHandle;

            mutexOwner.AddMutexWaiter(currentThread);

            currentThread.Reschedule(ThreadSchedState.Paused);

            _system.CriticalSectionLock.Unlock();
            _system.CriticalSectionLock.Lock();

            if (currentThread.MutexOwner != null) currentThread.MutexOwner.RemoveMutexWaiter(currentThread);

            _system.CriticalSectionLock.Unlock();

            return (uint)currentThread.ObjSyncResult;
        }

        public long ArbitrateUnlock(AMemory memory, long mutexAddress)
        {
            _system.CriticalSectionLock.Lock();

            KThread currentThread = _system.Scheduler.GetCurrentThread();

            (long result, KThread newOwnerThread) = MutexUnlock(memory, currentThread, mutexAddress);

            if (result != 0 && newOwnerThread != null)
            {
                newOwnerThread.SignaledObj   = null;
                newOwnerThread.ObjSyncResult = (int)result;
            }

            _system.CriticalSectionLock.Unlock();

            return result;
        }

        public long WaitProcessWideKeyAtomic(
            AMemory memory,
            long    mutexAddress,
            long    condVarAddress,
            int     threadHandle,
            long    timeout)
        {
            _system.CriticalSectionLock.Lock();

            KThread currentThread = _system.Scheduler.GetCurrentThread();

            currentThread.SignaledObj   = null;
            currentThread.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.Timeout);

            if (currentThread.ShallBeTerminated ||
                currentThread.SchedFlags == ThreadSchedState.TerminationPending)
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);
            }

            (long result, _) = MutexUnlock(memory, currentThread, mutexAddress);

            if (result != 0)
            {
                _system.CriticalSectionLock.Unlock();

                return result;
            }

            currentThread.MutexAddress             = mutexAddress;
            currentThread.ThreadHandleForUserMutex = threadHandle;
            currentThread.CondVarAddress           = condVarAddress;

            CondVarThreads.Add(currentThread);

            if (timeout != 0)
            {
                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0) _system.TimeManager.ScheduleFutureInvocation(currentThread, timeout);
            }

            _system.CriticalSectionLock.Unlock();

            if (timeout > 0) _system.TimeManager.UnscheduleFutureInvocation(currentThread);

            _system.CriticalSectionLock.Lock();

            if (currentThread.MutexOwner != null) currentThread.MutexOwner.RemoveMutexWaiter(currentThread);

            CondVarThreads.Remove(currentThread);

            _system.CriticalSectionLock.Unlock();

            return (uint)currentThread.ObjSyncResult;
        }

        private (long, KThread) MutexUnlock(AMemory memory, KThread currentThread, long mutexAddress)
        {
            KThread newOwnerThread = currentThread.RelinquishMutex(mutexAddress, out int count);

            int mutexValue = 0;

            if (newOwnerThread != null)
            {
                mutexValue = newOwnerThread.ThreadHandleForUserMutex;

                if (count >= 2) mutexValue |= HasListenersMask;

                newOwnerThread.SignaledObj   = null;
                newOwnerThread.ObjSyncResult = 0;

                newOwnerThread.ReleaseAndResume();
            }

            long result = 0;

            if (!KernelToUserInt32(memory, mutexAddress, mutexValue)) result = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

            return (result, newOwnerThread);
        }

        public void SignalProcessWideKey(Process process, AMemory memory, long address, int count)
        {
            Queue<KThread> signaledThreads = new Queue<KThread>();

            _system.CriticalSectionLock.Lock();

            IOrderedEnumerable<KThread> sortedThreads = CondVarThreads.OrderBy(x => x.DynamicPriority);

            foreach (KThread thread in sortedThreads.Where(x => x.CondVarAddress == address))
            {
                TryAcquireMutex(process, memory, thread);

                signaledThreads.Enqueue(thread);

                //If the count is <= 0, we should signal all threads waiting.
                if (count >= 1 && --count == 0) break;
            }

            while (signaledThreads.TryDequeue(out KThread thread)) CondVarThreads.Remove(thread);

            _system.CriticalSectionLock.Unlock();
        }

        private KThread TryAcquireMutex(Process process, AMemory memory, KThread requester)
        {
            long address = requester.MutexAddress;

            memory.SetExclusive(0, address);

            if (!UserToKernelInt32(memory, address, out int mutexValue))
            {
                //Invalid address.
                memory.ClearExclusive(0);

                requester.SignaledObj   = null;
                requester.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return null;
            }

            while (true)
            {
                if (memory.TestExclusive(0, address))
                {
                    if (mutexValue != 0)
                        memory.WriteInt32(address, mutexValue | HasListenersMask);
                    else
                        memory.WriteInt32(address, requester.ThreadHandleForUserMutex);

                    memory.ClearExclusiveForStore(0);

                    break;
                }

                memory.SetExclusive(0, address);

                mutexValue = memory.ReadInt32(address);
            }

            if (mutexValue == 0)
            {
                //We now own the mutex.
                requester.SignaledObj   = null;
                requester.ObjSyncResult = 0;

                requester.ReleaseAndResume();

                return null;
            }

            mutexValue &= ~HasListenersMask;

            KThread mutexOwner = process.HandleTable.GetObject<KThread>(mutexValue);

            if (mutexOwner != null)
            {
                //Mutex already belongs to another thread, wait for it.
                mutexOwner.AddMutexWaiter(requester);
            }
            else
            {
                //Invalid mutex owner.
                requester.SignaledObj   = null;
                requester.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                requester.ReleaseAndResume();
            }

            return mutexOwner;
        }

        public long WaitForAddressIfEqual(AMemory memory, long address, int value, long timeout)
        {
            KThread currentThread = _system.Scheduler.GetCurrentThread();

            _system.CriticalSectionLock.Lock();

            if (currentThread.ShallBeTerminated ||
                currentThread.SchedFlags == ThreadSchedState.TerminationPending)
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);
            }

            currentThread.SignaledObj   = null;
            currentThread.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.Timeout);

            if (!UserToKernelInt32(memory, address, out int currentValue))
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
            }

            if (currentValue == value)
            {
                if (timeout == 0)
                {
                    _system.CriticalSectionLock.Unlock();

                    return MakeError(ErrorModule.Kernel, KernelErr.Timeout);
                }

                currentThread.MutexAddress         = address;
                currentThread.WaitingInArbitration = true;

                InsertSortedByPriority(ArbiterThreads, currentThread);

                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0) _system.TimeManager.ScheduleFutureInvocation(currentThread, timeout);

                _system.CriticalSectionLock.Unlock();

                if (timeout > 0) _system.TimeManager.UnscheduleFutureInvocation(currentThread);

                _system.CriticalSectionLock.Lock();

                if (currentThread.WaitingInArbitration)
                {
                    ArbiterThreads.Remove(currentThread);

                    currentThread.WaitingInArbitration = false;
                }

                _system.CriticalSectionLock.Unlock();

                return currentThread.ObjSyncResult;
            }

            _system.CriticalSectionLock.Unlock();

            return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
        }

        public long WaitForAddressIfLessThan(
            AMemory memory,
            long    address,
            int     value,
            bool    shouldDecrement,
            long    timeout)
        {
            KThread currentThread = _system.Scheduler.GetCurrentThread();

            _system.CriticalSectionLock.Lock();

            if (currentThread.ShallBeTerminated ||
                currentThread.SchedFlags == ThreadSchedState.TerminationPending)
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);
            }

            currentThread.SignaledObj   = null;
            currentThread.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.Timeout);

            //If ShouldDecrement is true, do atomic decrement of the value at Address.
            memory.SetExclusive(0, address);

            if (!UserToKernelInt32(memory, address, out int currentValue))
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
            }

            if (shouldDecrement)
                while (currentValue < value)
                {
                    if (memory.TestExclusive(0, address))
                    {
                        memory.WriteInt32(address, currentValue - 1);

                        memory.ClearExclusiveForStore(0);

                        break;
                    }

                    memory.SetExclusive(0, address);

                    currentValue = memory.ReadInt32(address);
                }

            memory.ClearExclusive(0);

            if (currentValue < value)
            {
                if (timeout == 0)
                {
                    _system.CriticalSectionLock.Unlock();

                    return MakeError(ErrorModule.Kernel, KernelErr.Timeout);
                }

                currentThread.MutexAddress         = address;
                currentThread.WaitingInArbitration = true;

                InsertSortedByPriority(ArbiterThreads, currentThread);

                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0) _system.TimeManager.ScheduleFutureInvocation(currentThread, timeout);

                _system.CriticalSectionLock.Unlock();

                if (timeout > 0) _system.TimeManager.UnscheduleFutureInvocation(currentThread);

                _system.CriticalSectionLock.Lock();

                if (currentThread.WaitingInArbitration)
                {
                    ArbiterThreads.Remove(currentThread);

                    currentThread.WaitingInArbitration = false;
                }

                _system.CriticalSectionLock.Unlock();

                return currentThread.ObjSyncResult;
            }

            _system.CriticalSectionLock.Unlock();

            return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
        }

        private void InsertSortedByPriority(List<KThread> threads, KThread thread)
        {
            int nextIndex = -1;

            for (int index = 0; index < threads.Count; index++)
                if (threads[index].DynamicPriority > thread.DynamicPriority)
                {
                    nextIndex = index;

                    break;
                }

            if (nextIndex != -1)
                threads.Insert(nextIndex, thread);
            else
                threads.Add(thread);
        }

        public long Signal(long address, int count)
        {
            _system.CriticalSectionLock.Lock();

            WakeArbiterThreads(address, count);

            _system.CriticalSectionLock.Unlock();

            return 0;
        }

        public long SignalAndIncrementIfEqual(AMemory memory, long address, int value, int count)
        {
            _system.CriticalSectionLock.Lock();

            memory.SetExclusive(0, address);

            if (!UserToKernelInt32(memory, address, out int currentValue))
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
            }

            while (currentValue == value)
            {
                if (memory.TestExclusive(0, address))
                {
                    memory.WriteInt32(address, currentValue + 1);

                    memory.ClearExclusiveForStore(0);

                    break;
                }

                memory.SetExclusive(0, address);

                currentValue = memory.ReadInt32(address);
            }

            memory.ClearExclusive(0);

            if (currentValue != value)
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
            }

            WakeArbiterThreads(address, count);

            _system.CriticalSectionLock.Unlock();

            return 0;
        }

        public long SignalAndModifyIfEqual(AMemory memory, long address, int value, int count)
        {
            _system.CriticalSectionLock.Lock();

            int offset;

            //The value is decremented if the number of threads waiting is less
            //or equal to the Count of threads to be signaled, or Count is zero
            //or negative. It is incremented if there are no threads waiting.
            int waitingCount = 0;

            foreach (KThread thread in ArbiterThreads.Where(x => x.MutexAddress == address))
                if (++waitingCount > count) break;

            if (waitingCount > 0)
                offset = waitingCount <= count || count <= 0 ? -1 : 0;
            else
                offset = 1;

            memory.SetExclusive(0, address);

            if (!UserToKernelInt32(memory, address, out int currentValue))
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
            }

            while (currentValue == value)
            {
                if (memory.TestExclusive(0, address))
                {
                    memory.WriteInt32(address, currentValue + offset);

                    memory.ClearExclusiveForStore(0);

                    break;
                }

                memory.SetExclusive(0, address);

                currentValue = memory.ReadInt32(address);
            }

            memory.ClearExclusive(0);

            if (currentValue != value)
            {
                _system.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
            }

            WakeArbiterThreads(address, count);

            _system.CriticalSectionLock.Unlock();

            return 0;
        }

        private void WakeArbiterThreads(long address, int count)
        {
            Queue<KThread> signaledThreads = new Queue<KThread>();

            foreach (KThread thread in ArbiterThreads.Where(x => x.MutexAddress == address))
            {
                signaledThreads.Enqueue(thread);

                //If the count is <= 0, we should signal all threads waiting.
                if (count >= 1 && --count == 0) break;
            }

            while (signaledThreads.TryDequeue(out KThread thread))
            {
                thread.SignaledObj   = null;
                thread.ObjSyncResult = 0;

                thread.ReleaseAndResume();

                thread.WaitingInArbitration = false;

                ArbiterThreads.Remove(thread);
            }
        }

        private bool UserToKernelInt32(AMemory memory, long address, out int value)
        {
            if (memory.IsMapped(address))
            {
                value = memory.ReadInt32(address);

                return true;
            }

            value = 0;

            return false;
        }

        private bool KernelToUserInt32(AMemory memory, long address, int value)
        {
            if (memory.IsMapped(address))
            {
                memory.WriteInt32ToSharedAddr(address, value);

                return true;
            }

            return false;
        }
    }
}
