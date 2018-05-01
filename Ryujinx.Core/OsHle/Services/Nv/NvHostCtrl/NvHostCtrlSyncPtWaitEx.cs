namespace Ryujinx.Core.OsHle.Services.Nv
{
    struct NvHostCtrlSyncPtWaitEx
    {
        public int Id;
        public int Thresh;
        public int Timeout;
        public int Value;
    }
}