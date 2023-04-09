using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
    enum EyebrowType : byte
    {
        FlatAngledLarge,
        LowArchRoundedThin,
        SoftAngledLarge,
        MediumArchRoundedThin,
        RoundedMedium,
        LowArchMedium,
        RoundedThin,
        UpThin,
        MediumArchRoundedMedium,
        RoundedLarge,
        UpLarge,
        FlatAngledLargeInverted,
        MediumArchFlat,
        AngledThin,
        HorizontalLarge,
        HighArchFlat,
        Flat,
        MediumArchLarge,
        LowArchThin,
        RoundedThinInverted,
        HighArchLarge,
        Hairy,
        Dotted,
        None,

        Min = 0,
        Max = 23
    }
}
