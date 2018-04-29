namespace Ryujinx.Core.OsHle.Services.Nv
{
    struct NvHostCtrlSyncPtWait
    {
        public int Id;
        public int Thresh;
        public int Timeout;
    }
}