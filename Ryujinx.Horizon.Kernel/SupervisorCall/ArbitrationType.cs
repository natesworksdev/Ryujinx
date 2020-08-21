namespace Ryujinx.Horizon.Kernel.SupervisorCall
{
    public enum ArbitrationType
    {
        WaitIfLessThan             = 0,
        DecrementAndWaitIfLessThan = 1,
        WaitIfEqual                = 2
    }
}