namespace Ryujinx.HLE.HOS.Kernel
{
    internal enum ArbitrationType
    {
        WaitIfLessThan             = 0,
        DecrementAndWaitIfLessThan = 1,
        WaitIfEqual                = 2
    }
}