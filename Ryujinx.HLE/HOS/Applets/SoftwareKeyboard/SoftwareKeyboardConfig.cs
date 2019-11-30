using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// 
    /// </summary>
    internal enum KeyboardMode : uint
    {
        /// <summary>
        /// Normal keyboard.
        /// </summary>
        Default,

        /// <summary>
        /// Number pad. The buttons at the bottom left/right are only available when they're set in the config by leftButtonText / rightButtonText.
        /// </summary>
        NumbersOnly,

        /// <summary>
        /// QWERTY (and variants) keyboard only.
        /// </summary>
        LettersOnly
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum InvalidCharFlags : uint
    {
        None = 0 << 1,

        Space = 1 << 1,

        AtSymbol = 1 << 2,

        Percent = 1 << 3,

        ForwardSlash = 1 << 4,

        BackSlash = 1 << 5,

        Numbers = 1 << 6,

        DownloadCode = 1 << 7,

        Username = 1 << 8

    }

    /// <summary>
    /// 
    /// </summary>
    internal enum PasswordMode : uint
    {
        /// <summary>
        /// 
        /// </summary>
        Disabled,

        /// <summary>
        /// 
        /// </summary>
        Enabled
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum InputFormMode : uint
    {
        /// <summary>
        /// 
        /// </summary>
        SingleLine,

        /// <summary>
        /// 
        /// </summary>
        MultiLine
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum InitialCursorPosition : uint
    {
        /// <summary>
        /// 
        /// </summary>
        Start,

        /// <summary>
        /// 
        /// </summary>
        End
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SoftwareKeyboardConfig
    {
        /// <summary>
        /// 
        /// </summary>
        const int SubmitTextLength = 8;
        
        /// <summary>
        /// 
        /// </summary>
        const int HeaderTextLength = 64;
        
        /// <summary>
        /// 
        /// </summary>
        const int SubtitleTextLength = 128;
        
        /// <summary>
        /// 
        /// </summary>
        const int GuideTextLength = 256;

        /// <summary>
        /// Type of keyboard.
        /// </summary>
        public KeyboardMode Mode;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SubmitTextLength + 1)]
        public string SubmitText;

        /// <summary>
        /// 
        /// </summary>
        public char LeftOptionalSymbolKey;

        /// <summary>
        /// 
        /// </summary>
        public char RightOptionalSymbolKey;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool PredictionEnabled;

        /// <summary>
        /// 
        /// </summary>
        public InvalidCharFlags InvalidCharFlag;

        /// <summary>
        /// 
        /// </summary>
        public InitialCursorPosition InitialCursorPosition;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HeaderTextLength + 1)]
        public string HeaderText;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SubtitleTextLength + 1)]
        public string SubtitleText;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = GuideTextLength + 1)]
        public string GuideText;

        /// <summary>
        /// When non-zero, specifies the max string length. When the input is too long, swkbd will stop accepting more input until text is deleted via the B button (Backspace).
        /// </summary>
        public int StringLengthMax;

        /// <summary>
        /// When non-zero, specifies the minimum string length.
        /// </summary>
        public int StringLengthMin;

        /// <summary>
        /// 
        /// </summary>
        public PasswordMode PasswordMode;

        /// <summary>
        /// 
        /// </summary>
        public InputFormMode InputFormMode;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseNewLine;

        /// <summary>
        /// When set, the software keyboard will return a string UTF-8 encoded, rather than UTF-16.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseUtf8;

        /// <summary>
        /// 
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseBlurBackground;

        /// <summary>
        /// 
        /// </summary>
        public int InitialStringOffset;

        /// <summary>
        /// 
        /// </summary>
        public int InitialStringLength;

        /// <summary>
        /// 
        /// </summary>
        public int CustomDictionaryOffset;

        /// <summary>
        /// 
        /// </summary>
        public int CustomDictionaryCount;

        /// <summary>
        /// When set, the application will validate the entered text whilst the swkbd is still on screen.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool CheckText;
    }
}
