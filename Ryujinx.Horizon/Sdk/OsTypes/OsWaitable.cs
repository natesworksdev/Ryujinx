namespace Ryujinx.Horizon.Sdk.OsTypes
{
    public static partial class Os
    {
        internal static void FinalizeWaitableHolder(WaitableHolderBase holder)
        {
            DebugUtil.Assert(!holder.IsLinkedToManager);
        }
    }
}
