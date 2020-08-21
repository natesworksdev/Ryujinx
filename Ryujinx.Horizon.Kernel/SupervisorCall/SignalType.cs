namespace Ryujinx.Horizon.Kernel.SupervisorCall
{
    public enum SignalType
    {
        Signal                    = 0,
        SignalAndIncrementIfEqual = 1,
        SignalAndModifyIfEqual    = 2
    }
}
