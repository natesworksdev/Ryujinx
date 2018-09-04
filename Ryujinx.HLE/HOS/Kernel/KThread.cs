using ChocolArm64;
using System.Collections.Generic;
using System.Linq;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KThread : KSynchronizationObject, IKFutureSchedulerObject
    {
        public AThread Thread { get; private set; }

        public long AffinityMask { get; set; }

        public int ThreadId { get; private set; }

        public KSynchronizationObject SignaledObj;

        public long CondVarAddress     { get; set; }
        public long MutexAddress       { get; set; }
        public long ArbiterWaitAddress { get; set; }

        public Process Owner { get; private set; }

        private long LastScheduledTicks;

        private long YieldCount;

        public bool ArbiterSignaled { get; set; }

        public LinkedListNode<KThread>[] SiblingsPerCore { get; private set; }

        private LinkedListNode<KThread> Withholder;

        private LinkedList<KThread>     MutexWaiters;
        private LinkedListNode<KThread> MutexWaiterNode;

        public KThread MutexOwner { get; private set; }

        public int ThreadHandleForUserMutex { get; set; }

        private ThreadSchedState ForcePauseFlags;

        public int ObjSyncResult { get; set; }

        public int DynamicPriority { get; set; }
        public int CurrentCore     { get; set; }
        public int BasePriority    { get; set; }
        public int PreferredCore   { get; set; }

        private long AffinityMaskOverride;
        private int  PreferredCoreOverride;
        private int  AffinityOverrideCount;

        public ThreadSchedState SchedFlags { get; private set; }

        public bool ShallBeTerminated { get; private set; }

        public bool SyncCancelled  { get; set; }
        public bool WaitingSync    { get; set; }

        private bool HasExited;

        private KScheduler Scheduler;

        private KSchedulingData SchedulingData;

        public long LastPc { get; set; }

        public KThread(
            AThread Thread,
            Process Process,
            Horizon System,
            int     ProcessorId,
            int     Priority,
            int     ThreadId) : base(System)
        {
            this.Thread   = Thread;
            this.ThreadId = ThreadId;

            Owner          = Process;
            PreferredCore  = ProcessorId;
            Scheduler      = System.Scheduler;
            SchedulingData = System.Scheduler.SchedulingData;

            SiblingsPerCore = new LinkedListNode<KThread>[KScheduler.CpuCoresCount];

            MutexWaiters = new LinkedList<KThread>();

            AffinityMask = 1 << ProcessorId;

            DynamicPriority = BasePriority = Priority;

            CurrentCore = PreferredCore;
        }

        public long Start()
        {
            long Result = MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);

            System.CriticalSectionLock.Lock();

            if (!ShallBeTerminated)
            {
                KThread CurrentThread = System.Scheduler.GetCurrentThread();

                while (SchedFlags               != ThreadSchedState.TerminationPending &&
                       CurrentThread.SchedFlags != ThreadSchedState.TerminationPending &&
                       !CurrentThread.ShallBeTerminated)
                {
                    if ((SchedFlags & ThreadSchedState.LowNibbleMask) != ThreadSchedState.None)
                    {
                        Result = MakeError(ErrorModule.Kernel, KernelErr.InvalidState);

                        break;
                    }

                    if (CurrentThread.ForcePauseFlags == ThreadSchedState.None)
                    {
                        if (Owner != null && ForcePauseFlags != ThreadSchedState.None)
                        {
                            CombineForcePauseFlags();
                        }

                        SetNewSchedFlags(ThreadSchedState.Running);

                        Result = 0;

                        break;
                    }
                    else
                    {
                        CurrentThread.CombineForcePauseFlags();

                        System.CriticalSectionLock.Unlock();
                        System.CriticalSectionLock.Lock();

                        if (CurrentThread.ShallBeTerminated)
                        {
                            break;
                        }
                    }
                }
            }

            System.CriticalSectionLock.Unlock();

            return Result;
        }

        public void Exit()
        {
            System.CriticalSectionLock.Lock();

            ForcePauseFlags &= ~ThreadSchedState.ExceptionalMask;

            ExitImpl();

            System.CriticalSectionLock.Unlock();
        }

        private void ExitImpl()
        {
            System.CriticalSectionLock.Lock();

            SetNewSchedFlags(ThreadSchedState.TerminationPending);

            HasExited = true;

            Signal();

            System.CriticalSectionLock.Unlock();
        }

        public long Sleep(long Timeout)
        {
            System.CriticalSectionLock.Lock();

            if (ShallBeTerminated || SchedFlags == ThreadSchedState.TerminationPending)
            {
                System.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.ThreadTerminating);
            }

            SetNewSchedFlags(ThreadSchedState.Paused);

            if (Timeout >= 1)
            {
                System.TimeManager.ScheduleFutureInvocation(this, Timeout);
            }

            System.CriticalSectionLock.Unlock();

            System.TimeManager.UnscheduleFutureInvocation(this);

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

            if (DynamicPriority < KScheduler.PrioritiesCount)
            {
                //Move current thread to the end of the queue.
                SchedulingData.Reschedule(DynamicPriority, CurrentCore, this);
            }

            Scheduler.ThreadReselectionRequested = true;

            System.CriticalSectionLock.Unlock();

            System.Scheduler.ContextSwitch();
        }

        public void YieldWithLoadBalancing()
        {
            if (YieldCount == Owner.YieldCounter)
            {
                return;
            }

            int Prio = DynamicPriority;
            int Core = CurrentCore;

            System.CriticalSectionLock.Lock();

            if (SchedFlags != ThreadSchedState.Running)
            {
                System.CriticalSectionLock.Unlock();

                System.Scheduler.ContextSwitch();

                return;
            }

            KThread NextThreadOnCurrentQueue = null;

            if (DynamicPriority < KScheduler.PrioritiesCount)
            {
                //Move current thread to the end of the queue.
                SchedulingData.Reschedule(Prio, Core, this);

                NextThreadOnCurrentQueue = SchedulingData.ScheduledThreadsPerPrioPerCore[Prio][Core].First?.Value;
            }

            if (Owner != null)
            {
                Owner.YieldCounter++;
            }

            foreach (KThread Thread in SchedulingData.SuggestedThreads(Core))
            {
                KThread SelectedOnCandidateCore = null;

                if (Thread.CurrentCore >= 0)
                {
                    SelectedOnCandidateCore = Scheduler.CoreContexts[Thread.CurrentCore].SelectedThread;
                }

                if (Thread == SelectedOnCandidateCore)
                {
                    continue;
                }

                if (this != NextThreadOnCurrentQueue)
                {
                    //If the candidate has lower priority than the current thread,
                    //or the priority is the same, but it was already scheduled
                    //after the next thread on the queue, then it's not worth to run
                    //it, as the next thread on the queue is already a better candidate.
                    if (Thread.DynamicPriority > DynamicPriority ||
                        (Thread.DynamicPriority == DynamicPriority &&
                        Thread.LastScheduledTicks > NextThreadOnCurrentQueue.LastScheduledTicks))
                    {
                        break;
                    }
                }
                else if (Thread.DynamicPriority > DynamicPriority)
                {
                    break;
                }

                if (SelectedOnCandidateCore == null || SelectedOnCandidateCore.DynamicPriority >= 2)
                {
                    SchedulingData.MoveTo(Thread.DynamicPriority, Core, Thread);

                    Scheduler.ThreadReselectionRequested = true;

                    if (Thread.Owner != null)
                    {
                        Thread.Owner.YieldCounter++;
                    }

                    break;
                }
            }

            if (this != NextThreadOnCurrentQueue)
            {
                Scheduler.ThreadReselectionRequested = true;
            }
            else
            {
                YieldCount = Owner.YieldCounter;
            }

            System.CriticalSectionLock.Unlock();

            System.Scheduler.ContextSwitch();
        }

        public void YieldWithForcedLoadBalancing()
        {
            if (YieldCount == Owner.YieldCounter)
            {
                return;
            }

            System.CriticalSectionLock.Lock();

            if (SchedFlags != ThreadSchedState.Running)
            {
                System.CriticalSectionLock.Unlock();

                System.Scheduler.ContextSwitch();

                return;
            }

            int Core = CurrentCore;

            CurrentCore = -1;

            if (Core >= 0)
            {
                SchedulingData.Unschedule(DynamicPriority, Core, this);
                SchedulingData.Suggest(DynamicPriority, Core, this);
            }

            if (Owner != null)
            {
                Owner.YieldCounter++;
            }

            KThread FirstScheduled = SchedulingData.ScheduledThreads(Core).FirstOrDefault();
            KThread FirstSuggested = SchedulingData.SuggestedThreads(Core).FirstOrDefault();

            if (FirstScheduled != null || FirstSuggested == null)
            {
                Scheduler.ThreadReselectionRequested = true;
            }
            else
            {
                foreach (KThread Thread in SchedulingData.SuggestedThreads(Core))
                {
                    if (Thread.CurrentCore >= 0)
                    {
                        KThread FirstCandidate = SchedulingData.SuggestedThreads(Thread.CurrentCore).FirstOrDefault();

                        if (FirstCandidate != Thread)
                        {
                            if (FirstCandidate == null || FirstCandidate.DynamicPriority >= 2)
                            {
                                SchedulingData.MoveTo(Thread.DynamicPriority, Core, Thread);

                                if (Thread.Owner != null)
                                {
                                    Thread.Owner.YieldCounter++;
                                }

                                if (Thread != this)
                                {
                                    Scheduler.ThreadReselectionRequested = true;
                                }
                                else
                                {
                                    YieldCount = Owner.YieldCounter;
                                }
                            }

                            break;
                        }
                    }
                }
            }

            System.CriticalSectionLock.Unlock();

            System.Scheduler.ContextSwitch();
        }

        public void SetPriority(int Priority)
        {
            System.CriticalSectionLock.Lock();

            BasePriority = Priority;

            UpdatePriorityInheritance();

            System.CriticalSectionLock.Unlock();
        }

        public long SetActivity(bool Active)
        {
            System.CriticalSectionLock.Lock();

            if (SchedFlags == ThreadSchedState.TerminationPending ||
                (SchedFlags & ThreadSchedState.HighNibbleMask) != 0)
            {
                System.CriticalSectionLock.Unlock();

                return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
            }

            System.CriticalSectionLock.Lock();

            if (!ShallBeTerminated && SchedFlags != ThreadSchedState.TerminationPending)
            {
                if (Active)
                {
                    ForcePauseFlags &= ~ThreadSchedState.ForcePauseFlag;

                    UncombineForcePauseFlags();
                }
                else
                {
                    ForcePauseFlags |= ThreadSchedState.ForcePauseFlag;

                    CombineForcePauseFlags();
                }
            }

            System.CriticalSectionLock.Unlock();
            System.CriticalSectionLock.Unlock();

            return 0;
        }

        public void CancelSynchronization()
        {
            System.CriticalSectionLock.Lock();

            if ((SchedFlags & ThreadSchedState.LowNibbleMask) != ThreadSchedState.Paused || !WaitingSync)
            {
                SyncCancelled = true;
            }
            else if (Withholder != null)
            {
                Withholder.List.Remove(Withholder);

                SetNewSchedFlags(ThreadSchedState.Running);

                Withholder = null;

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

        public long SetCoreAndAffinityMask(int NewCore, long NewAffinityMask)
        {
            System.CriticalSectionLock.Lock();

            bool UseOverride = AffinityOverrideCount != 0;

            //The value -3 is "do not change the preferred core".
            if (NewCore == -3)
            {
                NewCore = UseOverride ? PreferredCoreOverride : PreferredCore;

                if ((NewAffinityMask & (1 << NewCore)) == 0)
                {
                    System.CriticalSectionLock.Unlock();

                    return MakeError(ErrorModule.Kernel, KernelErr.InvalidMaskValue);
                }
            }

            if (UseOverride)
            {
                PreferredCoreOverride = NewCore;
                AffinityMaskOverride  = NewAffinityMask;
            }
            else
            {
                long OldAffinityMask = AffinityMask;

                PreferredCore = NewCore;
                AffinityMask  = NewAffinityMask;

                if (OldAffinityMask != NewAffinityMask)
                {
                    int OldCore = CurrentCore;

                    if (CurrentCore >= 0 && ((AffinityMask >> CurrentCore) & 1) == 0)
                    {
                        if (PreferredCore < 0)
                        {
                            CurrentCore = 63 - CountLeadingZeros(AffinityMask);
                        }
                        else
                        {
                            CurrentCore = PreferredCore;
                        }
                    }

                    AdjustSchedulingForNewAffinity(OldAffinityMask, OldCore);
                }
            }

            System.CriticalSectionLock.Unlock();

            return 0;
        }

        private static int CountLeadingZeros(long Value)
        {
            for (int Bit = 0; Bit < 64; Bit++)
            {
                if (((Value >> (63 - Bit)) & 1) != 0)
                {
                    return Bit;
                }
            }

            return 64;
        }

        private void CombineForcePauseFlags()
        {
            ThreadSchedState OldStat   = SchedFlags;
            ThreadSchedState LowNibble = SchedFlags & ThreadSchedState.LowNibbleMask;

            SchedFlags = LowNibble | ForcePauseFlags;

            AdjustScheduling(OldStat);
        }

        private void UncombineForcePauseFlags()
        {
            ThreadSchedState OldStat   = SchedFlags;
            ThreadSchedState LowNibble = SchedFlags & ThreadSchedState.LowNibbleMask;

            SchedFlags = LowNibble;

            if (ForcePauseFlags != ThreadSchedState.None)
            {
                AdjustScheduling(OldStat);
            }
        }

        private void SetNewSchedFlags(ThreadSchedState NewFlags)
        {
            System.CriticalSectionLock.Lock();

            ThreadSchedState OldStat = SchedFlags;

            SchedFlags = (OldStat & ThreadSchedState.HighNibbleMask) | NewFlags;

            AdjustScheduling(OldStat);

            System.CriticalSectionLock.Unlock();
        }

        public void ReleaseAndResume()
        {
            System.CriticalSectionLock.Lock();

            if ((SchedFlags & ThreadSchedState.LowNibbleMask) == ThreadSchedState.Paused)
            {
                //TODO: withholder stuff.

                if (false)
                {
                    SetNewSchedFlags(ThreadSchedState.Running);
                }
                else
                {
                    SetNewSchedFlags(ThreadSchedState.Running);
                }
            }

            System.CriticalSectionLock.Unlock();
        }

        public void Reschedule(ThreadSchedState NewFlags)
        {
            System.CriticalSectionLock.Lock();

            ThreadSchedState OldFlags = SchedFlags;

            SchedFlags = (OldFlags & ThreadSchedState.HighNibbleMask) |
                         (NewFlags & ThreadSchedState.LowNibbleMask);

            AdjustScheduling(OldFlags);

            System.CriticalSectionLock.Unlock();
        }

        public void AddMutexWaiter(KThread Requester)
        {
            AddToMutexWaitersList(Requester);

            Requester.MutexOwner = this;

            UpdatePriorityInheritance();
        }

        public void RemoveMutexWaiter(KThread Thread)
        {
            if (Thread.MutexWaiterNode?.List != null)
            {
                MutexWaiters.Remove(Thread.MutexWaiterNode);
            }

            Thread.MutexOwner = null;

            UpdatePriorityInheritance();
        }

        public KThread RelinquishMutex(long MutexAddress, out int Count)
        {
            Count = 0;

            if (MutexWaiters.First == null)
            {
                return null;
            }

            KThread NewMutexOwner = null;

            LinkedListNode<KThread> CurrentNode = MutexWaiters.First;

            do
            {
                //Skip all threads that are not waiting for this mutex.
                while (CurrentNode != null && CurrentNode.Value.MutexAddress != MutexAddress)
                {
                    CurrentNode = CurrentNode.Next;
                }

                if (CurrentNode == null)
                {
                    break;
                }

                LinkedListNode<KThread> NextNode = CurrentNode.Next;

                MutexWaiters.Remove(CurrentNode);

                CurrentNode.Value.MutexOwner = NewMutexOwner;

                if (NewMutexOwner != null)
                {
                    //New owner was already selected, re-insert on new owner list.
                    NewMutexOwner.AddToMutexWaitersList(CurrentNode.Value);
                }
                else
                {
                    //New owner not selected yet, use current thread.
                    NewMutexOwner = CurrentNode.Value;
                }

                Count++;

                CurrentNode = NextNode;
            }
            while (CurrentNode != null);

            if (NewMutexOwner != null)
            {
                UpdatePriorityInheritance();

                KThread CurrOwner = NewMutexOwner;

                while (CurrOwner != null)
                {
                    CurrOwner.UpdatePriorityInheritance();

                    CurrOwner = CurrOwner.MutexOwner;
                }
            }

            return NewMutexOwner;
        }

        private void UpdatePriorityInheritance()
        {
            //If any of the threads waiting for the mutex has
            //higher priority than the current thread, then
            //the current thread inherits that priority.
            int HighestPriority = BasePriority;

            if (MutexWaiters.First != null)
            {
                int WaitingDynamicPriority = MutexWaiters.First.Value.DynamicPriority;

                if (WaitingDynamicPriority < HighestPriority)
                {
                    HighestPriority = WaitingDynamicPriority;
                }
            }

            if (HighestPriority != DynamicPriority)
            {
                int OldPriority = DynamicPriority;

                DynamicPriority = HighestPriority;

                AdjustSchedulingForNewPriority(OldPriority);

                if (MutexOwner != null)
                {
                    //Remove and re-insert to ensure proper sorting based on new priority.
                    MutexOwner.MutexWaiters.Remove(MutexWaiterNode);

                    MutexOwner.AddToMutexWaitersList(this);
                }
            }
        }

        private void AddToMutexWaitersList(KThread Thread)
        {
            LinkedListNode<KThread> NextPrio = MutexWaiters.First;

            int CurrentPriority = Thread.DynamicPriority;

            while (NextPrio != null && NextPrio.Value.DynamicPriority <= CurrentPriority)
            {
                NextPrio = NextPrio.Next;
            }

            if (NextPrio != null)
            {
                Thread.MutexWaiterNode = MutexWaiters.AddBefore(NextPrio, Thread);
            }
            else
            {
                Thread.MutexWaiterNode = MutexWaiters.AddLast(Thread);
            }
        }

        private void AdjustScheduling(ThreadSchedState OldFlags)
        {
            if (OldFlags == SchedFlags)
            {
                return;
            }

            if (OldFlags == ThreadSchedState.Running)
            {
                //Was running, now it's stopped.
                if (CurrentCore >= 0)
                {
                    SchedulingData.Unschedule(DynamicPriority, CurrentCore, this);
                }

                for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
                {
                    if (Core != CurrentCore && ((AffinityMask >> Core) & 1) != 0)
                    {
                        SchedulingData.Unsuggest(DynamicPriority, Core, this);
                    }
                }
            }
            else if (SchedFlags == ThreadSchedState.Running)
            {
                //Was stopped, now it's running.
                if (CurrentCore >= 0)
                {
                    SchedulingData.Schedule(DynamicPriority, CurrentCore, this);
                }

                for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
                {
                    if (Core != CurrentCore && ((AffinityMask >> Core) & 1) != 0)
                    {
                        SchedulingData.Suggest(DynamicPriority, Core, this);
                    }
                }
            }

            Scheduler.ThreadReselectionRequested = true;
        }

        private void AdjustSchedulingForNewPriority(int OldPriority)
        {
            if (SchedFlags != ThreadSchedState.Running)
            {
                return;
            }

            //Remove thread from the old priority queues.
            if (CurrentCore >= 0)
            {
                SchedulingData.Unschedule(OldPriority, CurrentCore, this);
            }

            for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
            {
                if (Core != CurrentCore && ((AffinityMask >> Core) & 1) != 0)
                {
                    SchedulingData.Unsuggest(OldPriority, Core, this);
                }
            }

            //Add thread to the new priority queues.
            KThread CurrentThread = Scheduler.GetCurrentThread();

            if (CurrentCore >= 0)
            {
                if (CurrentThread == this)
                {
                    SchedulingData.SchedulePrepend(DynamicPriority, CurrentCore, this);
                }
                else
                {
                    SchedulingData.Schedule(DynamicPriority, CurrentCore, this);
                }
            }

            for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
            {
                if (Core != CurrentCore && ((AffinityMask >> Core) & 1) != 0)
                {
                    SchedulingData.Suggest(DynamicPriority, Core, this);
                }
            }

            Scheduler.ThreadReselectionRequested = true;
        }

        private void AdjustSchedulingForNewAffinity(long OldAffinityMask, int OldCore)
        {
            if (SchedFlags != ThreadSchedState.Running || DynamicPriority >= KScheduler.PrioritiesCount)
            {
                return;
            }

            //Remove from old queues.
            for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
            {
                if (((OldAffinityMask >> Core) & 1) != 0)
                {
                    if (Core == OldCore)
                    {
                        SchedulingData.Unschedule(DynamicPriority, Core, this);
                    }
                    else
                    {
                        SchedulingData.Unsuggest(DynamicPriority, Core, this);
                    }
                }
            }

            //Insert on new queues.
            for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
            {
                if (((AffinityMask >> Core) & 1) != 0)
                {
                    if (Core == CurrentCore)
                    {
                        SchedulingData.Schedule(DynamicPriority, Core, this);
                    }
                    else
                    {
                        SchedulingData.Suggest(DynamicPriority, Core, this);
                    }
                }
            }

            Scheduler.ThreadReselectionRequested = true;
        }

        public override bool IsSignaled()
        {
            return HasExited;
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