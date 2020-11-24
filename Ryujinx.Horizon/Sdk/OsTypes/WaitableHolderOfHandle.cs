namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class WaitableHolderOfHandle : WaitableHolder
    {
        private int _handle;

        public override int Handle => _handle;

        public WaitableHolderOfHandle(int handle)
        {
            _handle = handle;
        }
    }
}
