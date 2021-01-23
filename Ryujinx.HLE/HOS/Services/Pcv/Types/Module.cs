namespace Ryujinx.HLE.HOS.Services.Pcv
{
    enum Module
    {
        // 0x40000001 - VddCpu
        Cpu             =  0,
        // 0x40000002 - VddGpu
        Gpu             =  1,
        // 0x40000003 - VddSoc
        I2s1            =  2,
        // 0x40000004 - VddSoc
        I2s2            =  3,
        // 0x40000005 - VddSoc
        I2s3            =  4,
        // 0x40000006 - VddSoc
        Pwm             =  5,
        // 0x02000001 - VddSoc
        I2c1            =  6,
        // 0x02000002 - VddSoc
        I2c2            =  7,
        // 0x02000003 - VddSoc
        I2c3            =  8,
        // 0x02000004 - VddSoc
        I2c4            =  9,
        // 0x02000005 - VddSoc
        I2c5            = 10,
        // 0x02000006 - VddSoc
        I2c6            = 11,
        // 0x07000000 - VddSoc
        Spi1            = 12,
        // 0x07000001 - VddSoc
        Spi2            = 13,
        // 0x07000002 - VddSoc
        Spi3            = 14,
        // 0x07000003 - VddSoc
        Spi4            = 15,
        // 0x40000011 - VddSoc
        Disp1           = 16,
        // 0x40000012 - VddSoc
        Disp2           = 17,
        // 0x40000013 - None
        Isp             = 18,
        // 0x40000014 - None
        Vi              = 19,
        // 0x40000015 - VddSoc
        Sdmmc1          = 20,
        // 0x40000016 - VddSoc
        Sdmmc2          = 21,
        // 0x40000017 - VddSoc
        Sdmmc3          = 22,
        // 0x40000018 - VddSoc
        Sdmmc4          = 23,
        // 0x40000019 - None
        Owr             = 24,
        // 0x4000001A - VddSoc
        Csite           = 25,
        // 0x4000001B - VddSoc
        Tsec            = 26,
        // 0x4000001C - VddSoc
        Mselect         = 27,
        // 0x4000001D - VddSoc
        Hda2codec2x     = 28,
        // 0x4000001E - VddSoc
        Actmon          = 29,
        // 0x4000001F - VddSoc
        I2cSlow         = 30,
        // 0x40000020 - VddSoc
        Sor1            = 31,
        // 0x40000021 - None
        Sata            = 32,
        // 0x40000022 - VddSoc
        Hda             = 33,
        // 0x40000023 - VddSoc
        XusbCoreHostSrc = 34,
        // 0x40000024 - VddSoc
        XusbFalconSrc   = 35,
        // 0x40000025 - VddSoc
        XusbFsSrc       = 36,
        // 0x40000026 - VddSoc
        XusbCoreDevSrc  = 37,
        // 0x40000027 - VddSoc
        XusbSsSrc       = 38,
        // 0x03000001 - VddSoc
        UartA           = 39,
        // 0x35000405 - VddSoc
        UartB           = 40,
        // 0x3500040F - VddSoc
        UartC           = 41,
        // 0x37000001 - VddSoc
        UartD           = 42,
        // 0x4000002C - VddSoc
        Host1x          = 43,
        // 0x4000002D - VddSoc
        Entropy         = 44,
        // 0x4000002E - VddSoc
        SocTherm        = 45,
        // 0x4000002F - VddSoc
        Vic             = 46,
        // 0x40000030 - VddSoc
        Nvenc           = 47,
        // 0x40000031 - VddSoc
        Nvjpg           = 48,
        // 0x40000032 - VddSoc
        Nvdec           = 49,
        // 0x40000033 - VddSoc
        Qspi            = 50,
        // 0x40000034 - None
        Vil2c           = 51,
        // 0x40000035 - VddSoc
        Tsecb           = 52,
        // 0x40000036 - VddSoc
        Ape             = 53,
        // 0x40000037 - VddSoc
        AudioDsp        = 54,
        // 0x40000038 - VddSoc
        AudioUart       = 55,
        // 0x40000039 - VddSoc
        Emc             = 56,
        // 0x4000003A - VddSoc
        Plle            = 57,
        // 0x4000003B - VddSoc
        PlleHwSeq       = 58,
        // 0x4000003C - VddSoc
        Dsi             = 59,
        // 0x4000003D - VddSoc
        Maud            = 60,
        // 0x4000003E - VddSoc
        Dpaux1          = 61,
        // 0x4000003F - VddSoc
        MipiCal         = 62,
        // 0x40000040 - VddSoc
        UartFstMipiCal  = 63,
        // 0x40000041 - VddSoc
        Osc             = 64,
        // 0x40000042 - VddSoc
        SysBus          = 65,
        // 0x40000043 - VddSoc
        SorSafe         = 66,
        // 0x40000044 - VddSoc
        XusbSs          = 67,
        // 0x40000045 - VddSoc
        XusbHost        = 68,
        // 0x40000046 - VddSoc
        XusbDevice      = 69,
        // 0x40000047 - VddSoc
        Extperph1       = 70,
        // 0x40000048 - VddSoc
        Ahub            = 71,
        // 0x40000049 - VddSoc
        Hda2hdmicodec   = 72,
        // 0x4000004A - VddSoc
        Gpuaux          = 73,
        // 0x4000004B - VddSoc
        UsbD            = 74,
        // 0x4000004C - VddSoc
        Usb2            = 75,
        // 0x4000004D - VddSoc
        Pcie            = 76,
        // 0x4000004E - VddSoc
        Afi             = 77,
        // 0x4000004F - VddSoc
        PciExClk        = 78,
        // 0x40000050 - VddSoc
        PExUsbPhy       = 79,
        // 0x40000051 - VddSoc
        XUsbPadCtl      = 80,
        // 0x40000052 - VddSoc
        Apbdma          = 81,
        // 0x40000053 - VddSoc
        Usb2TrkClk      = 82,
        // 0x40000054 - VddSoc
        XUsbIoPll       = 83,
        // 0x40000055 - VddSoc
        XUsbIoPllHwSeq  = 84,
        // 0x40000056 - VddSoc
        Cec             = 85,
        // 0x40000057 - VddSoc
        Extperiph2      = 86,

        // 0x40000080 - None
        // OscClk          = 87??
    }
}