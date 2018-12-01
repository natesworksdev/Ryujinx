namespace Ryujinx.HLE.Input
{
    internal interface IHidDevice
    {
        long Offset    { get; }
        bool Connected { get; }
    }
}
