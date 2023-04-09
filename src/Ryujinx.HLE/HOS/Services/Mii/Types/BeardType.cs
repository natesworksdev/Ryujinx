using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
    enum BeardType : byte
    {
        None,
        Goatee,
        GoateeLong,
        LionsManeLong,
        LionsMane,
        Full,

        Min = 0,
        Max = 5
    }
}
