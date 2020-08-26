using Ryujinx.Horizon.Common;
using System.Threading;

namespace Ryujinx.Horizon.Kernel.Common
{
    class KAutoObject
    {
        protected KernelContextInternal KernelContext;

        private int _referenceCount;

        public KAutoObject(KernelContextInternal context)
        {
            KernelContext = context;

            _referenceCount = 1;
        }

        public virtual Result SetName(string name)
        {
            if (!KernelContext.AutoObjectNames.TryAdd(name, this))
            {
                return KernelResult.InvalidState;
            }

            return Result.Success;
        }

        public static Result RemoveName(KernelContextInternal context, string name)
        {
            if (!context.AutoObjectNames.TryRemove(name, out _))
            {
                return KernelResult.NotFound;
            }

            return Result.Success;
        }

        public static KAutoObject FindNamedObject(KernelContextInternal context, string name)
        {
            if (context.AutoObjectNames.TryGetValue(name, out KAutoObject obj))
            {
                return obj;
            }

            return null;
        }

        public void IncrementReferenceCount()
        {
            Interlocked.Increment(ref _referenceCount);
        }

        public void DecrementReferenceCount()
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                Destroy();
            }
        }

        protected virtual void Destroy() { }
    }
}