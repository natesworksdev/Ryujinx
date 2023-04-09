using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
    enum Gender : byte
    {
        Male,
        Female,
        All,

        Min = 0,
        Max = 1
    }
}
