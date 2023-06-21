using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum MustacheType : byte
    {
        None,
        Walrus,
        Pencil,
        Horseshoe,
        Normal,
        Toothbrush,

        Min = 0,
        Max = 5
    }
}
