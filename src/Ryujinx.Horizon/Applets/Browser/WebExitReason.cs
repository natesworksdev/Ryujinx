namespace Ryujinx.Horizon.Applets.Browser
{
    public enum WebExitReason : uint
    {
        ExitButton,
        BackButton,
        Requested,
        LastUrl,
        ErrorDialog = 7,
    }
}
