using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Kernel
{
    public sealed class KernelContext : IDisposable
    {
        private readonly KernelContextInternal _internal;
        public KernelContext(MemoryBlock memory)
        {
            _internal = new KernelContextInternal(memory);
        }

        internal KernelContextInternal GetInternal()
        {
            return _internal;
        }

        public void EnableMultiCoreScheduling()
        {
            _internal.Scheduler.MultiCoreScheduling = true;
        }

        public void DisableMultiCoreScheduling()
        {
            _internal.Scheduler.MultiCoreScheduling = false;
        }

        public void Dispose()
        {
            _internal.Dispose();
        }
    }
}
