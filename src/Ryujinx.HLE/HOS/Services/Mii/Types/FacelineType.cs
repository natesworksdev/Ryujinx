using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum FacelineType : byte
    {
        Sharp,
        Rounded,
        SharpRounded,
        SharpRoundedSmall,
        Large,
        LargeRounded,
        SharpSmall,
        Flat,
        Bump,
        Angular,
        FlatRounded,
        AngularSmall,

        Min = 0,
        Max = 11
    }
}
