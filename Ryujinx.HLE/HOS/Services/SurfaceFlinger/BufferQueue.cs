using Ryujinx.HLE.HOS.Kernel.Process;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferQueue
    {
        public static void CreateBufferQueue(Switch device, KProcess process, out BufferQueueProducer produer, out BufferQueueConsumer consumer)
        {
            BufferQueueCore core = new BufferQueueCore(device, process);

            produer  = new BufferQueueProducer(core);
            consumer = new BufferQueueConsumer(core);
        }
    }
}
