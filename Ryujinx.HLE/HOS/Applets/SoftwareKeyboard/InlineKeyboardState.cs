namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Possible states for the keyboard when running in inline mode.
    /// </summary>
    enum InlineKeyboardState : uint
    {
        Uninitialized = 0x0,
        Initialized   = 0x1,
        Ready         = 0x2,
        DataAvailable = 0x3,
        Complete      = 0x4
    }
}
