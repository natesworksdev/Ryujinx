namespace Ryujinx.HLE.HOS.Services.Nv.NvHostCtrl
{
    internal struct NvHostCtrlSyncptWait
    {
        public int Id;
        public int Thresh;
        public int Timeout;
    }
}