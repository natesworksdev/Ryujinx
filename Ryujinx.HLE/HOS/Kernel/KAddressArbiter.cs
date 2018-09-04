using ChocolArm64.Memory;
using System.Collections.Generic;
using System.Linq;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KAddressArbiter
    {
        private const int HasListenersMask = 0x40000000;

        private Horizon System;

        public List<KThread> CondVarThreads;

        public KAddressArbiter(Horizon System)
        {
            this.System = System;

            CondVarThreads = new List<KThread>();
        }

        public long ArbitrateLock(
            Process Process,
            AMemory Memory,
            int     OwnerHandle,
            long    MutexAddress,
            int     RequesterHandle)
        {
            System.CriticalSectionLock.Lock();

            KThread CurrentThread = System.Scheduler.GetCurrentThread();

            CurrentThread.SignaledObj   = null;
            CurrentThread.ObjSyncResult = 0;

            int MutexValue = Memory.ReadInt32(MutexAddress);

            if (MutexValue != (OwnerHandle | HasListenersMask))
            {
                System.CriticalSectionLock.Unlock();

                return 0;
            }

            KThread MutexOwner = Process.HandleTable.GetData<KThread>(OwnerHandle);

            if (MutexOwner == null)
            {
                System.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);
            }

            CurrentThread.MutexAddress             = MutexAddress;
            CurrentThread.ThreadHandleForUserMutex = RequesterHandle;

            MutexOwner.AddMutexWaiter(CurrentThread);

            CurrentThread.Reschedule(ThreadSchedState.Paused);

            System.CriticalSectionLock.Unlock();
            System.CriticalSectionLock.Lock();

            if (CurrentThread.MutexOwner != null)
            {
                CurrentThread.MutexOwner.RemoveMutexWaiter(CurrentThread);
            }

            System.CriticalSectionLock.Unlock();

            return (uint)CurrentThread.ObjSyncResult;
        }

        public long ArbitrateUnlock(Process Process, AMemory Memory, long MutexAddress)
        {
            System.CriticalSectionLock.Lock();

            KThread CurrentThread = System.Scheduler.GetCurrentThread();

            MutexUnlock(Memory, CurrentThread, MutexAddress);

            System.CriticalSectionLock.Unlock();

            return 0;
        }

        public long WaitProcessWideKeyAtomic(
            Process Process,
            AMemory Memory,
            long    MutexAddress,
            long    CondVarAddress,
            int     ThreadHandle,
            long    Timeout)
        {
            System.CriticalSectionLock.Lock();

            KThread CurrentThread = System.Scheduler.GetCurrentThread();

            CurrentThread.SignaledObj   = null;
            CurrentThread.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.Timeout);

            if (CurrentThread.ShallBeTerminated ||
                CurrentThread.SchedFlags == ThreadSchedState.TerminationPending)
            {
                System.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);
            }

            MutexUnlock(Memory, CurrentThread, MutexAddress);

            CurrentThread.MutexAddress             = MutexAddress;
            CurrentThread.ThreadHandleForUserMutex = ThreadHandle;
            CurrentThread.CondVarAddress           = CondVarAddress;

            CondVarThreads.Add(CurrentThread);

            if (Timeout != 0)
            {
                CurrentThread.Reschedule(ThreadSchedState.Paused);

                if (Timeout >= 1)
                {
                    System.TimeManager.ScheduleFutureInvocation(CurrentThread, Timeout);
                }
            }

            System.CriticalSectionLock.Unlock();

            if (Timeout >= 1)
            {
                System.TimeManager.UnscheduleFutureInvocation(CurrentThread);
            }

            System.CriticalSectionLock.Lock();

            if (CurrentThread.MutexOwner != null)
            {
                CurrentThread.MutexOwner.RemoveMutexWaiter(CurrentThread);
            }

            CondVarThreads.Remove(CurrentThread);

            System.CriticalSectionLock.Unlock();

            return (uint)CurrentThread.ObjSyncResult;
        }

        private void MutexUnlock(AMemory Memory, KThread CurrentThread, long MutexAddress)
        {
            KThread NewOwnerThread = CurrentThread.RelinquishMutex(MutexAddress, out int Count);

            int MutexValue = 0;

            if (NewOwnerThread != null)
            {
                MutexValue = NewOwnerThread.ThreadHandleForUserMutex;

                if (Count >= 2)
                {
                    MutexValue |= HasListenersMask;
                }

                NewOwnerThread.SignaledObj   = null;
                NewOwnerThread.ObjSyncResult = 0;

                NewOwnerThread.ReleaseAndResume();
            }

            Memory.WriteInt32ToSharedAddr(MutexAddress, MutexValue);
        }

        public void SignalProcessWideKey(Process Process, AMemory Memory, long Address, int Count)
        {
            Queue<KThread> SignaledThreads = new Queue<KThread>();

            System.CriticalSectionLock.Lock();

            IOrderedEnumerable<KThread> SortedThreads = CondVarThreads.OrderBy(x => x.DynamicPriority);

            foreach (KThread CondVarThread in SortedThreads.Where(x => x.CondVarAddress == Address))
            {
                TryAcquireMutex(Process, Memory, CondVarThread);

                SignaledThreads.Enqueue(CondVarThread);

                //If the count is <= 0, we should signal all threads waiting
                //for the conditional variable.
                if (Count >= 1 && --Count == 0)
                {
                    break;
                }
            }

            while (SignaledThreads.TryDequeue(out KThread Thread))
            {
                CondVarThreads.Remove(Thread);
            }

            System.CriticalSectionLock.Unlock();
        }

        private KThread TryAcquireMutex(Process Process, AMemory Memory, KThread Requester)
        {
            //TODO: Use correct core number of find a better way to do that.
            int Core = 0;

            long Address = Requester.MutexAddress;

            Memory.SetExclusive(Core, Address);

            int MutexValue = Memory.ReadInt32(Address);

            while (true)
            {
                if (Memory.TestExclusive(Core, Address))
                {
                    if (MutexValue != 0)
                    {
                        //Update value to indicate there is a mutex waiter now.
                        Memory.WriteInt32(Address, MutexValue | HasListenersMask);
                    }
                    else
                    {
                        //No thread owning the mutex, assign to requesting thread.
                        Memory.WriteInt32(Address, Requester.ThreadHandleForUserMutex);
                    }

                    Memory.ClearExclusiveForStore(Core);

                    break;
                }

                Memory.SetExclusive(Core, Address);

                MutexValue = Memory.ReadInt32(Address);
            }

            if (MutexValue == 0)
            {
                //We now own the mutex.
                Requester.SignaledObj   = null;
                Requester.ObjSyncResult = 0;

                Requester.ReleaseAndResume();

                return null;
            }

            MutexValue &= ~HasListenersMask;

            KThread MutexOwner = Process.HandleTable.GetData<KThread>(MutexValue);

            if (MutexOwner != null)
            {
                //Mutex already belongs to another thread, wait for it.
                MutexOwner.AddMutexWaiter(Requester);
            }
            else
            {
                //Invalid mutex owner.
                Requester.SignaledObj   = null;
                Requester.ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                Requester.ReleaseAndResume();
            }

            return MutexOwner;
        }

    }
}
