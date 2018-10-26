namespace Ryujinx.HLE.HOS.Services.Sfdnsres
{
    internal enum NetDBError
    {
        Internal = -1,
        Success,
        HostNotFound,
        TryAgain,
        NoRecovery,
        NoData,
        NoAddress = NoData,
    }
}
