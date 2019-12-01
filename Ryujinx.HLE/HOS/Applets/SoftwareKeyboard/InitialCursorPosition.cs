namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Identifies the initial position of the cursor displayed in the area.
    /// </summary>
    internal enum InitialCursorPosition : uint
    {
        /// <summary>
        /// Position the cursor at the beginning of the text
        /// </summary>
        Start,

        /// <summary>
        /// Position the cursor at the end of the text
        /// </summary>
        End
    }
}
