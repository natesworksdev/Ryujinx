namespace Ryujinx.Horizon.Kernel.Threading
{
    class KEvent
    {
        public KReadableEvent ReadableEvent { get; private set; }
        public KWritableEvent WritableEvent { get; private set; }

        public KEvent(KernelContextInternal context)
        {
            ReadableEvent = new KReadableEvent(context, this);
            WritableEvent = new KWritableEvent(context, this);
        }
    }
}