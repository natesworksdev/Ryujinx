using ChocolArm64.State;
using Ryujinx.Common.Logging;
using System.Collections.Generic;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal partial class SvcHandler
    {
        private void SvcWaitSynchronization(AThreadState threadState)
        {
            long handlesPtr   = (long)threadState.X1;
            int  handlesCount =  (int)threadState.X2;
            long timeout      = (long)threadState.X3;

            Logger.PrintDebug(LogClass.KernelSvc,
                "HandlesPtr = 0x"   + handlesPtr  .ToString("x16") + ", " +
                "HandlesCount = 0x" + handlesCount.ToString("x8")  + ", " +
                "Timeout = 0x"      + timeout     .ToString("x16"));

            if ((uint)handlesCount > 0x40)
            {
                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.CountOutOfRange);

                return;
            }

            List<KSynchronizationObject> syncObjs = new List<KSynchronizationObject>();

            for (int index = 0; index < handlesCount; index++)
            {
                int handle = _memory.ReadInt32(handlesPtr + index * 4);

                KSynchronizationObject syncObj = _process.HandleTable.GetObject<KSynchronizationObject>(handle);

                if (syncObj == null) break;

                syncObjs.Add(syncObj);
            }

            int hndIndex = (int)threadState.X1;

            ulong high = threadState.X1 & (0xffffffffUL << 32);

            long result = _system.Synchronization.WaitFor(syncObjs.ToArray(), timeout, ref hndIndex);

            if (result != 0)
            {
                if (result == MakeError(ErrorModule.Kernel, KernelErr.Timeout) ||
                    result == MakeError(ErrorModule.Kernel, KernelErr.Cancelled))
                    Logger.PrintDebug(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");
                else
                    Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");
            }

            threadState.X0 = (ulong)result;
            threadState.X1 = (uint)hndIndex | high;
        }

        private void SvcCancelSynchronization(AThreadState threadState)
        {
            int threadHandle = (int)threadState.X0;

            Logger.PrintDebug(LogClass.KernelSvc, "ThreadHandle = 0x" + threadHandle.ToString("x8"));

            KThread thread = _process.HandleTable.GetKThread(threadHandle);

            if (thread == null)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid thread handle 0x{threadHandle:x8}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                return;
            }

            thread.CancelSynchronization();

            threadState.X0 = 0;
        }

        private void SvcArbitrateLock(AThreadState threadState)
        {
            int  ownerHandle     =  (int)threadState.X0;
            long mutexAddress    = (long)threadState.X1;
            int  requesterHandle =  (int)threadState.X2;

            Logger.PrintDebug(LogClass.KernelSvc,
                "OwnerHandle = 0x"     + ownerHandle    .ToString("x8")  + ", " +
                "MutexAddress = 0x"    + mutexAddress   .ToString("x16") + ", " +
                "RequesterHandle = 0x" + requesterHandle.ToString("x8"));

            if (IsPointingInsideKernel(mutexAddress))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid mutex address 0x{mutexAddress:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Unaligned mutex address 0x{mutexAddress:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long result = _system.AddressArbiter.ArbitrateLock(
                _process,
                _memory,
                ownerHandle,
                mutexAddress,
                requesterHandle);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcArbitrateUnlock(AThreadState threadState)
        {
            long mutexAddress = (long)threadState.X0;

            Logger.PrintDebug(LogClass.KernelSvc, "MutexAddress = 0x" + mutexAddress.ToString("x16"));

            if (IsPointingInsideKernel(mutexAddress))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid mutex address 0x{mutexAddress:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Unaligned mutex address 0x{mutexAddress:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long result = _system.AddressArbiter.ArbitrateUnlock(_memory, mutexAddress);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcWaitProcessWideKeyAtomic(AThreadState threadState)
        {
            long  mutexAddress   = (long)threadState.X0;
            long  condVarAddress = (long)threadState.X1;
            int   threadHandle   =  (int)threadState.X2;
            long  timeout        = (long)threadState.X3;

            Logger.PrintDebug(LogClass.KernelSvc,
                "MutexAddress = 0x"   + mutexAddress  .ToString("x16") + ", " +
                "CondVarAddress = 0x" + condVarAddress.ToString("x16") + ", " +
                "ThreadHandle = 0x"   + threadHandle  .ToString("x8")  + ", " +
                "Timeout = 0x"        + timeout       .ToString("x16"));

            if (IsPointingInsideKernel(mutexAddress))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid mutex address 0x{mutexAddress:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Unaligned mutex address 0x{mutexAddress:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long result = _system.AddressArbiter.WaitProcessWideKeyAtomic(
                _memory,
                mutexAddress,
                condVarAddress,
                threadHandle,
                timeout);

            if (result != 0)
            {
                if (result == MakeError(ErrorModule.Kernel, KernelErr.Timeout))
                    Logger.PrintDebug(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");
                else
                    Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");
            }

            threadState.X0 = (ulong)result;
        }

        private void SvcSignalProcessWideKey(AThreadState threadState)
        {
            long address = (long)threadState.X0;
            int  count   =  (int)threadState.X1;

            Logger.PrintDebug(LogClass.KernelSvc,
                "Address = 0x" + address.ToString("x16") + ", " +
                "Count = 0x"   + count  .ToString("x8"));

            _system.AddressArbiter.SignalProcessWideKey(_process, _memory, address, count);

            threadState.X0 = 0;
        }

        private void SvcWaitForAddress(AThreadState threadState)
        {
            long            address =            (long)threadState.X0;
            ArbitrationType type    = (ArbitrationType)threadState.X1;
            int             value   =             (int)threadState.X2;
            long            timeout =            (long)threadState.X3;

            Logger.PrintDebug(LogClass.KernelSvc,
                "Address = 0x" + address.ToString("x16") + ", " +
                "Type = "      + type   .ToString()      + ", " +
                "Value = 0x"   + value  .ToString("x8")  + ", " +
                "Timeout = 0x" + timeout.ToString("x16"));

            if (IsPointingInsideKernel(address))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid address 0x{address:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(address))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Unaligned address 0x{address:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long result;

            switch (type)
            {
                case ArbitrationType.WaitIfLessThan:
                    result = _system.AddressArbiter.WaitForAddressIfLessThan(_memory, address, value, false, timeout);
                    break;

                case ArbitrationType.DecrementAndWaitIfLessThan:
                    result = _system.AddressArbiter.WaitForAddressIfLessThan(_memory, address, value, true, timeout);
                    break;

                case ArbitrationType.WaitIfEqual:
                    result = _system.AddressArbiter.WaitForAddressIfEqual(_memory, address, value, timeout);
                    break;

                default:
                    result = MakeError(ErrorModule.Kernel, KernelErr.InvalidEnumValue);
                    break;
            }

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcSignalToAddress(AThreadState threadState)
        {
            long       address =       (long)threadState.X0;
            SignalType type    = (SignalType)threadState.X1;
            int        value   =        (int)threadState.X2;
            int        count   =        (int)threadState.X3;

            Logger.PrintDebug(LogClass.KernelSvc,
                "Address = 0x" + address.ToString("x16") + ", " +
                "Type = "      + type   .ToString()      + ", " +
                "Value = 0x"   + value  .ToString("x8")  + ", " +
                "Count = 0x"   + count  .ToString("x8"));

            if (IsPointingInsideKernel(address))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid address 0x{address:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (IsAddressNotWordAligned(address))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Unaligned address 0x{address:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            long result;

            switch (type)
            {
                case SignalType.Signal:
                    result = _system.AddressArbiter.Signal(address, count);
                    break;

                case SignalType.SignalAndIncrementIfEqual:
                    result = _system.AddressArbiter.SignalAndIncrementIfEqual(_memory, address, value, count);
                    break;

                case SignalType.SignalAndModifyIfEqual:
                    result = _system.AddressArbiter.SignalAndModifyIfEqual(_memory, address, value, count);
                    break;

                default:
                    result = MakeError(ErrorModule.Kernel, KernelErr.InvalidEnumValue);
                    break;
            }

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private bool IsPointingInsideKernel(long address)
        {
            return (ulong)address + 0x1000000000 < 0xffffff000;
        }

        private bool IsAddressNotWordAligned(long address)
        {
            return (address & 3) != 0;
        }
    }
}