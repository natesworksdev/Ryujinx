namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator;

static class LdnConst
{
    public const ulong SsidLengthMax = 32;
    public const ulong AdvertiseDataSizeMax = 384;
    public const ulong UserNameBytesMax = 32;
    public const uint NodeCountMax = 8;
    public const uint StationCountMax = NodeCountMax - 1;
    public const ulong PassphraseLengthMax = 64;
}
