using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Memory;

namespace Ryujinx.Horizon.Kernel.Svc
{
    class Syscall64
    {
        private readonly Syscall _syscall;

        public Syscall64(Syscall syscall)
        {
            _syscall = syscall;
        }

        // IPC

        public Result ConnectToNamedPort64([R(1)] ulong namePtr, [R(1)] out int handle)
        {
            return _syscall.ConnectToNamedPort(namePtr, out handle);
        }

        public Result SendSyncRequest64([R(0)] int handle)
        {
            return _syscall.SendSyncRequest(handle);
        }

        public Result SendSyncRequestWithUserBuffer64([R(0)] ulong messagePtr, [R(1)] ulong messageSize, [R(2)] int handle)
        {
            return _syscall.SendSyncRequestWithUserBuffer(messagePtr, messageSize, handle);
        }

        public Result SendAsyncRequestWithUserBuffer64(
            [R(1)] ulong messagePtr,
            [R(2)] ulong messageSize,
            [R(3)] int handle,
            [R(1)] out int doneEventHandle)
        {
            return _syscall.SendAsyncRequestWithUserBuffer(messagePtr, messageSize, handle, out doneEventHandle);
        }

        public Result CreateSession64(
            [R(2)] bool isLight,
            [R(3)] ulong namePtr,
            [R(1)] out int serverSessionHandle,
            [R(2)] out int clientSessionHandle)
        {
            return _syscall.CreateSession(isLight, namePtr, out serverSessionHandle, out clientSessionHandle);
        }

        public Result AcceptSession64([R(1)] int portHandle, [R(1)] out int sessionHandle)
        {
            return _syscall.AcceptSession(portHandle, out sessionHandle);
        }

        public Result ReplyAndReceive64(
            [R(1)] ulong handlesPtr,
            [R(2)] int handlesCount,
            [R(3)] int replyTargetHandle,
            [R(4)] long timeout,
            [R(1)] out int handleIndex)
        {
            return _syscall.ReplyAndReceive(handlesPtr, handlesCount, replyTargetHandle, timeout, out handleIndex);
        }

        public Result ReplyAndReceiveWithUserBuffer64(
            [R(1)] ulong messagePtr,
            [R(2)] ulong messageSize,
            [R(3)] ulong handlesPtr,
            [R(4)] int handlesCount,
            [R(5)] int replyTargetHandle,
            [R(6)] long timeout,
            [R(1)] out int handleIndex)
        {
            return _syscall.ReplyAndReceiveWithUserBuffer(
                handlesPtr,
                messagePtr,
                messageSize,
                handlesCount,
                replyTargetHandle,
                timeout,
                out handleIndex);
        }

        public Result CreatePort64(
            [R(2)] int maxSessions,
            [R(3)] bool isLight,
            [R(4)] ulong namePtr,
            [R(1)] out int serverPortHandle,
            [R(2)] out int clientPortHandle)
        {
            return _syscall.CreatePort(maxSessions, isLight, namePtr, out serverPortHandle, out clientPortHandle);
        }

        public Result ManageNamedPort64([R(1)] ulong namePtr, [R(2)] int maxSessions, [R(1)] out int handle)
        {
            return _syscall.ManageNamedPort(namePtr, maxSessions, out handle);
        }

        public Result ConnectToPort64([R(1)] int clientPortHandle, [R(1)] out int clientSessionHandle)
        {
            return _syscall.ConnectToPort(clientPortHandle, out clientSessionHandle);
        }

        // Memory

        public Result SetHeapSize64([R(1)] ulong size, [R(1)] out ulong position)
        {
            return _syscall.SetHeapSize(size, out position);
        }

        public Result SetMemoryAttribute64(
            [R(0)] ulong position,
            [R(1)] ulong size,
            [R(2)] KMemoryAttribute attributeMask,
            [R(3)] KMemoryAttribute attributeValue)
        {
            return _syscall.SetMemoryAttribute(position, size, attributeMask, attributeValue);
        }

        public Result MapMemory64([R(0)] ulong dst, [R(1)] ulong src, [R(2)] ulong size)
        {
            return _syscall.MapMemory(dst, src, size);
        }

