namespace Ryujinx.HLE.HOS.Services.Bcat
{
    enum ResultCode
    {
        ModuleId       = 122,
        ErrorCodeShift = 9,

        Success = 0,

        InvalidArgument        = (1 << ErrorCodeShift) | ModuleId,
        NullArgument           = (2 << ErrorCodeShift) | ModuleId,
        ObjectInUse            = (3 << ErrorCodeShift) | ModuleId,
        TargetAlreadyMounted   = (4 << ErrorCodeShift) | ModuleId,
        TargetNotMounted       = (5 << ErrorCodeShift) | ModuleId,
        ObjectAlreadyOpened    = (6 << ErrorCodeShift) | ModuleId,
        ObjectNotOpened        = (7 << ErrorCodeShift) | ModuleId,
        InternetRequestDenied  = (8 << ErrorCodeShift) | ModuleId,
        NullSaveData           = (31 << ErrorCodeShift) | ModuleId,
        PassphraseNotFound     = (80 << ErrorCodeShift) | ModuleId,
        DataVerificationFailed = (81 << ErrorCodeShift) | ModuleId,
        InvalidAPICall         = (90 << ErrorCodeShift) | ModuleId,
        NullObject             = (91 << ErrorCodeShift) | ModuleId,
        InvalidOperation       = (98 << ErrorCodeShift) | ModuleId,
    }
}
