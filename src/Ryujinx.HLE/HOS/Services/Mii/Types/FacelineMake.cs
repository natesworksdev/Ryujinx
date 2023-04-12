using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum FacelineMake : byte
    {
        None,
        CheekPorcelain,
        CheekNatural,
        EyeShadowBlue,
        CheekBlushPorcelain,
        CheekBlushNatural,
        CheekPorcelainEyeShadowBlue,
        CheekPorcelainEyeShadowNatural,
        CheekBlushPorcelainEyeShadowEspresso,
        Freckles,
        LionsManeBeard,
        StubbleBeard,

        Min = 0,
        Max = 11
    }
}
