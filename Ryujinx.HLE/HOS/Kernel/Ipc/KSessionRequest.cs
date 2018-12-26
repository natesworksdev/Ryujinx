using Ryujinx.HLE.HOS.Kernel.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KSessionRequest
    {
        public KBufferDescriptorTable BufferDescriptorTable { get; }

        public KThread SenderThread { get; }

        public KWritableEvent AsyncEvent { get; }

        public ulong CustomCmdBuffAddr { get; }
        public ulong CustomCmdBuffSize { get; }

        public KSessionRequest(
            KThread senderThread,
            ulong   customCmdBuffAddr,
            ulong   customCmdBuffSize)
        {
            SenderThread      = senderThread;
            CustomCmdBuffAddr = customCmdBuffAddr;
            CustomCmdBuffSize = customCmdBuffSize;

            BufferDescriptorTable = new KBufferDescriptorTable();
        }
    }
}