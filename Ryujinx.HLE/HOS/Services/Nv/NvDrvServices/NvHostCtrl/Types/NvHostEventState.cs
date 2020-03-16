namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    enum NvHostEventState
    {
        Availaible = 0,
        Waiting    = 1,
        Cancelling = 2,
        Signaling  = 3,
        Signaled   = 4,
        Cancelled  = 5
    }
}