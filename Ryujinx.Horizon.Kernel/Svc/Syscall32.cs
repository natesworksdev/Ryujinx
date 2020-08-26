using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Memory;

namespace Ryujinx.Horizon.Kernel.Svc
{
    class Syscall32
    {
        private readonly Syscall _syscall;

        public Syscall32(Syscall syscall)
        {
            _syscall = syscall;
        }

        // IPC

        public Result ConnectToNamedPort32([R(1)] uint namePtr, [R(1)] out int handle)
        {
            return _syscall.ConnectToNamedPort(namePtr, out handle);
        }

        public Result SendSyncRequest32([R(0)] int handle)
        {
            return _syscall.SendSyncRequest(handle);
        }

        public Result SendSyncRequestWithUserBuffer32([R(0)] uint messagePtr, [R(1)] uint messageSize, [R(2)] int handle)
        {
            return _syscall.SendSyncRequestWithUserBuffer(messagePtr, messageSize, handle);
        }

        public Result CreateSession32(
            [R(2)] bool isLight,
            [R(3)] uint namePtr,
            [R(1)] out int serverSessionHandle,
            [R(2)] out int clientSessionHandle)
        {
            return _syscall.CreateSession(isLight, namePtr, out serverSessionHandle, out clientSessionHandle);
        }

        public Result AcceptSession32([R(1)] int portHandle, [R(1)] out int sessionHandle)
        {
            return _syscall.AcceptSession(portHandle, out sessionHandle);
        }

        public Result ReplyAndReceive32(
            [R(0)] uint timeoutLow,
            [R(1)] ulong handlesPtr,
            [R(2)] int handlesCount,
            [R(3)] int replyTargetHandle,
            [R(4)] uint timeoutHigh,
            [R(1)] out int handleIndex)
        {
            long timeout = (long)(timeoutLow | ((ulong)timeoutHigh << 32));

            return _syscall.ReplyAndReceive(handlesPtr, handlesCount, replyTargetHandle, timeout, out handleIndex);
        }

        public Result CreatePort32(
            [R(0)] uint namePtr,
            [R(2)] int maxSessions,
            [R(3)] bool isLight,
            [R(1)] out int serverPortHandle,
            [R(2)] out int clientPortHandle)
        {
            return _syscall.CreatePort(maxSessions, isLight, namePtr, out serverPortHandle, out clientPortHandle);
        }

        public Result ManageNamedPort32([R(1)] uint namePtr, [R(2)] int maxSessions, [R(1)] out int handle)
        {
            return _syscall.ManageNamedPort(namePtr, maxSessions, out handle);
        }

        public Result ConnectToPort32([R(1)] int clientPortHandle, [R(1)] out int clientSessionHandle)
        {
            return _syscall.ConnectToPort(clientPortHandle, out clientSessionHandle);
        }

        // Memory

        public Result SetHeapSize32([R(1)] uint size, [R(1)] out uint position)
        {
            Result result = _syscall.SetHeapSize(size, out ulong temporaryPosition);

            position = (uint)temporaryPosition;

            return result;
        }

        public Result SetMemoryAttribute32(
            [R(0)] uint position,
            [R(1)] uint size,
            [R(2)] KMemoryAttribute attributeMask,
            [R(3)] KMemoryAttribute attributeValue)
        {
            return _syscall.SetMemoryAttribute(position, size, attributeMask, attributeValue);
        }

        public Result MapMemory32([R(0)] uint dst, [R(1)] uint src, [R(2)] uint size)
        {
            return _syscall.MapMemory(dst, src, size);
        }

        public Result UnmapMemory32([R(0)] uint dst, [R(1)] uint src, [R(2)] uint size)
        {
            return _syscall.UnmapMemory(dst, src, size);
        }

        public Result QueryMemory32([R(0)] uint infoPtr, [R(1)] uint r1, [R(2)] uint address)
        {
            return _syscall.QueryMemory(infoPtr, r1, address);
        }

        public Result MapSharedMemory32([R(0)] int handle, [R(1)] uint address, [R(2)] uint size, [R(3)] KMemoryPermission permission)
        {
            return _syscall.MapSharedMemory(handle, address, size, permission);
        }

        public Result UnmapSharedMemory32([R(0)] int handle, [R(1)] uint address, [R(2)] uint size)
        {
            return _syscall.UnmapSharedMemory(handle, address, size);
        }

        public Result CreateTransferMemory32(
            [R(1)] uint address,
            [R(2)] uint size,
            [R(3)] KMemoryPermission permission,
            [R(1)] out int handle)
        {
            return _syscall.CreateTransferMemory(address, size, permission, out handle);
        }

        public Result MapPhysicalMemory32([R(0)] uint address, [R(1)] uint size)
        {
            return _syscall.MapPhysicalMemory(address, size);
        }

        public Result UnmapPhysicalMemory32([R(0)] uint address, [R(1)] uint size)
        {
            return _syscall.UnmapPhysicalMemory(address, size);
        }

