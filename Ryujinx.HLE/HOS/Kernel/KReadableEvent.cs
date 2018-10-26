namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KReadableEvent : KSynchronizationObject
    {
        private KEvent _parent;

        private bool _signaled;

        public KReadableEvent(Horizon system, KEvent parent) : base(system)
        {
            _parent = parent;
        }

        public override void Signal()
        {
            System.CriticalSectionLock.Lock();

            if (!_signaled)
            {
                _signaled = true;

                base.Signal();
            }

            System.CriticalSectionLock.Unlock();
        }

        public KernelResult Clear()
        {
            _signaled = false;

            return KernelResult.Success;
        }

        public KernelResult ClearIfSignaled()
        {
            KernelResult result;

            System.CriticalSectionLock.Lock();

            if (_signaled)
            {
                _signaled = false;

                result = KernelResult.Success;
            }
            else
            {
                result = KernelResult.InvalidState;
            }

            System.CriticalSectionLock.Unlock();

            return result;
        }

        public override bool IsSignaled()
        {
            return _signaled;
        }
    }
}