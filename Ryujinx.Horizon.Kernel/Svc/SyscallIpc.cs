using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Ipc;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using System;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result ConnectToNamedPort(ulong namePtr, out int handle)
        {
            handle = 0;

            if (!KernelTransfer.UserToKernelString(_context, namePtr, 12, out string name))
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            return CheckResult(ConnectToNamedPort(name, out handle));
        }

        public Result ConnectToNamedPort(string name, out int handle)
        {
            handle = 0;

            if (name.Length > 11)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KAutoObject autoObj = KAutoObject.FindNamedObject(_context, name);

            if (!(autoObj is KClientPort clientPort))
            {
                return CheckResult(KernelResult.NotFound);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = currentProcess.HandleTable.ReserveHandle(out handle);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            result = clientPort.Connect(out KClientSession clientSession);

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CancelHandleReservation(handle);

                return CheckResult(result);
            }

            currentProcess.HandleTable.SetReservedHandleObj(handle, clientSession);

            clientSession.DecrementReferenceCount();

            return CheckResult(result);
        }

        public Result SendSyncRequest(int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KClientSession session = currentProcess.HandleTable.GetObject<KClientSession>(handle);

            if (session == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            return CheckResult(session.SendSyncRequest());
        }

        public Result SendSyncRequestWithUserBuffer(ulong messagePtr, ulong messageSize, int handle)
        {
            if (!PageAligned(messagePtr))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(messageSize) || messageSize == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (messagePtr + messageSize <= messagePtr)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = currentProcess.MemoryManager.BorrowIpcBuffer(messagePtr, messageSize);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            KClientSession session = currentProcess.HandleTable.GetObject<KClientSession>(handle);

            if (session == null)
            {
                result = KernelResult.InvalidHandle;
            }
            else
            {
                result = session.SendSyncRequest(messagePtr, messageSize);
            }

            Result result2 = currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

            if (result == Result.Success)
            {
                result = result2;
            }

            return CheckResult(result);
        }

        public Result SendAsyncRequestWithUserBuffer(ulong messagePtr, ulong messageSize, int handle, out int doneEventHandle)
        {
            doneEventHandle = 0;

            if (!PageAligned(messagePtr))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(messageSize) || messageSize == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (messagePtr + messageSize <= messagePtr)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = currentProcess.MemoryManager.BorrowIpcBuffer(messagePtr, messageSize);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.Event, 1))
            {
                currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

                return CheckResult(KernelResult.ResLimitExceeded);
            }

            KClientSession session = currentProcess.HandleTable.GetObject<KClientSession>(handle);

            if (session == null)
            {
                result = KernelResult.InvalidHandle;
            }
            else
            {
                KEvent doneEvent = new KEvent(_context);

                result = currentProcess.HandleTable.GenerateHandle(doneEvent.ReadableEvent, out doneEventHandle);

                if (result == Result.Success)
                {
                    result = session.SendAsyncRequest(doneEvent.WritableEvent, messagePtr, messageSize);

                    if (result != Result.Success)
                    {
                        currentProcess.HandleTable.CloseHandle(doneEventHandle);
                    }
                }
            }

            if (result != Result.Success)
            {
                resourceLimit?.Release(LimitableResource.Event, 1);

                currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);
            }

            return CheckResult(result);
        }

        public Result CreateSession(
            bool isLight,
            ulong namePtr,
            out int serverSessionHandle,
            out int clientSessionHandle)
        {
            serverSessionHandle = 0;
            clientSessionHandle = 0;

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.Session, 1))
            {
                return CheckResult(KernelResult.ResLimitExceeded);
            }

            Result result;

            if (isLight)
            {
                KLightSession session = new KLightSession(_context);

                result = currentProcess.HandleTable.GenerateHandle(session.ServerSession, out serverSessionHandle);

                if (result == Result.Success)
                {
                    result = currentProcess.HandleTable.GenerateHandle(session.ClientSession, out clientSessionHandle);

                    if (result != Result.Success)
                    {
                        currentProcess.HandleTable.CloseHandle(serverSessionHandle);

                        serverSessionHandle = 0;
                    }
                }

                session.ServerSession.DecrementReferenceCount();
                session.ClientSession.DecrementReferenceCount();
            }
            else
            {
                KSession session = new KSession(_context);

                result = currentProcess.HandleTable.GenerateHandle(session.ServerSession, out serverSessionHandle);

                if (result == Result.Success)
                {
                    result = currentProcess.HandleTable.GenerateHandle(session.ClientSession, out clientSessionHandle);

                    if (result != Result.Success)
                    {
                        currentProcess.HandleTable.CloseHandle(serverSessionHandle);

                        serverSessionHandle = 0;
                    }
                }

                session.ServerSession.DecrementReferenceCount();
                session.ClientSession.DecrementReferenceCount();
            }

            return CheckResult(result);
        }

        public Result AcceptSession(int portHandle, out int sessionHandle)
        {
            sessionHandle = 0;

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KServerPort serverPort = currentProcess.HandleTable.GetObject<KServerPort>(portHandle);

            if (serverPort == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            Result result = currentProcess.HandleTable.ReserveHandle(out int handle);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            KAutoObject session;

            if (serverPort.IsLight)
            {
                session = serverPort.AcceptIncomingLightConnection();
            }
            else
            {
                session = serverPort.AcceptIncomingConnection();
            }

            if (session != null)
            {
                currentProcess.HandleTable.SetReservedHandleObj(handle, session);

                session.DecrementReferenceCount();

                sessionHandle = handle;

                result = Result.Success;
            }
            else
            {
                currentProcess.HandleTable.CancelHandleReservation(handle);

                result = KernelResult.NotFound;
            }

            return CheckResult(result);
        }

        public Result ReplyAndReceive(
            ulong handlesPtr,
            int handlesCount,
            int replyTargetHandle,
            long timeout,
            out int handleIndex)
        {
            handleIndex = 0;

            if ((uint)handlesCount > 0x40)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            ulong copySize = (ulong)((long)handlesCount * 4);

            if (!currentProcess.MemoryManager.InsideAddrSpace(handlesPtr, copySize))
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            if (handlesPtr + copySize < handlesPtr)
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            int[] handles = new int[handlesCount];

            if (!KernelTransfer.UserToKernelInt32Array(_context, handlesPtr, handles))
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            return CheckResult(ReplyAndReceive(handles, replyTargetHandle, timeout, out handleIndex));
        }

        public Result ReplyAndReceive(ReadOnlySpan<int> handles, int replyTargetHandle, long timeout, out int handleIndex)
        {
            handleIndex = 0;

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KSynchronizationObject[] syncObjs = new KSynchronizationObject[handles.Length];

            for (int index = 0; index < handles.Length; index++)
            {
                KSynchronizationObject obj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[index]);

                if (obj == null)
                {
                    return CheckResult(KernelResult.InvalidHandle);
                }

                syncObjs[index] = obj;
            }

            Result result = Result.Success;

            if (replyTargetHandle != 0)
            {
                KServerSession replyTarget = currentProcess.HandleTable.GetObject<KServerSession>(replyTargetHandle);

                if (replyTarget == null)
                {
                    result = KernelResult.InvalidHandle;
                }
                else
                {
                    result = replyTarget.Reply();
                }
            }

            if (result == Result.Success)
            {
                while ((result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex)) == Result.Success)
                {
                    KServerSession session = currentProcess.HandleTable.GetObject<KServerSession>(handles[handleIndex]);

                    if (session == null)
                    {
                        break;
                    }

                    if ((result = session.Receive()) != KernelResult.NotFound)
                    {
                        break;
                    }
                }
            }

            return CheckResult(result);
        }

        public Result ReplyAndReceiveWithUserBuffer(
            ulong handlesPtr,
            ulong messagePtr,
            ulong messageSize,
            int handlesCount,
            int replyTargetHandle,
            long timeout,
            out int handleIndex)
        {
            handleIndex = 0;

            if ((uint)handlesCount > 0x40)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            ulong copySize = (ulong)((long)handlesCount * 4);

            if (!currentProcess.MemoryManager.InsideAddrSpace(handlesPtr, copySize))
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            if (handlesPtr + copySize < handlesPtr)
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            Result result = currentProcess.MemoryManager.BorrowIpcBuffer(messagePtr, messageSize);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            int[] handles = new int[handlesCount];

            if (!KernelTransfer.UserToKernelInt32Array(_context, handlesPtr, handles))
            {
                currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

                return CheckResult(KernelResult.UserCopyFailed);
            }

            KSynchronizationObject[] syncObjs = new KSynchronizationObject[handlesCount];

            for (int index = 0; index < handlesCount; index++)
            {
                KSynchronizationObject obj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[index]);

                if (obj == null)
                {
                    currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

                    return CheckResult(KernelResult.InvalidHandle);
                }

                syncObjs[index] = obj;
            }

            if (replyTargetHandle != 0)
            {
                KServerSession replyTarget = currentProcess.HandleTable.GetObject<KServerSession>(replyTargetHandle);

                if (replyTarget == null)
                {
                    result = KernelResult.InvalidHandle;
                }
                else
                {
                    result = replyTarget.Reply(messagePtr, messageSize);
                }
            }

            if (result == Result.Success)
            {
                while ((result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex)) == Result.Success)
                {
                    KServerSession session = currentProcess.HandleTable.GetObject<KServerSession>(handles[handleIndex]);

                    if (session == null)
                    {
                        break;
                    }

                    if ((result = session.Receive(messagePtr, messageSize)) != KernelResult.NotFound)
                    {
                        break;
                    }
                }
            }

            currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

            return CheckResult(result);
        }

        public Result CreatePort(
            int maxSessions,
            bool isLight,
            ulong namePtr,
            out int serverPortHandle,
            out int clientPortHandle)
        {
            serverPortHandle = clientPortHandle = 0;

            if (maxSessions < 1)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KPort port = new KPort(_context, maxSessions, isLight, (long)namePtr);

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = currentProcess.HandleTable.GenerateHandle(port.ClientPort, out clientPortHandle);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            result = currentProcess.HandleTable.GenerateHandle(port.ServerPort, out serverPortHandle);

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CloseHandle(clientPortHandle);
            }

            return CheckResult(result);
        }

        public Result ManageNamedPort(ulong namePtr, int maxSessions, out int handle)
        {
            handle = 0;

            if (!KernelTransfer.UserToKernelString(_context, namePtr, 12, out string name))
            {
                return CheckResult(KernelResult.UserCopyFailed);
            }

            if (name.Length > 11)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            return CheckResult(ManageNamedPort(name, maxSessions, out handle));
        }

        public Result ManageNamedPort(string name, int maxSessions, out int handle)
        {
            handle = 0;

            if (maxSessions < 0)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            if (maxSessions == 0)
            {
                return CheckResult(KAutoObject.RemoveName(_context, name));
            }

            KPort port = new KPort(_context, maxSessions, false, 0);

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = currentProcess.HandleTable.GenerateHandle(port.ServerPort, out handle);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            result = port.ClientPort.SetName(name);

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CloseHandle(handle);
            }

            return CheckResult(result);
        }

        public Result ConnectToPort(int clientPortHandle, out int clientSessionHandle)
        {
            clientSessionHandle = 0;

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KClientPort clientPort = currentProcess.HandleTable.GetObject<KClientPort>(clientPortHandle);

            if (clientPort == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            Result result = currentProcess.HandleTable.ReserveHandle(out int handle);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            KAutoObject session;

            if (clientPort.IsLight)
            {
                result = clientPort.ConnectLight(out KLightClientSession clientSession);

                session = clientSession;
            }
            else
            {
                result = clientPort.Connect(out KClientSession clientSession);

                session = clientSession;
            }

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CancelHandleReservation(handle);

                return CheckResult(result);
            }

            currentProcess.HandleTable.SetReservedHandleObj(handle, session);

            session.DecrementReferenceCount();

            clientSessionHandle = handle;

            return CheckResult(result);
        }
    }
}
