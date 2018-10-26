using ChocolArm64;
using System;
using System.Collections.Generic;
using System.Linq;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KThread : KSynchronizationObject, IKFutureSchedulerObject
    {
        public AThread Context { get; private set; }

        public long AffinityMask { get; set; }

        public int ThreadId { get; private set; }

        public KSynchronizationObject SignaledObj;

        public long CondVarAddress { get; set; }
        public long MutexAddress   { get; set; }

        public Process Owner { get; private set; }

        public long LastScheduledTicks { get; set; }

        public LinkedListNode<KThread>[] SiblingsPerCore { get; private set; }

        private LinkedListNode<KThread> _withholderNode;

        private LinkedList<KThread>     _mutexWaiters;
        private LinkedListNode<KThread> _mutexWaiterNode;

        public KThread MutexOwner { get; private set; }

        public int ThreadHandleForUserMutex { get; set; }

        private ThreadSchedState _forcePauseFlags;

        public int ObjSyncResult { get; set; }

        public int DynamicPriority { get; set; }
        public int CurrentCore     { get; set; }
        public int BasePriority    { get; set; }
        public int PreferredCore   { get; set; }

        private long _affinityMaskOverride;
        private int  _preferredCoreOverride;
        private int  _affinityOverrideCount;

        public ThreadSchedState SchedFlags { get; private set; }

        public bool ShallBeTerminated { get; private set; }

        public bool SyncCancelled { get; set; }
        public bool WaitingSync   { get; set; }

        private bool _hasExited;

        public bool WaitingInArbitration { get; set; }

        private KScheduler _scheduler;

        private KSchedulingData _schedulingData;

        public long LastPc { get; set; }

        public KThread(
            AThread thread,
            Process process,
            Horizon system,
            int     processorId,
            int     priority,
            int     threadId) : base(system)
        {
            this.ThreadId = threadId;

            Context        = thread;
            Owner          = process;
            PreferredCore  = processorId;
            _scheduler      = system.Scheduler;
            _schedulingData = system.Scheduler.SchedulingData;

            SiblingsPerCore = new LinkedListNode<KThread>[KScheduler.CpuCoresCount];

            _mutexWaiters = new LinkedList<KThread>();

            AffinityMask = 1 << processorId;

            DynamicPriority = BasePriority = priority;

            CurrentCore = PreferredCore;
        }

        public long Start()
        {
            long result = MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);

            System.CriticalSectionLock.Lock();

            if (!ShallBeTerminated)
            {
                KThread currentThread = System.Scheduler.GetCurrentThread();

                while (SchedFlags               != ThreadSchedState.TerminationPending &&
                       currentThread.SchedFlags != ThreadSchedState.TerminationPending &&
                       !currentThread.ShallBeTerminated)
                {
                    if ((SchedFlags & ThreadSchedState.LowNibbleMask) != ThreadSchedState.None)
                    {
                        result = MakeError(ErrorModule.Kernel, KernelErr.InvalidState);

                        break;
                    }

                    if (currentThread._forcePauseFlags == ThreadSchedState.None)
                    {
                        if (Owner != null && _forcePauseFlags != ThreadSchedState.None) CombineForcePauseFlags();

                        SetNewSchedFlags(ThreadSchedState.Running);

                        result = 0;

                        break;
                    }
                    else
                    {
                        currentThread.CombineForcePauseFlags();

                        System.CriticalSectionLock.Unlock();
                        System.CriticalSectionLock.Lock();

                        if (currentThread.ShallBeTerminated) break;
                    }
                }
            }

            System.CriticalSectionLock.Unlock();

            return result;
        }

        public void Exit()
        {
            System.CriticalSectionLock.Lock();

            _forcePauseFlags &= ~ThreadSchedState.ExceptionalMask;

            ExitImpl();

            System.CriticalSectionLock.Unlock();
        }

        private void ExitImpl()
        {
            System.CriticalSectionLock.Lock();

            SetNewSchedFlags(ThreadSchedState.TerminationPending);

            _hasExited = true;

            Signal();

            System.CriticalSectionLock.Unlock();
        }

        public long Sleep(long timeout)
        {
            System.CriticalSectionLock.Lock();

            if (ShallBeTerminated || SchedFlags == ThreadSchedState.TerminationPending)
            {
                System.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);
            }

            SetNewSchedFlags(ThreadSchedState.Paused);

            if (timeout > 0) System.TimeManager.ScheduleFutureInvocation(this, timeout);

            System.CriticalSectionLock.Unlock();

            if (timeout > 0) System.TimeManager.UnscheduleFutureInvocation(this);

            return 0;
        }

        public void Yield()
        {
            System.CriticalSectionLock.Lock();

            if (SchedFlags != ThreadSchedState.Running)
            {
                System.CriticalSectionLock.Unlock();

                System.Scheduler.ContextSwitch();

                return;
            }

            if (DynamicPriority < KScheduler.PrioritiesCount) _schedulingData.Reschedule(DynamicPriority, CurrentCore, this);

            _scheduler.ThreadReselectionRequested = true;

            System.CriticalSectionLock.Unlock();

            System.Scheduler.ContextSwitch();
        }

        public void YieldWithLoadBalancing()
        {
            System.CriticalSectionLock.Lock();

            int prio = DynamicPriority;
            int core = CurrentCore;

            if (SchedFlags != ThreadSchedState.Running)
            {
                System.CriticalSectionLock.Unlock();

                System.Scheduler.ContextSwitch();

                return;
            }

            KThread nextThreadOnCurrentQueue = null;

            if (DynamicPriority < KScheduler.PrioritiesCount)
            {
                //Move current thread to the end of the queue.
                _schedulingData.Reschedule(prio, core, this);

                Func<KThread, bool> predicate = x => x.DynamicPriority == prio;

                nextThreadOnCurrentQueue = _schedulingData.ScheduledThreads(core).FirstOrDefault(predicate);
            }

            IEnumerable<KThread> SuitableCandidates()
            {
                foreach (KThread thread in _schedulingData.SuggestedThreads(core))
                {
                    int srcCore = thread.CurrentCore;

                    if (srcCore >= 0)
                    {
                        KThread selectedSrcCore = _scheduler.CoreContexts[srcCore].SelectedThread;

                        if (selectedSrcCore == thread || (selectedSrcCore?.DynamicPriority ?? 2) < 2) continue;
                    }

                    //If the candidate was scheduled after the current thread, then it's not worth it,
                    //unless the priority is higher than the current one.
                    if (nextThreadOnCurrentQueue.LastScheduledTicks >= thread.LastScheduledTicks ||
                        nextThreadOnCurrentQueue.DynamicPriority    <  thread.DynamicPriority)
                        yield return thread;
                }
            }

            KThread dst = SuitableCandidates().FirstOrDefault(x => x.DynamicPriority <= prio);

            if (dst != null)
            {
                _schedulingData.TransferToCore(dst.DynamicPriority, core, dst);

                _scheduler.ThreadReselectionRequested = true;
            }

            if (this != nextThreadOnCurrentQueue) _scheduler.ThreadReselectionRequested = true;

            System.CriticalSectionLock.Unlock();

            System.Scheduler.ContextSwitch();
        }

        public void YieldAndWaitForLoadBalancing()
        {
            System.CriticalSectionLock.Lock();

            if (SchedFlags != ThreadSchedState.Running)
            {
                System.CriticalSectionLock.Unlock();

                System.Scheduler.ContextSwitch();

                return;
            }

            int core = CurrentCore;

            _schedulingData.TransferToCore(DynamicPriority, -1, this);

            KThread selectedThread = null;

            if (!_schedulingData.ScheduledThreads(core).Any())
                foreach (KThread thread in _schedulingData.SuggestedThreads(core))
                {
                    if (thread.CurrentCore < 0) continue;

                    KThread firstCandidate = _schedulingData.ScheduledThreads(thread.CurrentCore).FirstOrDefault();

                    if (firstCandidate == thread) continue;

                    if (firstCandidate == null || firstCandidate.DynamicPriority >= 2)
                    {
                        _schedulingData.TransferToCore(thread.DynamicPriority, core, thread);

                        selectedThread = thread;
                    }

                    break;
                }

            if (selectedThread != this) _scheduler.ThreadReselectionRequested = true;

            System.CriticalSectionLock.Unlock();

            System.Scheduler.ContextSwitch();
        }

        public void SetPriority(int priority)
        {
            System.CriticalSectionLock.Lock();

            BasePriority = priority;

            UpdatePriorityInheritance();

            System.CriticalSectionLock.Unlock();
        }

        public long SetActivity(bool pause)
        {
            long result = 0;

            System.CriticalSectionLock.Lock();

            ThreadSchedState lowNibble = SchedFlags & ThreadSchedState.LowNibbleMask;

            if (lowNibble != ThreadSchedState.Paused && lowNibble != ThreadSchedState.Running)
            {
                System.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
            }

            System.CriticalSectionLock.Lock();

            if (!ShallBeTerminated && SchedFlags != ThreadSchedState.TerminationPending)
            {
                if (pause)
                {
                    //Pause, the force pause flag should be clear (thread is NOT paused).
                    if ((_forcePauseFlags & ThreadSchedState.ForcePauseFlag) == 0)
                    {
                        _forcePauseFlags |= ThreadSchedState.ForcePauseFlag;

                        CombineForcePauseFlags();
                    }
                    else
                    {
                        result = MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
                    }
                }
                else
                {
                    //Unpause, the force pause flag should be set (thread is paused).
                    if ((_forcePauseFlags & ThreadSchedState.ForcePauseFlag) != 0)
                    {
                        ThreadSchedState oldForcePauseFlags = _forcePauseFlags;

                        _forcePauseFlags &= ~ThreadSchedState.ForcePauseFlag;

                        if ((oldForcePauseFlags & ~ThreadSchedState.ForcePauseFlag) == ThreadSchedState.None)
                        {
                            ThreadSchedState oldSchedFlags = SchedFlags;

                            SchedFlags &= ThreadSchedState.LowNibbleMask;

                            AdjustScheduling(oldSchedFlags);
                        }
                    }
                    else
                    {
                        result = MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
                    }
                }
            }

            System.CriticalSectionLock.Unlock();
            System.CriticalSectionLock.Unlock();

            return result;
        }

        public void CancelSynchronization()
        {
            System.CriticalSectionLock.Lock();

            if ((SchedFlags & ThreadSchedState.LowNibbleMask) != ThreadSchedState.Paused || !WaitingSync)
            {
                SyncCancelled = true;
            }
            else if (_withholderNode != null)
            {
                System.Withholders.Remove(_withholderNode);

                SetNewSchedFlags(ThreadSchedState.Running);

                _withholderNode = null;

                SyncCancelled = true;
            }
            else
            {
                SignaledObj   = null;
                ObjSyncResult = (int)MakeError(ErrorModule.Kernel, KernelErr.Cancelled);

                SetNewSchedFlags(ThreadSchedState.Running);

                SyncCancelled = false;
            }

            System.CriticalSectionLock.Unlock();
        }

        public long SetCoreAndAffinityMask(int newCore, long newAffinityMask)
        {
            System.CriticalSectionLock.Lock();

            bool useOverride = _affinityOverrideCount != 0;

            //The value -3 is "do not change the preferred core".
            if (newCore == -3)
            {
                newCore = useOverride ? _preferredCoreOverride : PreferredCore;

                if ((newAffinityMask & (1 << newCore)) == 0)
                {
                    System.CriticalSectionLock.Unlock();

                    return MakeError(ErrorModule.Kernel, KernelErr.InvalidMaskValue);
                }
            }

            if (useOverride)
            {
                _preferredCoreOverride = newCore;
                _affinityMaskOverride  = newAffinityMask;
            }
            else
            {
                long oldAffinityMask = AffinityMask;

                PreferredCore = newCore;
                AffinityMask  = newAffinityMask;

                if (oldAffinityMask != newAffinityMask)
                {
                    int oldCore = CurrentCore;

                    if (CurrentCore >= 0 && ((AffinityMask >> CurrentCore) & 1) == 0)
                    {
                        if (PreferredCore < 0)
                            CurrentCore = HighestSetCore(AffinityMask);
                        else
                            CurrentCore = PreferredCore;
                    }

                    AdjustSchedulingForNewAffinity(oldAffinityMask, oldCore);
                }
            }

            System.CriticalSectionLock.Unlock();

            return 0;
        }

        private static int HighestSetCore(long mask)
        {
            for (int core = KScheduler.CpuCoresCount - 1; core >= 0; core--)
                if (((mask >> core) & 1) != 0) return core;

            return -1;
        }

        private void CombineForcePauseFlags()
        {
            ThreadSchedState oldFlags  = SchedFlags;
            ThreadSchedState lowNibble = SchedFlags & ThreadSchedState.LowNibbleMask;

            SchedFlags = lowNibble | _forcePauseFlags;

            AdjustScheduling(oldFlags);
        }

        private void SetNewSchedFlags(ThreadSchedState newFlags)
        {
            System.CriticalSectionLock.Lock();

            ThreadSchedState oldFlags = SchedFlags;

            SchedFlags = (oldFlags & ThreadSchedState.HighNibbleMask) | newFlags;

            if ((oldFlags & ThreadSchedState.LowNibbleMask) != newFlags) AdjustScheduling(oldFlags);

            System.CriticalSectionLock.Unlock();
        }

        public void ReleaseAndResume()
        {
            System.CriticalSectionLock.Lock();

            if ((SchedFlags & ThreadSchedState.LowNibbleMask) == ThreadSchedState.Paused)
            {
                if (_withholderNode != null)
                {
                    System.Withholders.Remove(_withholderNode);

                    SetNewSchedFlags(ThreadSchedState.Running);

                    _withholderNode = null;
                }
                else
                {
                    SetNewSchedFlags(ThreadSchedState.Running);
                }
            }

            System.CriticalSectionLock.Unlock();
        }

        public void Reschedule(ThreadSchedState newFlags)
        {
            System.CriticalSectionLock.Lock();

            ThreadSchedState oldFlags = SchedFlags;

            SchedFlags = (oldFlags & ThreadSchedState.HighNibbleMask) |
                         (newFlags & ThreadSchedState.LowNibbleMask);

            AdjustScheduling(oldFlags);

            System.CriticalSectionLock.Unlock();
        }

        public void AddMutexWaiter(KThread requester)
        {
            AddToMutexWaitersList(requester);

            requester.MutexOwner = this;

            UpdatePriorityInheritance();
        }

        public void RemoveMutexWaiter(KThread thread)
        {
            if (thread._mutexWaiterNode?.List != null) _mutexWaiters.Remove(thread._mutexWaiterNode);

            thread.MutexOwner = null;

            UpdatePriorityInheritance();
        }

        public KThread RelinquishMutex(long mutexAddress, out int count)
        {
            count = 0;

            if (_mutexWaiters.First == null) return null;

            KThread newMutexOwner = null;

            LinkedListNode<KThread> currentNode = _mutexWaiters.First;

            do
            {
                //Skip all threads that are not waiting for this mutex.
                while (currentNode != null && currentNode.Value.MutexAddress != mutexAddress) currentNode = currentNode.Next;

                if (currentNode == null) break;

                LinkedListNode<KThread> nextNode = currentNode.Next;

                _mutexWaiters.Remove(currentNode);

                currentNode.Value.MutexOwner = newMutexOwner;

                if (newMutexOwner != null)
                    newMutexOwner.AddToMutexWaitersList(currentNode.Value);
                else
                    newMutexOwner = currentNode.Value;

                count++;

                currentNode = nextNode;
            }
            while (currentNode != null);

            if (newMutexOwner != null)
            {
                UpdatePriorityInheritance();

                newMutexOwner.UpdatePriorityInheritance();
            }

            return newMutexOwner;
        }

        private void UpdatePriorityInheritance()
        {
            //If any of the threads waiting for the mutex has
            //higher priority than the current thread, then
            //the current thread inherits that priority.
            int highestPriority = BasePriority;

            if (_mutexWaiters.First != null)
            {
                int waitingDynamicPriority = _mutexWaiters.First.Value.DynamicPriority;

                if (waitingDynamicPriority < highestPriority) highestPriority = waitingDynamicPriority;
            }

            if (highestPriority != DynamicPriority)
            {
                int oldPriority = DynamicPriority;

                DynamicPriority = highestPriority;

                AdjustSchedulingForNewPriority(oldPriority);

                if (MutexOwner != null)
                {
                    //Remove and re-insert to ensure proper sorting based on new priority.
                    MutexOwner._mutexWaiters.Remove(_mutexWaiterNode);

                    MutexOwner.AddToMutexWaitersList(this);

                    MutexOwner.UpdatePriorityInheritance();
                }
            }
        }

        private void AddToMutexWaitersList(KThread thread)
        {
            LinkedListNode<KThread> nextPrio = _mutexWaiters.First;

            int currentPriority = thread.DynamicPriority;

            while (nextPrio != null && nextPrio.Value.DynamicPriority <= currentPriority) nextPrio = nextPrio.Next;

            if (nextPrio != null)
                thread._mutexWaiterNode = _mutexWaiters.AddBefore(nextPrio, thread);
            else
                thread._mutexWaiterNode = _mutexWaiters.AddLast(thread);
        }

        private void AdjustScheduling(ThreadSchedState oldFlags)
        {
            if (oldFlags == SchedFlags) return;

            if (oldFlags == ThreadSchedState.Running)
            {
                //Was running, now it's stopped.
                if (CurrentCore >= 0) _schedulingData.Unschedule(DynamicPriority, CurrentCore, this);

                for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                    if (core != CurrentCore && ((AffinityMask >> core) & 1) != 0) _schedulingData.Unsuggest(DynamicPriority, core, this);
            }
            else if (SchedFlags == ThreadSchedState.Running)
            {
                //Was stopped, now it's running.
                if (CurrentCore >= 0) _schedulingData.Schedule(DynamicPriority, CurrentCore, this);

                for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                    if (core != CurrentCore && ((AffinityMask >> core) & 1) != 0) _schedulingData.Suggest(DynamicPriority, core, this);
            }

            _scheduler.ThreadReselectionRequested = true;
        }

        private void AdjustSchedulingForNewPriority(int oldPriority)
        {
            if (SchedFlags != ThreadSchedState.Running) return;

            //Remove thread from the old priority queues.
            if (CurrentCore >= 0) _schedulingData.Unschedule(oldPriority, CurrentCore, this);

            for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                if (core != CurrentCore && ((AffinityMask >> core) & 1) != 0) _schedulingData.Unsuggest(oldPriority, core, this);

            //Add thread to the new priority queues.
            KThread currentThread = _scheduler.GetCurrentThread();

            if (CurrentCore >= 0)
            {
                if (currentThread == this)
                    _schedulingData.SchedulePrepend(DynamicPriority, CurrentCore, this);
                else
                    _schedulingData.Schedule(DynamicPriority, CurrentCore, this);
            }

            for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                if (core != CurrentCore && ((AffinityMask >> core) & 1) != 0) _schedulingData.Suggest(DynamicPriority, core, this);

            _scheduler.ThreadReselectionRequested = true;
        }

        private void AdjustSchedulingForNewAffinity(long oldAffinityMask, int oldCore)
        {
            if (SchedFlags != ThreadSchedState.Running || DynamicPriority >= KScheduler.PrioritiesCount) return;

            //Remove from old queues.
            for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                if (((oldAffinityMask >> core) & 1) != 0)
                {
                    if (core == oldCore)
                        _schedulingData.Unschedule(DynamicPriority, core, this);
                    else
                        _schedulingData.Unsuggest(DynamicPriority, core, this);
                }

            //Insert on new queues.
            for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                if (((AffinityMask >> core) & 1) != 0)
                {
                    if (core == CurrentCore)
                        _schedulingData.Schedule(DynamicPriority, core, this);
                    else
                        _schedulingData.Suggest(DynamicPriority, core, this);
                }

            _scheduler.ThreadReselectionRequested = true;
        }

        public override bool IsSignaled()
        {
            return _hasExited;
        }

        public void ClearExclusive()
        {
            Owner.Memory.ClearExclusive(CurrentCore);
        }

        public void TimeUp()
        {
            System.CriticalSectionLock.Lock();

            SetNewSchedFlags(ThreadSchedState.Running);

            System.CriticalSectionLock.Unlock();
        }
    }
}