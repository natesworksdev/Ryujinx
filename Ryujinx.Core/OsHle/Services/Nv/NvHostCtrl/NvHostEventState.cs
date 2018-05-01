namespace Ryujinx.Core.OsHle.Services.Nv
{
    enum NvHostEventState
    {
        Registered = 0,
        Waiting    = 1,
        Busy       = 2,
        Free       = 5
    }
}