using ChocolArm64.State;
using Ryujinx.HLE.Logging;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    partial class SvcHandler
    {
        private void SvcWaitSynchronization(AThreadState ThreadState)
        {
            long HandlesPtr   = (long)ThreadState.X1;
            int  HandlesCount =  (int)ThreadState.X2;
            long Timeout      = (long)ThreadState.X3;

            Device.Log.PrintDebug(LogClass.KernelSvc,
                "HandlesPtr = 0x"   + HandlesPtr  .ToString("x16") + ", " +
                "HandlesCount = 0x" + HandlesCount.ToString("x8")  + ", " +
                "Timeout = 0x"      + Timeout     .ToString("x16"));

            if ((uint)HandlesCount > 0x40)
            {
                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.CountOutOfRange);

                return;
            }

            KSynchronizationObject[] SyncObjs = new KSynchronizationObject[HandlesCount];

            for (int Index = 0; Index < HandlesCount; Index++)
            {
                int Handle = Memory.ReadInt32(HandlesPtr + Index * 4);

                KSynchronizationObject SyncObj = Process.HandleTable.GetData<KSynchronizationObject>(Handle);

                SyncObjs[Index] = SyncObj;
            }

            int HndIndex = (int)ThreadState.X1;

            ulong High = ThreadState.X1 & (0xffffffffUL << 32);

            long Result = System.Synchronization.WaitFor(SyncObjs, Timeout, ref HndIndex);

            if (Result != 0)
            {
                if (Result == MakeError(ErrorModule.Kernel, KernelErr.Timeout) ||
                    Result == MakeError(ErrorModule.Kernel, KernelErr.Cancelled))
                {
                    Device.Log.PrintDebug(LogClass.KernelSvc, $"Operation failed with error 0x{Result:x}!");
                }
                else
                {
                    Device.Log.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{Result:x}!");
                }
            }

            ThreadState.X0 = (ulong)Result;
            ThreadState.X1 = (uint)HndIndex | High;
        }

        private void SvcCancelSynchronization(AThreadState ThreadState)
        {
            int ThreadHandle = (int)ThreadState.X0;

            Device.Log.PrintDebug(LogClass.KernelSvc, "ThreadHandle = 0x" + ThreadHandle.ToString("x8"));

            KThread Thread = Process.HandleTable.GetData<KThread>(ThreadHandle);

            if (Thread == null)
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Invalid thread handle 0x{ThreadHandle:x8}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                return;
            }

            Thread.CancelSynchronization();

            ThreadState.X0 = 0;
        }

        private void SvcArbitrateLock(AThreadState ThreadState)
        {
            int  OwnerHandle     =  (int)ThreadState.X0;
            long MutexAddress    = (long)ThreadState.X1;
            int  RequesterHandle =  (int)ThreadState.X2;

            Device.Log.PrintDebug(LogClass.KernelSvc,
                "OwnerHandle = 0x"     + OwnerHandle    .ToString("x8")  + ", " +
                "MutexAddress = 0x"    + MutexAddress   .ToString("x16") + ", " +
                "RequesterHandle = 0x" + RequesterHandle.ToString("x8"));

            if (IsPointingInsideKernel(MutexAddress))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Invalid mutex address 0x{MutexAddress:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(MutexAddress))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Unaligned mutex address 0x{MutexAddress:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long Result = System.AddressArbiter.ArbitrateLock(
                Process,
                Memory,
                OwnerHandle,
                MutexAddress,
                RequesterHandle);

            if (Result != 0)
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{Result:x}!");
            }

            ThreadState.X0 = (ulong)Result;
        }

        private void SvcArbitrateUnlock(AThreadState ThreadState)
        {
            long MutexAddress = (long)ThreadState.X0;

            Device.Log.PrintDebug(LogClass.KernelSvc, "MutexAddress = 0x" + MutexAddress.ToString("x16"));

            if (IsPointingInsideKernel(MutexAddress))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Invalid mutex address 0x{MutexAddress:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(MutexAddress))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Unaligned mutex address 0x{MutexAddress:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long Result = System.AddressArbiter.ArbitrateUnlock(Process, Memory, MutexAddress);

            if (Result != 0)
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{Result:x}!");
            }

            ThreadState.X0 = (ulong)Result;
        }

        private void SvcWaitProcessWideKeyAtomic(AThreadState ThreadState)
        {
            long  MutexAddress   = (long)ThreadState.X0;
            long  CondVarAddress = (long)ThreadState.X1;
            int   ThreadHandle   =  (int)ThreadState.X2;
            long  Timeout        = (long)ThreadState.X3;

            Device.Log.PrintDebug(LogClass.KernelSvc,
                "MutexAddress = 0x"   + MutexAddress  .ToString("x16") + ", " +
                "CondVarAddress = 0x" + CondVarAddress.ToString("x16") + ", " +
                "ThreadHandle = 0x"   + ThreadHandle  .ToString("x8")  + ", " +
                "Timeout = 0x"        + Timeout       .ToString("x16"));

            if (IsPointingInsideKernel(MutexAddress))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Invalid mutex address 0x{MutexAddress:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(MutexAddress))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Unaligned mutex address 0x{MutexAddress:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long Result = System.AddressArbiter.WaitProcessWideKeyAtomic(
                Process,
                Memory,
                MutexAddress,
                CondVarAddress,
                ThreadHandle,
                Timeout);

            if (Result != 0)
            {
                if (Result == MakeError(ErrorModule.Kernel, KernelErr.Timeout))
                {
                    Device.Log.PrintDebug(LogClass.KernelSvc, $"Operation failed with error 0x{Result:x}!");
                }
                else
                {
                    Device.Log.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{Result:x}!");
                }
            }

            ThreadState.X0 = (ulong)Result;
        }

        private void SvcSignalProcessWideKey(AThreadState ThreadState)
        {
            long Address = (long)ThreadState.X0;
            int  Count   =  (int)ThreadState.X1;

            Device.Log.PrintDebug(LogClass.KernelSvc,
                "Address = 0x" + Address.ToString("x16") + ", " +
                "Count = 0x"   + Count  .ToString("x8"));

            System.AddressArbiter.SignalProcessWideKey(Process, Memory, Address, Count);

            ThreadState.X0 = 0;
        }

        private void SvcWaitForAddress(AThreadState ThreadState)
        {
            long            Address = (long)ThreadState.X0;
            ArbitrationType Type    = (ArbitrationType)ThreadState.X1;
            int             Value   = (int)ThreadState.X2;
            ulong           Timeout = ThreadState.X3;

            Device.Log.PrintDebug(LogClass.KernelSvc,
                "Address = 0x"         + Address.ToString("x16") + ", " +
                "ArbitrationType = 0x" + Type   .ToString()      + ", " +
                "Value = 0x"           + Value  .ToString("x8")  + ", " +
                "Timeout = 0x"         + Timeout.ToString("x16"));

            if (IsPointingInsideKernel(Address))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Invalid address 0x{Address:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(Address))
            {
                Device.Log.PrintWarning(LogClass.KernelSvc, $"Unaligned address 0x{Address:x16}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            switch (Type)
            {
                case ArbitrationType.WaitIfLessThan:
                    ThreadState.X0 = AddressArbiter.WaitForAddressIfLessThan(Process, ThreadState, Memory, Address, Value, Timeout, false);
                    break;

                case ArbitrationType.DecrementAndWaitIfLessThan:
                    ThreadState.X0 = AddressArbiter.WaitForAddressIfLessThan(Process, ThreadState, Memory, Address, Value, Timeout, true);
                    break;

                case ArbitrationType.WaitIfEqual:
                    ThreadState.X0 = AddressArbiter.WaitForAddressIfEqual(Process, ThreadState, Memory, Address, Value, Timeout);
                    break;

                default:
                    ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidEnumValue);
                    break;
            }
        }

        private bool IsPointingInsideKernel(long Address)
        {
            return ((ulong)Address + 0x1000000000) < 0xffffff000;
        }

        private bool IsAddressNotWordAligned(long Address)
        {
            return (Address & 3) != 0;
        }
    }
}