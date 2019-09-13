namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl.Types
{
    class NvHostEvent
    {
        public int Id;
        public int Thresh;

        public NvHostEventState State;
    }
}