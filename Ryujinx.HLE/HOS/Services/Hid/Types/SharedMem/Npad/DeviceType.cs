namespace Ryujinx.HLE.HOS.Services.Hid
{
    [System.Flags]
    public enum DeviceType : int
    {
        FullKey = 1 << 0,
        HandheldLeft = 1 << 2,
        HandheldRight = 1 << 3,
        JoyLeft = 1 << 4,
        JoyRight = 1 << 5,
        Palma = 1 << 6, // Poké Ball Plus
        GenericExternal = 1 << 15,
        Generic = 1 << 31
    }
}