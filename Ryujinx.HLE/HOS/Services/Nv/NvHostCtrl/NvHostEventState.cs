namespace Ryujinx.HLE.HOS.Services.Nv.NvHostCtrl
{
    internal enum NvHostEventState
    {
        Registered = 0,
        Waiting    = 1,
        Busy       = 2,
        Free       = 5
    }
}