using System;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KScheduler : IDisposable
    {
        public const int PrioritiesCount = 64;
        public const int CpuCoresCount   = 4;

        private int CurrentCore;

        public bool MultiCoreScheduling { get; set; }

        public KSchedulingData SchedulingData { get; private set; }

        private HleCoreManager CoreManager;

        public KCoreContext[] CoreContexts;

        public bool ThreadReselectionRequested;

        private bool KeepPreempting;

        public KScheduler()
        {
            SchedulingData = new KSchedulingData();

            CoreManager = new HleCoreManager();

            CoreContexts = new KCoreContext[CpuCoresCount];

            for (int Core = 0; Core < CpuCoresCount; Core++)
            {
                CoreContexts[Core] = new KCoreContext(this, CoreManager);
            }

            if (!MultiCoreScheduling)
            {
                Thread PreemptionThread = new Thread(PreemptCurrentThread);

                KeepPreempting = true;

                PreemptionThread.Start();
            }
        }

        public void ContextSwitch()
        {
            lock (CoreContexts)
            {
                if (MultiCoreScheduling)
                {
                    int SelectedCount = 0;

                    for (int Core = 0; Core < KScheduler.CpuCoresCount; Core++)
                    {
                        KCoreContext CoreContext = CoreContexts[Core];

                        if (CoreContext.ContextSwitchNeeded && (CoreContext.CurrentThread?.Thread.IsCurrentThread() ?? false))
                        {
                            CoreContext.ContextSwitch();
                        }

                        if (CoreContext.CurrentThread?.Thread.IsCurrentThread() ?? false)
                        {
                            SelectedCount++;
                        }
                    }

                    if (SelectedCount == 0)
                    {
                        CoreManager.GetThread(Thread.CurrentThread).Pause();
                    }
                    else if (SelectedCount == 1)
                    {
                        CoreManager.GetThread(Thread.CurrentThread).Unpause();
                    }
                    else
                    {
                        throw new InvalidOperationException("Thread scheduled in more than one core!");
                    }
                }
                else
                {
                    KThread CurrentThread = CoreContexts[CurrentCore].CurrentThread;

                    bool HasThreadExecuting = CurrentThread != null;

                    if (HasThreadExecuting)
                    {
                        //This is not the thread that is currently executing, we need
                        //to request an interrupt to allow safely starting another thread.
                        if (!CurrentThread.Thread.IsCurrentThread())
                        {
                            CurrentThread.Thread.RequestInterrupt();

                            return;
                        }

                        CoreManager.GetThread(CurrentThread.Thread.Work).Pause();
                    }

                    //Advance current core and try picking a thread,
                    //keep advancing if it is null.
                    for (int Core = 0; Core < 4; Core++)
                    {
                        CurrentCore = (CurrentCore + 1) % CpuCoresCount;

                        KCoreContext CoreContext = CoreContexts[CurrentCore];

                        CoreContext.UpdateCurrentThread();

                        if (CoreContext.CurrentThread != null)
                        {
                            CoreContext.CurrentThread.ClearExclusive();

                            CoreManager.GetThread(CoreContext.CurrentThread.Thread.Work).Unpause();

                            CoreContext.CurrentThread.Thread.Execute();

                            break;
                        }
                    }

                    //If nothing was running before, then we are on a "external"
                    //HLE thread, we don't need to wait.
                    if (!HasThreadExecuting)
                    {
                        return;
                    }
                }
            }

            CoreManager.GetThread(Thread.CurrentThread).Wait();
        }

        private void PreemptCurrentThread()
        {
            //Preempts current thread every 10 milliseconds on a round-robin fashion,
            //when multi core scheduling is disabled, to try ensuring that all threads
            //gets a chance to run.
            while (KeepPreempting)
            {
                lock (CoreContexts)
                {
                    KThread CurrentThread = CoreContexts[CurrentCore].CurrentThread;

                    CurrentThread?.Thread.RequestInterrupt();
                }

                Thread.Sleep(10);
            }
        }

        public void StopThread(KThread Thread)
        {
            Thread.Thread.StopExecution();

            CoreManager.GetThread(Thread.Thread.Work).Unpause();
        }

        public void SelectThreads()
        {
            ThreadReselectionRequested = false;

            KThread[] SelectedThreads = new KThread[CpuCoresCount];

            for (int Core = 0; Core < CpuCoresCount; Core++)
            {
                KThread Thread = null;

                if (!SchedulingData.IsCoreIdle(Core))
                {
                    Thread = SchedulingData.ScheduledThreads(Core).FirstOrDefault();
                }

                SelectedThreads[Core] = Thread;

                CoreContexts[Core].SelectThread(Thread);
            }

            for (int Core = 0; Core < CpuCoresCount; Core++)
            {
                if (!SchedulingData.IsCoreIdle(Core))
                {
                    continue;
                }

                int[] SrcCoresHighestPrioThreads = new int[CpuCoresCount];

                int SrcCoresHighestPrioThreadsCount = 0;

                KThread Dst = null;

                foreach (KThread CurrDst in SchedulingData.SuggestedThreads(Core))
                {
                    if (CurrDst.CurrentCore < 0 || CurrDst != SelectedThreads[CurrDst.CurrentCore])
                    {
                        Dst = CurrDst;

                        break;
                    }

                    SrcCoresHighestPrioThreads[SrcCoresHighestPrioThreadsCount++] = CurrDst.CurrentCore;
                }

                //Not yet selected candidate found.
                if (Dst != null)
                {
                    //Those priorities are used for the kernel message dispatching
                    //threads, we should skip load balancing entirely.
                    if (Dst.DynamicPriority < 2)
                    {
                        break;
                    }

                    SchedulingData.MoveTo(Dst.DynamicPriority, Core, Dst);

                    SelectedThreads[Core] = Dst;

                    CoreContexts[Core].SelectThread(Dst);

                    continue;
                }

                for (int Index = 0; Index < SrcCoresHighestPrioThreadsCount; Index++)
                {
                    int SrcCore = SrcCoresHighestPrioThreads[Index];

                    KThread OrigSelectedCoreSrc = SelectedThreads[SrcCore];

                    KThread Src = SchedulingData.ScheduledThreads(SrcCore).ElementAtOrDefault(1);

                    if (Src != null)
                    {
                        SelectedThreads[SrcCore] = Src;

                        CoreContexts[SrcCore].SelectThread(Src);

                        SchedulingData.MoveTo(OrigSelectedCoreSrc.DynamicPriority, Core, OrigSelectedCoreSrc);

                        SelectedThreads[Core] = OrigSelectedCoreSrc;

                        CoreContexts[Core].SelectThread(OrigSelectedCoreSrc);
                    }
                }
            }
        }

        public KThread GetCurrentThread()
        {
            lock (CoreContexts)
            {
                for (int Core = 0; Core < CpuCoresCount; Core++)
                {
                    if (CoreContexts[Core].CurrentThread?.Thread.IsCurrentThread() ?? false)
                    {
                        return CoreContexts[Core].CurrentThread;
                    }
                }
            }

            throw new InvalidOperationException("Current thread is not scheduled!");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                KeepPreempting = false;
            }
        }
    }
}