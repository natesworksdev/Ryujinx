namespace Ryujinx.HLE.HOS.Services.Pctl
{
    enum ResultCode
    {
        ModuleId       = 142,
        ErrorCodeShift = 9,

        Success = 0,

        FreeCommunicationDisabled = (101 << ErrorCodeShift) | ModuleId,
        InvalidPid                = (131 << ErrorCodeShift) | ModuleId,
        InvalidUnknownFlag        = (133 << ErrorCodeShift) | ModuleId
    }
}
