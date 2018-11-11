using ChocolArm64.Memory;
using ChocolArm64.State;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services;
using System;
using System.Threading;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    partial class SvcHandler
    {
        private const int AllowedCpuIdBitmask = 0b1111;

        private const bool EnableProcessDebugging = false;

        private void SvcExitProcess(CpuThreadState ThreadState)
        {
            System.Scheduler.GetCurrentProcess().Terminate();
        }

        private void SignalEvent64(CpuThreadState ThreadState)
        {
            ThreadState.X0 = (ulong)SignalEvent((int)ThreadState.X0);
        }

        private KernelResult SignalEvent(int Handle)
        {
            KWritableEvent WritableEvent = Process.HandleTable.GetObject<KWritableEvent>(Handle);

            KernelResult Result;

            if (WritableEvent != null)
            {
                WritableEvent.Signal();

                Result = KernelResult.Success;
            }
            else
            {
                Result = KernelResult.InvalidHandle;
            }

            if (Result != KernelResult.Success)
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Operation failed with error: " + Result + "!");
            }

            return Result;
        }

        private void ClearEvent64(CpuThreadState ThreadState)
        {
            ThreadState.X0 = (ulong)ClearEvent((int)ThreadState.X0);
        }

        private KernelResult ClearEvent(int Handle)
        {
            KernelResult Result;

            KWritableEvent WritableEvent = Process.HandleTable.GetObject<KWritableEvent>(Handle);

            if (WritableEvent == null)
            {
                KReadableEvent ReadableEvent = Process.HandleTable.GetObject<KReadableEvent>(Handle);

                Result = ReadableEvent?.Clear() ?? KernelResult.InvalidHandle;
            }
            else
            {
                Result = WritableEvent.Clear();
            }

            if (Result != KernelResult.Success)
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Operation failed with error: " + Result + "!");
            }

            return Result;
        }

        private void SvcCloseHandle(CpuThreadState ThreadState)
        {
            int Handle = (int)ThreadState.X0;

            object Obj = Process.HandleTable.GetObject<object>(Handle);

            Process.HandleTable.CloseHandle(Handle);

            if (Obj == null)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid handle 0x{Handle:x8}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                return;
            }

            if (Obj is KSession Session)
            {
                Session.Dispose();
            }
            else if (Obj is KTransferMemory TransferMemory)
            {
                Process.MemoryManager.ResetTransferMemory(
                    TransferMemory.Position,
                    TransferMemory.Size);
            }

            ThreadState.X0 = 0;
        }

        private void ResetSignal64(CpuThreadState ThreadState)
        {
            ThreadState.X0 = (ulong)ResetSignal((int)ThreadState.X0);
        }

        private KernelResult ResetSignal(int Handle)
        {
            KProcess CurrentProcess = System.Scheduler.GetCurrentProcess();

            KReadableEvent ReadableEvent = CurrentProcess.HandleTable.GetObject<KReadableEvent>(Handle);

            KernelResult Result;

            if (ReadableEvent != null)
            {
                Result = ReadableEvent.ClearIfSignaled();
            }
            else
            {
                KProcess Process = CurrentProcess.HandleTable.GetKProcess(Handle);

                if (Process != null)
                {
                    Result = Process.ClearIfNotExited();
                }
                else
                {
                    Result = KernelResult.InvalidHandle;
                }
            }

            if (Result == KernelResult.InvalidState)
            {
                Logger.PrintDebug(LogClass.KernelSvc, "Operation failed with error: " + Result + "!");
            }
            else if (Result != KernelResult.Success)
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Operation failed with error: " + Result + "!");
            }

            return Result;
        }

        private void SvcGetSystemTick(CpuThreadState ThreadState)
        {
            ThreadState.X0 = ThreadState.CntpctEl0;
        }

        private void SvcConnectToNamedPort(CpuThreadState ThreadState)
        {
            long StackPtr = (long)ThreadState.X0;
            long NamePtr  = (long)ThreadState.X1;

            string Name = MemoryHelper.ReadAsciiString(Memory, NamePtr, 8);

            //TODO: Validate that app has perms to access the service, and that the service
            //actually exists, return error codes otherwise.
            KSession Session = new KSession(ServiceFactory.MakeService(System, Name), Name);

            if (Process.HandleTable.GenerateHandle(Session, out int Handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            ThreadState.X0 = 0;
            ThreadState.X1 = (uint)Handle;
        }

        private void SvcSendSyncRequest(CpuThreadState ThreadState)
        {
            SendSyncRequest(ThreadState, ThreadState.Tpidr, 0x100, (int)ThreadState.X0);
        }

        private void SvcSendSyncRequestWithUserBuffer(CpuThreadState ThreadState)
        {
            SendSyncRequest(
                      ThreadState,
                (long)ThreadState.X0,
                (long)ThreadState.X1,
                 (int)ThreadState.X2);
        }

        private void SendSyncRequest(CpuThreadState ThreadState, long MessagePtr, long Size, int Handle)
        {
            byte[] MessageData = Memory.ReadBytes(MessagePtr, Size);

            KSession Session = Process.HandleTable.GetObject<KSession>(Handle);

            if (Session != null)
            {
                System.CriticalSection.Enter();

                KThread CurrentThread = System.Scheduler.GetCurrentThread();

                CurrentThread.SignaledObj   = null;
                CurrentThread.ObjSyncResult = 0;

                CurrentThread.Reschedule(ThreadSchedState.Paused);

                IpcMessage Message = new IpcMessage(MessageData, MessagePtr);

                ThreadPool.QueueUserWorkItem(ProcessIpcRequest, new HleIpcMessage(
                    CurrentThread,
                    Session,
                    Message,
                    MessagePtr));

                System.CriticalSection.Leave();

                ThreadState.X0 = (ulong)CurrentThread.ObjSyncResult;
            }
            else
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid session handle 0x{Handle:x8}!");

                ThreadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);
            }
        }

        private void ProcessIpcRequest(object State)
        {
            HleIpcMessage IpcMessage = (HleIpcMessage)State;

            IpcMessage.Thread.ObjSyncResult = (int)IpcHandler.IpcCall(
                Device,
                Process,
                Memory,
                IpcMessage.Session,
                IpcMessage.Message,
                IpcMessage.MessagePtr);

            IpcMessage.Thread.Reschedule(ThreadSchedState.Running);
        }

        private void SvcBreak(CpuThreadState ThreadState)
        {
            long Reason  = (long)ThreadState.X0;
            long Unknown = (long)ThreadState.X1;
            long Info    = (long)ThreadState.X2;

            KThread CurrentThread = System.Scheduler.GetCurrentThread();

            if ((Reason & (1 << 31)) == 0)
            {
                CurrentThread.PrintGuestStackTrace();

                throw new GuestBrokeExecutionException();
            }
            else
            {
                Logger.PrintInfo(LogClass.KernelSvc, "Debugger triggered.");

                CurrentThread.PrintGuestStackTrace();
            }
        }

        private void SvcOutputDebugString(CpuThreadState ThreadState)
        {
            long Position = (long)ThreadState.X0;
            long Size     = (long)ThreadState.X1;

            string Str = MemoryHelper.ReadAsciiString(Memory, Position, Size);

            Logger.PrintWarning(LogClass.KernelSvc, Str);

            ThreadState.X0 = 0;
        }

        private void GetInfo64(CpuThreadState ThreadState)
        {
            long StackPtr = (long)ThreadState.X0;
            int  Id       =  (int)ThreadState.X1;
            int  Handle   =  (int)ThreadState.X2;
            long SubId    = (long)ThreadState.X3;

            KernelResult Result = GetInfo(Id, Handle, SubId, out long Value);

            ThreadState.X0 = (ulong)Result;
            ThreadState.X1 = (ulong)Value;
        }

        private KernelResult GetInfo(int Id, int Handle, long SubId, out long Value)
        {
            Value = 0;

            switch (Id)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 20:
                case 21:
                case 22:
                {
                    if (SubId != 0)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    KProcess CurrentProcess = System.Scheduler.GetCurrentProcess();

                    KProcess Process = CurrentProcess.HandleTable.GetKProcess(Handle);

                    if (Process == null)
                    {
                        return KernelResult.InvalidHandle;
                    }

                    switch (Id)
                    {
                        case 0: Value = Process.Capabilities.AllowedCpuCoresMask;    break;
                        case 1: Value = Process.Capabilities.AllowedThreadPriosMask; break;

                        case 2: Value = Process.MemoryManager.AliasRegionStart; break;
                        case 3: Value = Process.MemoryManager.AliasRegionEnd -
                                        Process.MemoryManager.AliasRegionStart; break;

                        case 4: Value = Process.MemoryManager.HeapRegionStart; break;
                        case 5: Value = Process.MemoryManager.HeapRegionEnd -
                                        Process.MemoryManager.HeapRegionStart; break;

                        case 6: Value = Process.GetMemoryCapacity(); break;

                        case 7: Value = Process.GetMemoryUsage(); break;

                        case 12: Value = Process.MemoryManager.GetAddrSpaceBaseAddr(); break;

                        case 13: Value = Process.MemoryManager.GetAddrSpaceSize(); break;

                        case 14: Value = Process.MemoryManager.StackRegionStart; break;
                        case 15: Value = Process.MemoryManager.StackRegionEnd -
                                         Process.MemoryManager.StackRegionStart; break;

                        case 16: Value = Process.PersonalMmHeapPagesCount * KMemoryManager.PageSize; break;

                        case 17:
                            if (Process.PersonalMmHeapPagesCount != 0)
                            {
                                Value = Process.MemoryManager.GetMmUsedPages() * KMemoryManager.PageSize;
                            }

                            break;

                        case 18: Value = Process.TitleId; break;

                        case 20: Value = Process.UserExceptionContextAddress; break;

                        case 21: Value = Process.GetMemoryCapacityWithoutPersonalMmHeap(); break;

                        case 22: Value = Process.GetMemoryUsageWithoutPersonalMmHeap(); break;
                    }

                    break;
                }

                case 8:
                {
                    if (Handle != 0)
                    {
                        return KernelResult.InvalidHandle;
                    }

                    if (SubId != 0)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    Value = System.Scheduler.GetCurrentProcess().Debug ? 1 : 0;

                    break;
                }

                case 9:
                {
                    if (Handle != 0)
                    {
                        return KernelResult.InvalidHandle;
                    }

                    if (SubId != 0)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    KProcess CurrentProcess = System.Scheduler.GetCurrentProcess();

                    if (CurrentProcess.ResourceLimit != null)
                    {
                        KHandleTable   HandleTable   = CurrentProcess.HandleTable;
                        KResourceLimit ResourceLimit = CurrentProcess.ResourceLimit;

                        KernelResult Result = HandleTable.GenerateHandle(ResourceLimit, out int ResLimHandle);

                        if (Result != KernelResult.Success)
                        {
                            return Result;
                        }

                        Value = (uint)ResLimHandle;
                    }

                    break;
                }

                case 10:
                {
                    if (Handle != 0)
                    {
                        return KernelResult.InvalidHandle;
                    }

                    int CurrentCore = System.Scheduler.GetCurrentThread().CurrentCore;

                    if (SubId != -1 && SubId != CurrentCore)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    Value = System.Scheduler.CoreContexts[CurrentCore].TotalIdleTimeTicks;

                    break;
                }

                case 11:
                {
                    if (Handle != 0)
                    {
                        return KernelResult.InvalidHandle;
                    }

                    if ((ulong)SubId > 3)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    Value = System.Scheduler.GetCurrentProcess().RandomEntropy[SubId];

                    break;
                }

                case unchecked((int)0xf0000002u):
                {
                    if (SubId < -1 || SubId > 3)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    KThread Thread = System.Scheduler.GetCurrentProcess().HandleTable.GetKThread(Handle);

                    if (Thread == null)
                    {
                        return KernelResult.InvalidHandle;
                    }

                    KThread CurrentThread = System.Scheduler.GetCurrentThread();

                    int CurrentCore = CurrentThread.CurrentCore;

                    if (SubId != -1 && SubId != CurrentCore)
                    {
                        return KernelResult.Success;
                    }

                    KCoreContext CoreContext = System.Scheduler.CoreContexts[CurrentCore];

                    long TimeDelta = PerformanceCounter.ElapsedMilliseconds - CoreContext.LastContextSwitchTime;

                    if (SubId != -1)
                    {
                        Value = KTimeManager.ConvertMillisecondsToTicks(TimeDelta);
                    }
                    else
                    {
                        long TotalTimeRunning = Thread.TotalTimeRunning;

                        if (Thread == CurrentThread)
                        {
                            TotalTimeRunning += TimeDelta;
                        }

                        Value = KTimeManager.ConvertMillisecondsToTicks(TotalTimeRunning);
                    }

                    break;
                }

                default: return KernelResult.InvalidEnumValue;
            }

            return KernelResult.Success;
        }

        private void CreateEvent64(CpuThreadState State)
        {
            KernelResult Result = CreateEvent(out int WEventHandle, out int REventHandle);

            State.X0 = (ulong)Result;
            State.X1 = (ulong)WEventHandle;
            State.X2 = (ulong)REventHandle;
        }

        private KernelResult CreateEvent(out int WEventHandle, out int REventHandle)
        {
            KEvent Event = new KEvent(System);

            KernelResult Result = Process.HandleTable.GenerateHandle(Event.WritableEvent, out WEventHandle);

            if (Result == KernelResult.Success)
            {
                Result = Process.HandleTable.GenerateHandle(Event.ReadableEvent, out REventHandle);

                if (Result != KernelResult.Success)
                {
                    Process.HandleTable.CloseHandle(WEventHandle);
                }
            }
            else
            {
                REventHandle = 0;
            }

            return Result;
        }
    }
}
