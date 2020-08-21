using Ryujinx.Horizon.Kernel.Common;

namespace Ryujinx.Horizon.Kernel.Ipc
{
    class KLightServerSession : KAutoObject
    {
        private readonly KLightSession _parent;

        public KLightServerSession(KernelContextInternal context, KLightSession parent) : base(context)
        {
            _parent = parent;
        }
    }
}