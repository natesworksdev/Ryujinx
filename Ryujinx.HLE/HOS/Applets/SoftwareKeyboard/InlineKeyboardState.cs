namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Possible states for the software keyboard when running in inline mode.
    /// </summary>
    enum InlineKeyboardState : uint
    {
        /// <summary>
        /// The software keyboard has just been created or finalized and is uninitialized.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// The software keyboard is initialized, but it is not visible and not processing input.
        /// </summary>
        Initialized,

        /// <summary>
        /// The software keyboard is transitioning to a visible state.
        /// </summary>
        Appearing,

        /// <summary>
        /// The software keyboard is visible and receiving processing input.
        /// </summary>
        Shown,

        /// <summary>
        /// software keyboard is transitioning to a hidden state because the user pressed either OK or Cancel.
        /// </summary>
        Disappearing
    }
}