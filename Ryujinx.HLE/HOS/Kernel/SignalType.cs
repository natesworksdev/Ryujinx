namespace Ryujinx.HLE.HOS.Kernel
{
    internal enum SignalType
    {
        Signal                    = 0,
        SignalAndIncrementIfEqual = 1,
        SignalAndModifyIfEqual    = 2
    }
}
