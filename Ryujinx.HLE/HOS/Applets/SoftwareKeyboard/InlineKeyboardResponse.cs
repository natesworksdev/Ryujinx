namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Possible responses from the software keyboard when running in inline mode.
    /// </summary>
    enum InlineKeyboardResponse : uint
    {
        /// <summary>
        /// The software keyboard received a Calc and it is fully initialized. Reply data is ignored by the user-process.
        /// </summary>
        FinishedInitialize,

        /// <summary>
        /// Default response. Official sw has no handling for this besides just closing the storage.
        /// </summary>
        Default,

        /// <summary>
        /// The text data in the software keyboard changed (UTF-16 encoding).
        /// </summary>
        ChangedString,

        /// <summary>
        /// The cursor position in the software keyboard changed (UTF-16 encoding).
        /// </summary>
        MovedCursor,

        /// <summary>
        /// A tab in the software keyboard changed.
        /// </summary>
        MovedTab,

        /// <summary>
        /// The OK key was pressed in the software keyboard, confirming the input text (UTF-16 encoding).
        /// </summary>
        DecidedEnter,

        /// <summary>
        /// The Cancel key was pressed in the software keyboard, cancelling the input.
        /// </summary>
        DecidedCancel,

        /// <summary>
        /// Same as ChangedString, but with UTF-8 encoding.
        /// </summary>
        ChangedStringUtf8,

        /// <summary>
        /// Same as MovedCursor, but with UTF-8 encoding.
        /// </summary>
        MovedCursorUtf8,

        /// <summary>
        /// Same as DecidedEnter, but with UTF-8 encoding.
        /// </summary>
        DecidedEnterUtf8,

        /// <summary>
        /// They software keyboard is releasing the data previously set by a SetCustomizeDic request.
        /// </summary>
        UnsetCustomizeDic,

        /// <summary>
        /// They software keyboard is releasing the data previously set by a SetUserWordInfo request.
        /// </summary>
        ReleasedUserWordInfo,

        /// <summary>
        /// They software keyboard is releasing the data previously set by a SetCustomizedDictionaries request.
        /// </summary>
        UnsetCustomizedDictionaries,

        /// <summary>
        /// Same as ChangedString, but with additional fields.
        /// </summary>
        ChangedStringV2,

        /// <summary>
        /// Same as MovedCursor, but with additional fields.
        /// </summary>
        MovedCursorV2,

        /// <summary>
        /// Same as ChangedStringUtf8, but with additional fields.
        /// </summary>
        ChangedStringUtf8V2,

        /// <summary>
        /// Same as MovedCursorUtf8, but with additional fields.
        /// </summary>
        MovedCursorUtf8V2
    }
}