namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class MultiWaitHolderOfHandle : MultiWaitHolder
    {
        public override int Handle { get; }

        public MultiWaitHolderOfHandle(int handle)
        {
            Handle = handle;
        }
    }
}