        public Result UnmapMemory64([R(0)] ulong dst, [R(1)] ulong src, [R(2)] ulong size)
        {
            return _syscall.UnmapMemory(dst, src, size);
        }

        public Result QueryMemory64([R(0)] ulong infoPtr, [R(1)] ulong x1, [R(2)] ulong address)
        {
            return _syscall.QueryMemory(infoPtr, x1, address);
        }

        public Result MapSharedMemory64([R(0)] int handle, [R(1)] ulong address, [R(2)] ulong size, [R(3)] KMemoryPermission permission)
        {
            return _syscall.MapSharedMemory(handle, address, size, permission);
        }

        public Result UnmapSharedMemory64([R(0)] int handle, [R(1)] ulong address, [R(2)] ulong size)
        {
            return _syscall.UnmapSharedMemory(handle, address, size);
        }

        public Result CreateTransferMemory64(
            [R(1)] ulong address,
            [R(2)] ulong size,
            [R(3)] KMemoryPermission permission,
            [R(1)] out int handle)
        {
            return _syscall.CreateTransferMemory(address, size, permission, out handle);
        }

        public Result MapPhysicalMemory64([R(0)] ulong address, [R(1)] ulong size)
        {
            return _syscall.MapPhysicalMemory(address, size);
        }

        public Result UnmapPhysicalMemory64([R(0)] ulong address, [R(1)] ulong size)
        {
            return _syscall.UnmapPhysicalMemory(address, size);
        }

        public Result MapProcessCodeMemory64([R(0)] int handle, [R(1)] ulong dst, [R(2)] ulong src, [R(3)] ulong size)
        {
            return _syscall.MapProcessCodeMemory(handle, dst, src, size);
        }

        public Result UnmapProcessCodeMemory64([R(0)] int handle, [R(1)] ulong dst, [R(2)] ulong src, [R(3)] ulong size)
        {
            return _syscall.UnmapProcessCodeMemory(handle, dst, src, size);
        }

        public Result SetProcessMemoryPermission64([R(0)] int handle, [R(1)] ulong src, [R(2)] ulong size, [R(3)] KMemoryPermission permission)
        {
            return _syscall.SetProcessMemoryPermission(handle, src, size, permission);
        }

        // System

        public void ExitProcess64()
        {
            _syscall.ExitProcess();
        }

        public Result TerminateProcess64([R(0)] int handle)
        {
            return _syscall.TerminateProcess(handle);
        }

        public Result SignalEvent64([R(0)] int handle)
        {
            return _syscall.SignalEvent(handle);
        }

        public Result ClearEvent64([R(0)] int handle)
        {
            return _syscall.ClearEvent(handle);
        }

        public Result CloseHandle64([R(0)] int handle)
        {
            return _syscall.CloseHandle(handle);
        }

        public Result ResetSignal64([R(0)] int handle)
        {
            return _syscall.ResetSignal(handle);
        }

        public ulong GetSystemTick64()
        {
            return _syscall.GetSystemTick();
        }

        public Result GetProcessId64([R(1)] int handle, [R(1)] out long pid)
        {
            return _syscall.GetProcessId(handle, out pid);
        }

        public void Break64([R(0)] ulong reason, [R(1)] ulong x1, [R(2)] ulong info)
        {
            _syscall.Break(reason);
        }

        public void OutputDebugString64([R(0)] ulong strPtr, [R(1)] ulong size)
        {
            _syscall.OutputDebugString(strPtr, size);
        }

        public Result GetInfo64([R(1)] InfoType id, [R(2)] int handle, [R(3)] long subId, [R(1)] out ulong value)
        {
            return _syscall.GetInfo(id, handle, subId, out value);
        }

        public Result CreateEvent64([R(1)] out int wEventHandle, [R(2)] out int rEventHandle)
        {
            return _syscall.CreateEvent(out wEventHandle, out rEventHandle);
        }

        public Result GetProcessList64([R(1)] ulong address, [R(2)] int maxCount, [R(1)] out int count)
        {
            return _syscall.GetProcessList(address, maxCount, out count);
        }

