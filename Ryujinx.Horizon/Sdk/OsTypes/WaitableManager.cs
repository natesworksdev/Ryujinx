using Ryujinx.Horizon.Sdk.OsTypes.Impl;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class WaitableManager
    {
        private readonly WaitableManagerImpl _impl;

        public WaitableManager()
        {
            _impl = new WaitableManagerImpl();
        }

        public void LinkWaitableHolder(WaitableHolderBase waitableHolder)
        {
            DebugUtil.Assert(!waitableHolder.IsLinkedToManager);

            _impl.LinkWaitableHolder(waitableHolder);

            waitableHolder.SetManager(_impl);
        }

        public void MoveAllFrom(WaitableManager other)
        {
            _impl.MoveAllFrom(other._impl);
        }

        public WaitableHolder WaitAny()
        {
            return (WaitableHolder)_impl.WaitAnyImpl(true, -1L);
        }

        public WaitableHolder TryWaitAny()
        {
            return (WaitableHolder)_impl.WaitAnyImpl(false, 0);
        }

        public WaitableHolder TimedWaitAny(long timeout)
        {
            return (WaitableHolder)_impl.WaitAnyImpl(false, timeout);
        }
    }
}
