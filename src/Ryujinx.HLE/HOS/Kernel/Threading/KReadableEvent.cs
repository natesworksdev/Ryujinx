using Ryujinx.HLE.HOS.Kernel.Common;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KReadableEvent : KSynchronizationObject
    {
        private readonly KEvent _parent;
        private readonly object _lock = new();
        private TaskCompletionSource _tcs = new ();

        public KReadableEvent(KernelContext context, KEvent parent) : base(context)
        {
            _parent = parent;
        }
    }
}
