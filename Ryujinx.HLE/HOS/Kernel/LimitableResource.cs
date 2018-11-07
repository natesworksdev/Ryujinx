namespace Ryujinx.HLE.HOS.Kernel
{
    enum LimitableResource : byte
    {
        Memory,
        Thread,
        Event,
        TransferMemory,
        Session
    }
}