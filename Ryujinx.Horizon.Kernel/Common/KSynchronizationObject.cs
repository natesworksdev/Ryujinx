using Ryujinx.Horizon.Kernel.Threading;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Kernel.Common
{
    class KSynchronizationObject : KAutoObject
    {
        public LinkedList<KThread> WaitingThreads { get; }

        public KSynchronizationObject(KernelContextInternal context) : base(context)
        {
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
            KernelContext.Synchronization.SignalObject(this);
        }

        public virtual bool IsSignaled()
        {
            return false;
        }
    }
}