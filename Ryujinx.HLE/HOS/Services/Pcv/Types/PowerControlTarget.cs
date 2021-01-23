namespace Ryujinx.HLE.HOS.Services.Pcv
{
    enum PowerControlTarget
    {
        // 0x3C000004 - SdCard (Ldo2)
        SdCard = 0,
        // 0x34000007 - DisplayPort, HDMI (Ldo8)
        VideoOutput = 1,
        // Unknwon - Invalid (Ldo7)
        Invalid = 2,
        // 0x3500041A - SioMcu (Ldo8)
        SioMcu = 3
    }
}