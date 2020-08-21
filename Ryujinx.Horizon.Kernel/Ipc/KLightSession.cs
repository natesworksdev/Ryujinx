using Ryujinx.Horizon.Kernel.Common;

namespace Ryujinx.Horizon.Kernel.Ipc
{
    class KLightSession : KAutoObject
    {
        public KLightServerSession ServerSession { get; }
        public KLightClientSession ClientSession { get; }

        public KLightSession(KernelContextInternal context) : base(context)
        {
            ServerSession = new KLightServerSession(context, this);
            ClientSession = new KLightClientSession(context, this);
        }
    }
}