        public Result MapProcessCodeMemory32([R(0)] int handle, [R(1)] uint srcLow, [R(2)] uint dstLow, [R(3)] uint dstHigh, [R(4)] uint srcHigh, [R(5)] uint sizeLow, [R(6)] uint sizeHigh)
        {
            ulong src = srcLow | ((ulong)srcHigh << 32);
            ulong dst = dstLow | ((ulong)dstHigh << 32);
            ulong size = sizeLow | ((ulong)sizeHigh << 32);

            return _syscall.MapProcessCodeMemory(handle, dst, src, size);
        }

        public Result UnmapProcessCodeMemory32([R(0)] int handle, [R(1)] uint srcLow, [R(2)] uint dstLow, [R(3)] uint dstHigh, [R(4)] uint srcHigh, [R(5)] uint sizeLow, [R(6)] uint sizeHigh)
        {
            ulong src = srcLow | ((ulong)srcHigh << 32);
            ulong dst = dstLow | ((ulong)dstHigh << 32);
            ulong size = sizeLow | ((ulong)sizeHigh << 32);

            return _syscall.UnmapProcessCodeMemory(handle, dst, src, size);
        }

        public Result SetProcessMemoryPermission32(
            [R(0)] int handle,
            [R(1)] uint sizeLow,
            [R(2)] uint srcLow,
            [R(3)] uint srcHigh,
            [R(4)] uint sizeHigh,
            [R(5)] KMemoryPermission permission)
        {
            ulong src = srcLow | ((ulong)srcHigh << 32);
            ulong size = sizeLow | ((ulong)sizeHigh << 32);

            return _syscall.SetProcessMemoryPermission(handle, src, size, permission);
        }

        // System

        public void ExitProcess32()
        {
            _syscall.ExitProcess();
        }

        public Result TerminateProcess32([R(0)] int handle)
        {
            return _syscall.TerminateProcess(handle);
        }

        public Result SignalEvent32([R(0)] int handle)
        {
            return _syscall.SignalEvent(handle);
        }

        public Result ClearEvent32([R(0)] int handle)
        {
            return _syscall.ClearEvent(handle);
        }

        public Result CloseHandle32([R(0)] int handle)
        {
            return _syscall.CloseHandle(handle);
        }

        public Result ResetSignal32([R(0)] int handle)
        {
            return _syscall.ResetSignal(handle);
        }

        public void GetSystemTick32([R(0)] out uint resultLow, [R(1)] out uint resultHigh)
        {
            ulong result = _syscall.GetSystemTick();

            resultLow = (uint)(result & uint.MaxValue);
            resultHigh = (uint)(result >> 32);
        }

        public Result GetProcessId32([R(1)] int handle, [R(1)] out int pidLow, [R(2)] out int pidHigh)
        {
            Result result = _syscall.GetProcessId(handle, out long pid);

            pidLow = (int)(pid & uint.MaxValue);
            pidHigh = (int)(pid >> 32);

            return result;
        }

        public void Break32([R(0)] uint reason, [R(1)] uint r1, [R(2)] uint info)
        {
            _syscall.Break(reason);
        }

        public void OutputDebugString32([R(0)] uint strPtr, [R(1)] uint size)
        {
            _syscall.OutputDebugString(strPtr, size);
        }

        public Result GetInfo32(
            [R(0)] uint subIdLow,
            [R(1)] InfoType id,
            [R(2)] int handle,
            [R(3)] uint subIdHigh,
            [R(1)] out uint valueLow,
            [R(2)] out uint valueHigh)
        {
            long subId = (long)(subIdLow | ((ulong)subIdHigh << 32));

            Result result = _syscall.GetInfo(id, handle, subId, out ulong value);

            valueHigh = (uint)(value >> 32);
            valueLow = (uint)(value & uint.MaxValue);

            return result;
        }

        public Result CreateEvent32([R(1)] out int wEventHandle, [R(2)] out int rEventHandle)
        {
            return _syscall.CreateEvent(out wEventHandle, out rEventHandle);
        }

        public Result GetProcessList32([R(1)] ulong address, [R(2)] int maxCount, [R(1)] out int count)
        {
            return _syscall.GetProcessList(address, maxCount, out count);
        }

        public Result GetSystemInfo32([R(1)] uint subIdLow, [R(2)] uint id, [R(3)] int handle, [R(3)] uint subIdHigh, [R(1)] out int valueLow, [R(2)] out int valueHigh)
        {
            long subId = (long)(subIdLow | ((ulong)subIdHigh << 32));

            Result result = _syscall.GetSystemInfo(id, handle, subId, out long value);

            valueHigh = (int)(value >> 32);
            valueLow = (int)(value & uint.MaxValue);

            return result;
        }

        public Result FlushProcessDataCache32(
            [R(0)] uint processHandle,
            [R(2)] uint addressLow,
            [R(3)] uint addressHigh,
            [R(1)] uint sizeLow,
            [R(4)] uint sizeHigh)
        {
            // FIXME: This needs to be implemented as ARMv7 doesn't have any way to do cache maintenance operations on EL0.
            // As we don't support (and don't actually need) to flush the cache, this is stubbed.
            return Result.Success;
        }

