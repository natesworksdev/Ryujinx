namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Identifies the variant of keyboard displayed on screen.
    /// </summary>
    enum KeyboardMode : uint
    {
        /// <summary>
        /// A full alpha-numeric keyboard.
        /// </summary>
        Default,

        /// <summary>
        /// Number pad.
        /// </summary>
        NumbersOnly,

        /// <summary>
        /// ASCII characters keyboard.
        /// </summary>
        ASCII,

        FullLatin,
        Alphabet,
        SimplifiedChinese,
        TraditionalChinese,
        Korean,
        LanguageSet2,
        LanguageSet2Latin,
    }
}