        public Result GetSystemInfo64([R(1)] uint id, [R(2)] int handle, [R(3)] long subId, [R(1)] out long value)
        {
            return _syscall.GetSystemInfo(id, handle, subId, out value);
        }

        // Thread

        public Result CreateThread64(
            [R(1)] ulong entrypoint,
            [R(2)] ulong argsPtr,
            [R(3)] ulong stackTop,
            [R(4)] int priority,
            [R(5)] int cpuCore,
            [R(1)] out int handle)
        {
            return _syscall.CreateThread(entrypoint, argsPtr, stackTop, priority, cpuCore, out handle);
        }

        public Result StartThread64([R(0)] int handle)
        {
            return _syscall.StartThread(handle);
        }

        public void ExitThread64()
        {
            _syscall.ExitThread();
        }

        public void SleepThread64([R(0)] long timeout)
        {
            _syscall.SleepThread(timeout);
        }

        public Result GetThreadPriority64([R(1)] int handle, [R(1)] out int priority)
        {
            return _syscall.GetThreadPriority(handle, out priority);
        }

        public Result SetThreadPriority64([R(0)] int handle, [R(1)] int priority)
        {
            return _syscall.SetThreadPriority(handle, priority);
        }

        public Result GetThreadCoreMask64([R(2)] int handle, [R(1)] out int preferredCore, [R(2)] out long affinityMask)
        {
            return _syscall.GetThreadCoreMask(handle, out preferredCore, out affinityMask);
        }

        public Result SetThreadCoreMask64([R(0)] int handle, [R(1)] int preferredCore, [R(2)] long affinityMask)
        {
            return _syscall.SetThreadCoreMask(handle, preferredCore, affinityMask);
        }

        public int GetCurrentProcessorNumber64()
        {
            return _syscall.GetCurrentProcessorNumber();
        }

        public Result GetThreadId64([R(1)] int handle, [R(1)] out long threadUid)
        {
            return _syscall.GetThreadId(handle, out threadUid);
        }

        public Result SetThreadActivity64([R(0)] int handle, [R(1)] bool pause)
        {
            return _syscall.SetThreadActivity(handle, pause);
        }

        public Result GetThreadContext364([R(0)] ulong address, [R(1)] int handle)
        {
            return _syscall.GetThreadContext3(address, handle);
        }

        // Thread synchronization

        public Result WaitSynchronization64([R(1)] ulong handlesPtr, [R(2)] int handlesCount, [R(3)] long timeout, [R(1)] out int handleIndex)
        {
            return _syscall.WaitSynchronization(out handleIndex, handlesPtr, handlesCount, timeout);
        }

        public Result CancelSynchronization64([R(0)] int handle)
        {
            return _syscall.CancelSynchronization(handle);
        }

        public Result ArbitrateLock64([R(0)] int ownerHandle, [R(1)] ulong mutexAddress, [R(2)] int requesterHandle)
        {
            return _syscall.ArbitrateLock(ownerHandle, mutexAddress, requesterHandle);
        }

        public Result ArbitrateUnlock64([R(0)] ulong mutexAddress)
        {
            return _syscall.ArbitrateUnlock(mutexAddress);
        }

        public Result WaitProcessWideKeyAtomic64(
            [R(0)] ulong mutexAddress,
            [R(1)] ulong condVarAddress,
            [R(2)] int handle,
            [R(3)] long timeout)
        {
            return _syscall.WaitProcessWideKeyAtomic(mutexAddress, condVarAddress, handle, timeout);
        }

        public Result SignalProcessWideKey64([R(0)] ulong address, [R(1)] int count)
        {
            return _syscall.SignalProcessWideKey(address, count);
        }

        public Result WaitForAddress64([R(0)] ulong address, [R(1)] ArbitrationType type, [R(2)] int value, [R(3)] long timeout)
        {
            return _syscall.WaitForAddress(address, type, value, timeout);
        }

        public Result SignalToAddress64([R(0)] ulong address, [R(1)] SignalType type, [R(2)] int value, [R(3)] int count)
        {
            return _syscall.SignalToAddress(address, type, value, count);
        }
    }
}
