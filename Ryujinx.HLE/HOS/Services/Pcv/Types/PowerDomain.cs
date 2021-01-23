namespace Ryujinx.HLE.HOS.Services.Pcv
{
    enum PowerDomain
    {
        // 0x3A000080 - SoC (1.125v)
        Max77620_Sd0  =  0,
        // 0x3A000081 - Dram (1.1v)
        Max77620_Sd1  =  1,
        // 0x3A000082 - Ldo0, Ldo1, Ldo7, Ldo8 (1.325v)
        Max77620_Sd2  =  2,
        // 0x3A000083 - Reserved (1.8v)
        Max77620_Sd3  =  3,
        // 0x3A0000A0 - Panel (1.2v)
        Max77620_Ldo0 =  4,
        // 0x3A0000A1 - Xusb, PCIe (1.05v)
        Max77620_Ldo1 =  5,
        // 0x3A0000A2 - SdCard (1.8v, 3.3v)
        Max77620_Ldo2 =  6,
        // 0x3A0000A3 - GcAsic (3.1v)
        Max77620_Ldo3 =  7,
        // 0x3A0000A4 - Rtc (0.85v)
        Max77620_Ldo4 =  8,
        // 0x3A0000A5 - GcCard (1.8v)
        Max77620_Ldo5 =  9,
        // 0x3A0000A6 - TouchPanel, ALS (2.9v)
        Max77620_Ldo6 = 10,
        // 0x3A0000A7 - Xusb (1.05v)
        Max77620_Ldo7 = 11,
        // 0x3A0000A8 - DisplayPort, HDMI, SioMcu (1.05v)
        Max77620_Ldo8 = 12,
        // 0x3A000003
        Max77621_Cpu  = 13,
        // 0x3A000004
        Max77621_Gpu  = 14,
        // 0x3A000003
        Max77812_Cpu  = 15,
        // 0x3A000004
        Max77812_Gpu  = 16,
        // 0x3A000005
        Max77812_Dram = 17
    }
}