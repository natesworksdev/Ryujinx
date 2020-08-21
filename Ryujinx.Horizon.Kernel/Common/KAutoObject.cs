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

        public virtual KernelResult SetName(string name)
        {
            if (!KernelContext.AutoObjectNames.TryAdd(name, this))
            {
                return KernelResult.InvalidState;
            }

            return KernelResult.Success;
        }

        public static KernelResult RemoveName(KernelContextInternal context, string name)
        {
            if (!context.AutoObjectNames.TryRemove(name, out _))
            {
                return KernelResult.NotFound;
            }

            return KernelResult.Success;
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