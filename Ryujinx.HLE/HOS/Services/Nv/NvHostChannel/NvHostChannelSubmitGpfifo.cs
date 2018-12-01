namespace Ryujinx.HLE.HOS.Services.Nv.NvHostChannel
{
    internal struct NvHostChannelSubmitGpfifo
    {
        public long Address;
        public int  NumEntries;
        public int  Flags;
        public int  SyncptId;
        public int  SyncptValue;
    }
}