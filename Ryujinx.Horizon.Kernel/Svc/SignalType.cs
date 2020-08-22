namespace Ryujinx.Horizon.Kernel.Svc
{
    public enum SignalType
    {
        Signal = 0,
        SignalAndIncrementIfEqual = 1,
        SignalAndModifyIfEqual = 2
    }
}
