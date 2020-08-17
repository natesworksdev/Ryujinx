using Ryujinx.HLE.HOS.Kernel.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    struct SignalableEvent
    {
        private readonly KWritableEvent _event;
        internal SignalableEvent(KWritableEvent writableEvent) => _event = writableEvent;
        public void Signal() => _event.Signal();
    }
}
