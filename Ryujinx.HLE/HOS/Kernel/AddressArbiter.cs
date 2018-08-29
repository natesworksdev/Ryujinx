using ChocolArm64.Memory;
using ChocolArm64.State;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    static class AddressArbiter
    {
        static ulong WaitForAddress(Process Process, AThreadState ThreadState, long Address, ulong Timeout)
        {
            //TODO: Update.
            KThread CurrentThread = Process.GetThread(ThreadState.Tpidr);

            CurrentThread.ArbiterWaitAddress = Address;
            CurrentThread.ArbiterSignaled    = false;

            if (!CurrentThread.ArbiterSignaled)
            {
                return MakeError(ErrorModule.Kernel, KernelErr.Timeout);
            }

            return 0;
        }

        public static ulong WaitForAddressIfLessThan(Process      Process,
                                                     AThreadState ThreadState,
                                                     AMemory      Memory,
                                                     long         Address,
                                                     int          Value,
                                                     ulong        Timeout,
                                                     bool         ShouldDecrement)
        {
            return 0;
        }

        public static ulong WaitForAddressIfEqual(Process      Process,
                                                  AThreadState ThreadState,
                                                  AMemory      Memory,
                                                  long         Address,
                                                  int          Value,
                                                  ulong        Timeout)
        {
            if (Memory.ReadInt32(Address) != Value)
            {
                return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
            }

            if (Timeout == 0)
            {
                return MakeError(ErrorModule.Kernel, KernelErr.Timeout);
            }

            return WaitForAddress(Process, ThreadState, Address, Timeout);
        }
    }

    enum ArbitrationType : int
    {
        WaitIfLessThan,
        DecrementAndWaitIfLessThan,
        WaitIfEqual
    }

    enum SignalType : int
    {
        Signal,
        IncrementAndSignalIfEqual,
        ModifyByWaitingCountAndSignalIfEqual
    }
}
