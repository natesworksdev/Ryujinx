using System;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KCoreContext
    {
        private KScheduler _scheduler;

        private HleCoreManager _coreManager;

        public bool ContextSwitchNeeded { get; private set; }

        public KThread CurrentThread  { get; private set; }
        public KThread SelectedThread { get; private set; }

        public KCoreContext(KScheduler scheduler, HleCoreManager coreManager)
        {
            _scheduler   = scheduler;
            _coreManager = coreManager;
        }

        public void SelectThread(KThread thread)
        {
            SelectedThread = thread;

            if (thread != null)
            {
                thread.LastScheduledTicks = (uint)Environment.TickCount;
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
                _coreManager.GetThread(CurrentThread.Context.Work).Reset();
            }

            CurrentThread = SelectedThread;

            if (CurrentThread != null)
            {
                CurrentThread.ClearExclusive();

                _coreManager.GetThread(CurrentThread.Context.Work).Set();

                CurrentThread.Context.Execute();
            }
        }
    }
}