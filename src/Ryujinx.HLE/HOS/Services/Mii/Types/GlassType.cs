using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum GlassType : byte
    {
        None,
        Oval,
        Wayfarer,
        Rectangle,
        TopRimless,
        Rounded,
        Oversized,
        CatEye,
        Square,
        BottomRimless,
        SemiOpaqueRounded,
        SemiOpaqueCatEye,
        SemiOpaqueOval,
        SemiOpaqueRectangle,
        SemiOpaqueAviator,
        OpaqueRounded,
        OpaqueCatEye,
        OpaqueOval,
        OpaqueRectangle,
        OpaqueAviator,

        Min = 0,
        Max = 19
    }
}
