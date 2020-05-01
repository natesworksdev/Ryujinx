namespace Ryujinx.HLE.HOS.Applets.Browser
{
    enum WebExitReason : uint
    {
        ExitButton,
        BackButton,
        Requested,
        LastUrl,
        ErrorDialog = 7
    }
}
