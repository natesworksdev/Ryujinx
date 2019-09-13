namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl.Types
{
    struct NvHostCtrlSyncptWait
    {
        public int Id;
        public int Thresh;
        public int Timeout;
    }
}