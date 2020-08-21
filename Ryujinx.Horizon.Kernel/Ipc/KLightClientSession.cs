using Ryujinx.Horizon.Kernel.Common;

namespace Ryujinx.Horizon.Kernel.Ipc
{
    class KLightClientSession : KAutoObject
    {
        private readonly KLightSession _parent;

        public KLightClientSession(KernelContextInternal context, KLightSession parent) : base(context)
        {
            _parent = parent;
        }
    }
}