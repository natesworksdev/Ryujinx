namespace Ryujinx.HLE.HOS.Services.Ldn.Spacemeowx2Ldn
{
    internal enum LanPacketType : byte
    {
        Scan,
        ScanResponse,
        Connect,
        SyncNetwork
    }
}