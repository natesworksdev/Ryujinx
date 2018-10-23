using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KSynchronizationObject
    {
        public LinkedList<KThread> WaitingThreads;

        protected Horizon System;

        public KSynchronizationObject(Horizon system)
        {
            System = system;

            WaitingThreads = new LinkedList<KThread>();
        }

        public LinkedListNode<KThread> AddWaitingThread(KThread thread)
        {
            return WaitingThreads.AddLast(thread);
        }

        public void RemoveWaitingThread(LinkedListNode<KThread> node)
        {
            WaitingThreads.Remove(node);
        }

        public virtual void Signal()
        {
            System.Synchronization.SignalObject(this);
        }

        public virtual bool IsSignaled()
        {
            return false;
        }
    }
}