using ChocolArm64.Memory;
using ChocolArm64.State;
using Ryujinx.HLE.OsHle.Handles;

using static Ryujinx.HLE.OsHle.ErrorCode;

namespace Ryujinx.HLE.OsHle.Kernel
{
    static class AddressArbiter
    {
        static ulong WaitForAddress(Process Process, AThreadState ThreadState, long Address, ulong Timeout)
        {
            KThread CurrentThread = Process.GetThread(ThreadState.Tpidr);

            Process.Scheduler.SetReschedule(CurrentThread.ProcessorId);

            CurrentThread.ArbiterWaitAddress = Address;
            CurrentThread.ArbiterSignaled    = false;

            Process.Scheduler.EnterWait(CurrentThread, NsTimeConverter.GetTimeMs(Timeout));

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
            int CurrentValue = Memory.ReadInt32(Address);

            if (CurrentValue < Value)
            {
                if (ShouldDecrement)
                {
                    Memory.WriteInt32(Address, CurrentValue - 1);
                }
            }
            else
            {
                return MakeError(ErrorModule.Kernel, KernelErr.InvalidState);
            }

            if (Timeout == 0)
            {
                return MakeError(ErrorModule.Kernel, KernelErr.Timeout);
            }

            return WaitForAddress(Process, ThreadState, Address, Timeout);
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
