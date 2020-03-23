namespace Ryujinx.HLE.HOS.Services.Hid
{
    [System.Flags]
    public enum HidControllerConnectionState : long
    {
        ControllerStateConnected = (1 << 0),
        ControllerStateWired = (1 << 1),
        JoyLeftConnected = (1 << 2),
        JoyRightConnected = (1 << 4)
    }
}