        // Thread

        public Result CreateThread32(
            [R(1)] uint entrypoint,
            [R(2)] uint argsPtr,
            [R(3)] uint stackTop,
            [R(0)] int priority,
            [R(4)] int cpuCore,
            [R(1)] out int handle)
        {
            return _syscall.CreateThread(entrypoint, argsPtr, stackTop, priority, cpuCore, out handle);
        }

        public Result StartThread32([R(0)] int handle)
        {
            return _syscall.StartThread(handle);
        }

        public void ExitThread32()
        {
            _syscall.ExitThread();
        }

        public void SleepThread32([R(0)] uint timeoutLow, [R(1)] uint timeoutHigh)
        {
            long timeout = (long)(timeoutLow | ((ulong)timeoutHigh << 32));

            _syscall.SleepThread(timeout);
        }

        public Result GetThreadPriority32([R(1)] int handle, [R(1)] out int priority)
        {
            return _syscall.GetThreadPriority(handle, out priority);
        }

        public Result SetThreadPriority32([R(0)] int handle, [R(1)] int priority)
        {
            return _syscall.SetThreadPriority(handle, priority);
        }

        public Result GetThreadCoreMask32([R(2)] int handle, [R(1)] out int preferredCore, [R(2)] out int affinityMaskLow, [R(3)] out int affinityMaskHigh)
        {
            Result result = _syscall.GetThreadCoreMask(handle, out preferredCore, out long affinityMask);

            affinityMaskLow = (int)(affinityMask >> 32);
            affinityMaskHigh = (int)(affinityMask & uint.MaxValue);

            return result;
        }

        public Result SetThreadCoreMask32([R(0)] int handle, [R(1)] int preferredCore, [R(2)] uint affinityMaskLow, [R(3)] uint affinityMaskHigh)
        {
            long affinityMask = (long)(affinityMaskLow | ((ulong)affinityMaskHigh << 32));

            return _syscall.SetThreadCoreMask(handle, preferredCore, affinityMask);
        }

        public int GetCurrentProcessorNumber32()
        {
            return _syscall.GetCurrentProcessorNumber();
        }

        public Result GetThreadId32([R(1)] int handle, [R(1)] out uint threadUidLow, [R(2)] out uint threadUidHigh)
        {

            Result result = _syscall.GetThreadId(handle, out long threadUid);

            threadUidLow = (uint)(threadUid >> 32);
            threadUidHigh = (uint)(threadUid & uint.MaxValue);

            return result;
        }

        public Result SetThreadActivity32([R(0)] int handle, [R(1)] bool pause)
        {
            return _syscall.SetThreadActivity(handle, pause);
        }

        public Result GetThreadContext332([R(0)] uint address, [R(1)] int handle)
        {
            return _syscall.GetThreadContext3(address, handle);
        }

        // Thread synchronization

        public Result WaitSynchronization32(
            [R(0)] uint timeoutLow,
            [R(1)] uint handlesPtr,
            [R(2)] int handlesCount,
            [R(3)] uint timeoutHigh,
            [R(1)] out int handleIndex)
        {
            long timeout = (long)(timeoutLow | ((ulong)timeoutHigh << 32));

            return _syscall.WaitSynchronization(out handleIndex, handlesPtr, handlesCount, timeout);
        }

        public Result CancelSynchronization32([R(0)] int handle)
        {
            return _syscall.CancelSynchronization(handle);
        }


        public Result ArbitrateLock32([R(0)] int ownerHandle, [R(1)] uint mutexAddress, [R(2)] int requesterHandle)
        {
            return _syscall.ArbitrateLock(ownerHandle, mutexAddress, requesterHandle);
        }

        public Result ArbitrateUnlock32([R(0)] uint mutexAddress)
        {
            return _syscall.ArbitrateUnlock(mutexAddress);
        }

        public Result WaitProcessWideKeyAtomic32(
            [R(0)] uint mutexAddress,
            [R(1)] uint condVarAddress,
            [R(2)] int handle,
            [R(3)] uint timeoutLow,
            [R(4)] uint timeoutHigh)
        {
            long timeout = (long)(timeoutLow | ((ulong)timeoutHigh << 32));

            return _syscall.WaitProcessWideKeyAtomic(mutexAddress, condVarAddress, handle, timeout);
        }

        public Result SignalProcessWideKey32([R(0)] uint address, [R(1)] int count)
        {
            return _syscall.SignalProcessWideKey(address, count);
        }

        public Result WaitForAddress32([R(0)] uint address, [R(1)] ArbitrationType type, [R(2)] int value, [R(3)] uint timeoutLow, [R(4)] uint timeoutHigh)
        {
            long timeout = (long)(timeoutLow | ((ulong)timeoutHigh << 32));

            return _syscall.WaitForAddress(address, type, value, timeout);
        }

        public Result SignalToAddress32([R(0)] uint address, [R(1)] SignalType type, [R(2)] int value, [R(3)] int count)
        {
            return _syscall.SignalToAddress(address, type, value, count);
        }
    }
}
