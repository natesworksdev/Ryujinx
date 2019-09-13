namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl.Types
{
    struct NvHostCtrlSyncptWaitEx
    {
        public int Id;
        public int Thresh;
        public int Timeout;
        public int Value;
    }
}