namespace Ryujinx.HLE.Loaders
{
    enum ElfSymType
    {
        SttNotype  = 0,
        SttObject  = 1,
        SttFunc    = 2,
        SttSection = 3,
        SttFile    = 4,
        SttCommon  = 5,
        SttTls     = 6
    }
}