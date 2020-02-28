namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    public enum Gender : byte
    {
        Male,
        Female,
        // No non binary option..? :/
        All,

        Min = 0,
        Max = 1
    }
}
