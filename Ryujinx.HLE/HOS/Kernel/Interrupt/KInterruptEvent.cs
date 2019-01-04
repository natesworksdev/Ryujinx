using Ryujinx.HLE.HOS.Kernel.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Interrupt
{
    class KInterruptEvent
    {
        public KReadableEvent Event { get; }

        public int  IrqId   { get; private set; }
        public bool Enabled { get; private set; }

        public KInterruptEvent(Horizon system)
        {
            Event = new KReadableEvent(system);

            IrqId = -1;
        }
    }
}