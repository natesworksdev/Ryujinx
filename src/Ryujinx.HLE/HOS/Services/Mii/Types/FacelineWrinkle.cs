using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum FacelineWrinkle : byte
    {
        None,
        TearTroughs,
        FacialPain,
        Cheeks,
        Folds,
        UnderTheEyes,
        SplitChin,
        Chin,
        BrowDroop,
        MouthFrown,
        CrowsFeet,
        FoldsCrowsFrown,

        Min = 0,
        Max = 11
    }
}
