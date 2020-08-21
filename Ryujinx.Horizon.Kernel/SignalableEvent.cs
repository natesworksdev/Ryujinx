using Ryujinx.Horizon.Kernel.Threading;

namespace Ryujinx.Horizon.Kernel
{
    public struct SignalableEvent
    {
        private readonly KWritableEvent _event;
        internal SignalableEvent(KWritableEvent writableEvent) => _event = writableEvent;
        public void Signal() => _event?.Signal();
        public void Clear() => _event?.Clear();
    }
}
