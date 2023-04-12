using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum HairFlip : byte
    {
        Left,
        Right,

        Min = 0,
        Max = 1
    }
}
