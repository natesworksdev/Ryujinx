namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    enum ScanFilterFlag
    {
        LocalCommunicationId = 1 << 0,
        SessionId            = 1 << 1,
        NetworkType          = 1 << 2,
        MacAddress           = 1 << 3,
        Ssid                 = 1 << 4,
        SceneId              = 1 << 5,
        IntentId             = LocalCommunicationId | SceneId,
        NetworkId            = IntentId | SessionId
    }
}