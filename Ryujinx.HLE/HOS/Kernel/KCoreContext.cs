using Ryujinx.Common;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KCoreContext
    {
        private KScheduler Scheduler;

        private HleCoreManager CoreManager;

        public bool ContextSwitchNeeded { get; private set; }

        public KThread CurrentThread  { get; private set; }
        public KThread SelectedThread { get; private set; }

        public KCoreContext(KScheduler Scheduler, HleCoreManager CoreManager)
        {
            this.Scheduler   = Scheduler;
            this.CoreManager = CoreManager;
        }

        public void SelectThread(KThread Thread)
        {
            SelectedThread = Thread;

            if (Thread != null)
            {
                Thread.LastScheduledTicks = PerformanceCounter.ElapsedMilliseconds;
            }

            if (SelectedThread != CurrentThread)
            {
                ContextSwitchNeeded = true;
            }
        }

        public void UpdateCurrentThread()
        {
            ContextSwitchNeeded = false;

            CurrentThread = SelectedThread;
        }

        public void ContextSwitch()
        {
            ContextSwitchNeeded = false;

            if (CurrentThread != null)
            {
                CoreManager.Reset(CurrentThread.Context.Work);
            }

            CurrentThread = SelectedThread;

            if (CurrentThread != null)
            {
                CurrentThread.ClearExclusive();

                CoreManager.Set(CurrentThread.Context.Work);

                CurrentThread.Context.Execute();
            }
        }
    }
}