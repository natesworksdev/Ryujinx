using ChocolArm64.Memory;
using ChocolArm64.State;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services;
using System;
using System.Threading;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal partial class SvcHandler
    {
        private const int AllowedCpuIdBitmask = 0b1111;

        private const bool EnableProcessDebugging = false;

        private void SvcExitProcess(AThreadState threadState)
        {
            _device.System.ExitProcess(_process.ProcessId);
        }

        private void SignalEvent64(AThreadState threadState)
        {
            threadState.X0 = (ulong)SignalEvent((int)threadState.X0);
        }

        private KernelResult SignalEvent(int handle)
        {
            KWritableEvent writableEvent = _process.HandleTable.GetObject<KWritableEvent>(handle);

            KernelResult result;

            if (writableEvent != null)
            {
                writableEvent.Signal();

                result = KernelResult.Success;
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            if (result != KernelResult.Success) Logger.PrintWarning(LogClass.KernelSvc, "Operation failed with error: " + result + "!");

            return result;
        }

        private void ClearEvent64(AThreadState threadState)
        {
            threadState.X0 = (ulong)ClearEvent((int)threadState.X0);
        }

        private KernelResult ClearEvent(int handle)
        {
            KernelResult result;

            KWritableEvent writableEvent = _process.HandleTable.GetObject<KWritableEvent>(handle);

            if (writableEvent == null)
            {
                KReadableEvent readableEvent = _process.HandleTable.GetObject<KReadableEvent>(handle);

                result = readableEvent?.Clear() ?? KernelResult.InvalidHandle;
            }
            else
            {
                result = writableEvent.Clear();
            }

            if (result != KernelResult.Success) Logger.PrintWarning(LogClass.KernelSvc, "Operation failed with error: " + result + "!");

            return result;
        }

        private void SvcCloseHandle(AThreadState threadState)
        {
            int handle = (int)threadState.X0;

            object obj = _process.HandleTable.GetObject<object>(handle);

            _process.HandleTable.CloseHandle(handle);

            if (obj == null)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid handle 0x{handle:x8}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                return;
            }

            if (obj is KSession session)
                session.Dispose();
            else if (obj is KTransferMemory transferMemory)
                _process.MemoryManager.ResetTransferMemory(
                    transferMemory.Position,
                    transferMemory.Size);

            threadState.X0 = 0;
        }

        private void ResetSignal64(AThreadState threadState)
        {
            threadState.X0 = (ulong)ResetSignal((int)threadState.X0);
        }

        private KernelResult ResetSignal(int handle)
        {
            KReadableEvent readableEvent = _process.HandleTable.GetObject<KReadableEvent>(handle);

            KernelResult result;

            //TODO: KProcess support.
            if (readableEvent != null)
                result = readableEvent.ClearIfSignaled();
            else
                result = KernelResult.InvalidHandle;

            if (result == KernelResult.InvalidState)
                Logger.PrintDebug(LogClass.KernelSvc, "Operation failed with error: " + result + "!");
            else if (result != KernelResult.Success) Logger.PrintWarning(LogClass.KernelSvc, "Operation failed with error: " + result + "!");

            return result;
        }

        private void SvcGetSystemTick(AThreadState threadState)
        {
            threadState.X0 = threadState.CntpctEl0;
        }

        private void SvcConnectToNamedPort(AThreadState threadState)
        {
            long stackPtr = (long)threadState.X0;
            long namePtr  = (long)threadState.X1;

            string name = AMemoryHelper.ReadAsciiString(_memory, namePtr, 8);

            //TODO: Validate that app has perms to access the service, and that the service
            //actually exists, return error codes otherwise.
            KSession session = new KSession(ServiceFactory.MakeService(_system, name), name);

            if (_process.HandleTable.GenerateHandle(session, out int handle) != KernelResult.Success) throw new InvalidOperationException("Out of handles!");

            threadState.X0 = 0;
            threadState.X1 = (uint)handle;
        }

        private void SvcSendSyncRequest(AThreadState threadState)
        {
            SendSyncRequest(threadState, threadState.Tpidr, 0x100, (int)threadState.X0);
        }

        private void SvcSendSyncRequestWithUserBuffer(AThreadState threadState)
        {
            SendSyncRequest(
                      threadState,
                (long)threadState.X0,
                (long)threadState.X1,
                 (int)threadState.X2);
        }

        private void SendSyncRequest(AThreadState threadState, long messagePtr, long size, int handle)
        {
            KThread currThread = _process.GetThread(threadState.Tpidr);

            byte[] messageData = _memory.ReadBytes(messagePtr, size);

            KSession session = _process.HandleTable.GetObject<KSession>(handle);

            if (session != null)
            {
                //Process.Scheduler.Suspend(CurrThread);

                _system.CriticalSectionLock.Lock();

                KThread currentThread = _system.Scheduler.GetCurrentThread();

                currentThread.SignaledObj   = null;
                currentThread.ObjSyncResult = 0;

                currentThread.Reschedule(ThreadSchedState.Paused);

                IpcMessage message = new IpcMessage(messageData, messagePtr);

                ThreadPool.QueueUserWorkItem(ProcessIpcRequest, new HleIpcMessage(
                    currentThread,
                    session,
                    message,
                    messagePtr));

                _system.CriticalSectionLock.Unlock();

                threadState.X0 = (ulong)currentThread.ObjSyncResult;
            }
            else
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid session handle 0x{handle:x8}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);
            }
        }

        private void ProcessIpcRequest(object state)
        {
            HleIpcMessage ipcMessage = (HleIpcMessage)state;

            ipcMessage.Thread.ObjSyncResult = (int)IpcHandler.IpcCall(
                _device,
                _process,
                _memory,
                ipcMessage.Session,
                ipcMessage.Message,
                ipcMessage.MessagePtr);

            ipcMessage.Thread.Reschedule(ThreadSchedState.Running);
        }

        private void SvcBreak(AThreadState threadState)
        {
            long reason  = (long)threadState.X0;
            long unknown = (long)threadState.X1;
            long info    = (long)threadState.X2;

            if ((reason & (1 << 31)) == 0)
            {
                _process.PrintStackTrace(threadState);

                throw new GuestBrokeExecutionException();
            }
            else
            {
                Logger.PrintInfo(LogClass.KernelSvc, "Debugger triggered");
                _process.PrintStackTrace(threadState);
            }
        }

        private void SvcOutputDebugString(AThreadState threadState)
        {
            long position = (long)threadState.X0;
            long size     = (long)threadState.X1;

            string str = AMemoryHelper.ReadAsciiString(_memory, position, size);

            Logger.PrintWarning(LogClass.KernelSvc, str);

            threadState.X0 = 0;
        }

        private void SvcGetInfo(AThreadState threadState)
        {
            long stackPtr = (long)threadState.X0;
            int  infoType =  (int)threadState.X1;
            long handle   = (long)threadState.X2;
            int  infoId   =  (int)threadState.X3;

            //Fail for info not available on older Kernel versions.
            if (infoType == 18 ||
                infoType == 19 ||
                infoType == 20 ||
                infoType == 21 ||
                infoType == 22)
            {
                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidEnumValue);

                return;
            }

            switch (infoType)
            {
                case 0:
                    threadState.X1 = AllowedCpuIdBitmask;
                    break;

                case 2:
                    threadState.X1 = (ulong)_process.MemoryManager.MapRegionStart;
                    break;

                case 3:
                    threadState.X1 = (ulong)_process.MemoryManager.MapRegionEnd -
                                     (ulong)_process.MemoryManager.MapRegionStart;
                    break;

                case 4:
                    threadState.X1 = (ulong)_process.MemoryManager.HeapRegionStart;
                    break;

                case 5:
                    threadState.X1 = (ulong)_process.MemoryManager.HeapRegionEnd -
                                     (ulong)_process.MemoryManager.HeapRegionStart;
                    break;

                case 6:
                    threadState.X1 = (ulong)_process.Device.Memory.Allocator.TotalAvailableSize;
                    break;

                case 7:
                    threadState.X1 = (ulong)_process.Device.Memory.Allocator.TotalUsedSize;
                    break;

                case 8:
                    threadState.X1 = EnableProcessDebugging ? 1 : 0;
                    break;

                case 11:
                    threadState.X1 = (ulong)_rng.Next() + ((ulong)_rng.Next() << 32);
                    break;

                case 12:
                    threadState.X1 = (ulong)_process.MemoryManager.AddrSpaceStart;
                    break;

                case 13:
                    threadState.X1 = (ulong)_process.MemoryManager.AddrSpaceEnd -
                                     (ulong)_process.MemoryManager.AddrSpaceStart;
                    break;

                case 14:
                    threadState.X1 = (ulong)_process.MemoryManager.NewMapRegionStart;
                    break;

                case 15:
                    threadState.X1 = (ulong)_process.MemoryManager.NewMapRegionEnd -
                                     (ulong)_process.MemoryManager.NewMapRegionStart;
                    break;

                case 16:
                    threadState.X1 = (ulong)(_process.MetaData?.SystemResourceSize ?? 0);
                    break;

                case 17:
                    threadState.X1 = (ulong)_process.MemoryManager.PersonalMmHeapUsage;
                    break;

                default:
                    _process.PrintStackTrace(threadState);

                    throw new NotImplementedException($"SvcGetInfo: {infoType} 0x{handle:x8} {infoId}");
            }

            threadState.X0 = 0;
        }

        private void CreateEvent64(AThreadState state)
        {
            KernelResult result = CreateEvent(out int wEventHandle, out int rEventHandle);

            state.X0 = (ulong)result;
            state.X1 = (ulong)wEventHandle;
            state.X2 = (ulong)rEventHandle;
        }

        private KernelResult CreateEvent(out int wEventHandle, out int rEventHandle)
        {
            KEvent Event = new KEvent(_system);

            KernelResult result = _process.HandleTable.GenerateHandle(Event.WritableEvent, out wEventHandle);

            if (result == KernelResult.Success)
            {
                result = _process.HandleTable.GenerateHandle(Event.ReadableEvent, out rEventHandle);

                if (result != KernelResult.Success) _process.HandleTable.CloseHandle(wEventHandle);
            }
            else
            {
                rEventHandle = 0;
            }

            return result;
        }
    }
}
