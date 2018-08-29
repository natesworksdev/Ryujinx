using System;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KScheduler
    {
        public const int PrioritiesCount = 64;
        public const int CpuCoresCount   = 4;

        public KSchedulingData SchedulingData { get; private set; }

        private HleCoreManager CoreManager;

        public KCoreContext[] CoreContexts;

        public bool ThreadReselectionRequested;

        public KScheduler()
        {
            SchedulingData = new KSchedulingData();

            CoreManager = new HleCoreManager();

            CoreContexts = new KCoreContext[CpuCoresCount];

            for (int Core = 0; Core < CpuCoresCount; Core++)
            {
                CoreContexts[Core] = new KCoreContext(this, CoreManager);
            }
        }

        public void ContextSwitch()
        {
            lock (CoreContexts)
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

            CoreManager.GetThread(Thread.CurrentThread).Wait();
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
    }
}