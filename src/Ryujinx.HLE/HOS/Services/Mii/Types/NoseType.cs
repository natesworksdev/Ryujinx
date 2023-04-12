using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
    enum NoseType : byte
    {
        Normal,
        Rounded,
        Dot,
        Arrow,
        Roman,
        Triangle,
        Button,
        RoundedInverted,
        Potato,
        Grecian,
        Snub,
        Aquiline,
        ArrowLeft,
        RoundedLarge,
        Hooked,
        Fat,
        Droopy,
        ArrowLarge,

        Min = 0,
        Max = 17
    }
}
