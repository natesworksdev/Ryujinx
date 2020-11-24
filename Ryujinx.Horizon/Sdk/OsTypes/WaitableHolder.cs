namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class WaitableHolder : WaitableHolderBase
    {
        public object UserData { get; set; }

        public void UnlinkFromWaitableManager()
        {
            DebugUtil.Assert(IsLinkedToManager);

            Manager.UnlinkWaitableHolder(this);

            SetManager(null);
        }
    }
}
