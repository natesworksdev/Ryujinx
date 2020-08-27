using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Ipc;
using Ryujinx.Horizon.Kernel.Memory;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public class Syscall
    {
        private readonly KernelContextInternal _context;

        internal Syscall(KernelContextInternal context)
        {
            _context = context;
        }

        private Result CheckResult(Result result, [CallerMemberName] string svcName = null)
        {
            _context.Scheduler.GetCurrentThread().HandlePostSyscall();

            // Filter out some errors that are expected to occur under normal operation,
            // this avoids false warnings.
            if (result.IsFailure &&
                result != KernelResult.TimedOut &&
                result != KernelResult.Cancelled &&
                result != KernelResult.PortRemoteClosed &&
                result != KernelResult.InvalidState)
            {
                Logger.Warning?.Print(LogClass.KernelSvc, $"{svcName} returned error {result}.");
            }
            else
            {
                Logger.Debug?.Print(LogClass.KernelSvc, $"{svcName} returned result {result}.");
            }

            return result;
        }

        // Process

        public Result GetProcessId(int handle, out long pid)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess process = currentProcess.HandleTable.GetKProcess(handle);

            if (process == null)
            {
                KThread thread = currentProcess.HandleTable.GetKThread(handle);

                if (thread != null)
                {
                    process = thread.Owner;
                }

                // TODO: KDebugEvent.
            }

            pid = process?.Pid ?? 0;

            return CheckResult(process != null ? Result.Success : KernelResult.InvalidHandle);
        }

        public Result CreateProcess(
            ProcessCreationInfo info,
            ReadOnlySpan<int> capabilities,
            out int handle,
            IProcessContextFactory contextFactory,
            ThreadStart customThreadStart = null)
        {
            handle = 0;

            if ((info.Flags & ~ProcessCreationFlags.All) != 0)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            // TODO: Address space check.

            if ((info.Flags & ProcessCreationFlags.PoolPartitionMask) > ProcessCreationFlags.PoolPartitionSystemNonSecure)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            if ((info.CodeAddress & 0x1fffff) != 0)
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (info.CodePagesCount < 0 || info.SystemResourcePagesCount < 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (info.Flags.HasFlag(ProcessCreationFlags.OptimizeMemoryAllocation) &&
                !info.Flags.HasFlag(ProcessCreationFlags.IsApplication))
            {
                return CheckResult(KernelResult.InvalidThread);
            }

            KHandleTable handleTable = _context.Scheduler.GetCurrentProcess().HandleTable;

            KProcess process = new KProcess(_context);

            using var _ = new OnScopeExit(process.DecrementReferenceCount);

            KResourceLimit resourceLimit;

            if (info.ResourceLimitHandle != 0)
            {
                resourceLimit = handleTable.GetObject<KResourceLimit>(info.ResourceLimitHandle);

                if (resourceLimit == null)
                {
                    return CheckResult(KernelResult.InvalidHandle);
                }
            }
            else
            {
                resourceLimit = _context.ResourceLimit;
            }

            KMemoryRegion memRegion = (info.Flags & ProcessCreationFlags.PoolPartitionMask) switch
            {
                ProcessCreationFlags.PoolPartitionApplication => KMemoryRegion.Application,
                ProcessCreationFlags.PoolPartitionApplet => KMemoryRegion.Applet,
                ProcessCreationFlags.PoolPartitionSystem => KMemoryRegion.Service,
                ProcessCreationFlags.PoolPartitionSystemNonSecure => KMemoryRegion.NvServices,
                _ => KMemoryRegion.NvServices
            };

            Result result = process.Initialize(
                info,
                capabilities,
                resourceLimit,
                memRegion,
                contextFactory,
                customThreadStart);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            _context.Processes.TryAdd(process.Pid, process);

            return CheckResult(handleTable.GenerateHandle(process, out handle));
        }

        public Result StartProcess(int handle, int priority, int cpuCore, ulong mainThreadStackSize)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KProcess>(handle);

            if (process == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if ((uint)cpuCore >= KScheduler.CpuCoresCount || !process.IsCpuCoreAllowed(cpuCore))
            {
                return CheckResult(KernelResult.InvalidCpuCore);
            }

            if ((uint)priority >= KScheduler.PrioritiesCount || !process.IsPriorityAllowed(priority))
            {
                return CheckResult(KernelResult.InvalidPriority);
            }

            process.DefaultCpuCore = cpuCore;

            Result result = process.Start(priority, mainThreadStackSize);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            process.IncrementReferenceCount();

            return CheckResult(Result.Success);
        }

        // IPC

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

        // Memory

        public Result SetHeapSize(ulong size, out ulong position)
        {
            if ((size & 0xfffffffe001fffff) != 0)
            {
                position = 0;

                return CheckResult(KernelResult.InvalidSize);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.SetHeapSize(size, out position));
        }

        public Result SetMemoryAttribute(
            ulong position,
            ulong size,
            KMemoryAttribute attributeMask,
            KMemoryAttribute attributeValue)
        {
            if (!PageAligned(position))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            KMemoryAttribute attributes = attributeMask | attributeValue;

            if (attributes != attributeMask ||
               (attributes | KMemoryAttribute.Uncached) != KMemoryAttribute.Uncached)
            {
                return CheckResult(KernelResult.InvalidCombination);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            Result result = process.MemoryManager.SetMemoryAttribute(
                position,
                size,
                attributeMask,
                attributeValue);

            return CheckResult(result);
        }

        public Result MapMemory(ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(src | dst))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (src + size <= src || dst + size <= dst)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (currentProcess.MemoryManager.OutsideStackRegion(dst, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(dst, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.Map(dst, src, size));
        }

        public Result UnmapMemory(ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(src | dst))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (src + size <= src || dst + size <= dst)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (currentProcess.MemoryManager.OutsideStackRegion(dst, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(dst, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.Unmap(dst, src, size));
        }

        public Result QueryMemory(ulong infoPtr, ulong pageInfoPtr, ulong address)
        {
            return CheckResult(QueryProcessMemory(infoPtr, pageInfoPtr, KHandleTable.SelfProcessHandle, address));
        }

        public Result QueryMemory(out MemoryInfo info, ulong address)
        {
            return CheckResult(QueryProcessMemory(out info, KHandleTable.SelfProcessHandle, address));
        }

        public Result MapSharedMemory(int handle, ulong address, ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if ((permission | KMemoryPermission.Write) != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KSharedMemory sharedMemory = currentProcess.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return sharedMemory.MapIntoProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess,
                permission);
        }

        public Result UnmapSharedMemory(int handle, ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KSharedMemory sharedMemory = currentProcess.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return sharedMemory.UnmapFromProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess);
        }

        public Result CreateTransferMemory(ulong address, ulong size, KMemoryPermission permission, out int handle)
        {
            handle = 0;

            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (permission > KMemoryPermission.ReadAndWrite || permission == KMemoryPermission.Write)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KResourceLimit resourceLimit = process.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.TransferMemory, 1))
            {
                return CheckResult(KernelResult.ResLimitExceeded);
            }

            void CleanUpForError()
            {
                resourceLimit?.Release(LimitableResource.TransferMemory, 1);
            }

            if (!process.MemoryManager.InsideAddrSpace(address, size))
            {
                CleanUpForError();

                return CheckResult(KernelResult.InvalidMemState);
            }

            KTransferMemory transferMemory = new KTransferMemory(_context);

            Result result = transferMemory.Initialize(address, size, permission);

            if (result != Result.Success)
            {
                CleanUpForError();

                return CheckResult(result);
            }

            result = process.HandleTable.GenerateHandle(transferMemory, out handle);

            transferMemory.DecrementReferenceCount();

            return CheckResult(result);
        }

        public Result MapPhysicalMemory(ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if ((currentProcess.PersonalMmHeapPagesCount & 0xfffffffffffff) == 0)
            {
                return CheckResult(KernelResult.InvalidState);
            }

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size) ||
                 currentProcess.MemoryManager.OutsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.MapPhysicalMemory(address, size));
        }

        public Result UnmapPhysicalMemory(ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if ((currentProcess.PersonalMmHeapPagesCount & 0xfffffffffffff) == 0)
            {
                return CheckResult(KernelResult.InvalidState);
            }

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size) ||
                 currentProcess.MemoryManager.OutsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.UnmapPhysicalMemory(address, size));
        }

        public Result CreateSharedMemory(out int handle, ulong size, KMemoryPermission ownerPermission, KMemoryPermission userPermission)
        {
            handle = 0;

            if (!PageAligned(size) || size == 0 || size >= 0x100000000UL)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (ownerPermission != KMemoryPermission.Read &&
                ownerPermission != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            if (userPermission != KMemoryPermission.DontCare &&
                userPermission != KMemoryPermission.Read &&
                userPermission != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KSharedMemory sharedMemory = new KSharedMemory(_context);

            using var _ = new OnScopeExit(sharedMemory.DecrementReferenceCount);

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = sharedMemory.Initialize(currentProcess, size, ownerPermission, userPermission);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            return CheckResult(currentProcess.HandleTable.GenerateHandle(sharedMemory, out handle));
        }

        public Result MapTransferMemory(int handle, ulong address, ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (size + address <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (permission != KMemoryPermission.None &&
                permission != KMemoryPermission.Read &&
                permission != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KTransferMemory transferMemory = currentProcess.HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (!currentProcess.MemoryManager.CanContain(address, size, KMemoryState.TransferMemoryIsolated))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return CheckResult(transferMemory.Map(address, size, permission));
        }

        public Result UnmapTransferMemory(int handle, ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (size + address <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KTransferMemory transferMemory = currentProcess.HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (!currentProcess.MemoryManager.CanContain(address, size, KMemoryState.TransferMemoryIsolated))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return CheckResult(transferMemory.Unmap(address, size));
        }

        public Result MapProcessMemory(ulong dst, int processHandle, ulong src, ulong size)
        {
            return CheckResult(MapOrUnmapProcessMemory(dst, processHandle, src, size, map: true));
        }

        public Result UnmapProcessMemory(ulong dst, int processHandle, ulong src, ulong size)
        {
            return CheckResult(MapOrUnmapProcessMemory(dst, processHandle, src, size, map: false));
        }

        private Result MapOrUnmapProcessMemory(ulong dst, int processHandle, ulong src, ulong size, bool map)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (size + dst <= dst || size + src <= src)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess sourceProcess = currentProcess.HandleTable.GetObject<KProcess>(processHandle);

            if (sourceProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (!sourceProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (!currentProcess.MemoryManager.CanContain(dst, size, KMemoryState.ProcessMemory))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KPageList pageList = new KPageList();

            Result result = sourceProcess.MemoryManager.GetPages(
                src,
                size / KMemoryManager.PageSize,
                KMemoryState.MapProcessAllowed,
                KMemoryState.MapProcessAllowed,
                KMemoryPermission.None,
                KMemoryPermission.None,
                KMemoryAttribute.Mask,
                KMemoryAttribute.None,
                pageList);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            if (map)
            {
                return CheckResult(currentProcess.MemoryManager.MapPages(dst, pageList, KMemoryState.ProcessMemory, KMemoryPermission.ReadAndWrite));
            }
            else
            {
                return CheckResult(currentProcess.MemoryManager.UnmapPages(dst, pageList, KMemoryState.ProcessMemory));
            }
        }

        public Result QueryProcessMemory(ulong infoPtr, ulong pageInfoPtr, int processHandle, ulong address)
        {
            Result result = QueryProcessMemory(out MemoryInfo info, processHandle, address);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            return KernelTransfer.KernelToUser(_context, infoPtr, info)
                ? Result.Success
                : KernelResult.InvalidMemState;
        }

        public Result QueryProcessMemory(out MemoryInfo info, int processHandle, ulong address)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess().HandleTable.GetKProcess(processHandle);

            if (process == null)
            {
                info = default;

                return CheckResult(KernelResult.InvalidHandle);
            }

            KMemoryInfo blockInfo = process.MemoryManager.QueryMemory(address);

            info = new MemoryInfo(
                blockInfo.Address,
                blockInfo.Size,
                (int)blockInfo.State & 0xff,
                (int)blockInfo.Attribute,
                (int)blockInfo.Permission,
                blockInfo.IpcRefCount,
                blockInfo.DeviceRefCount);

            return CheckResult(Result.Success);
        }

        public Result MapProcessCodeMemory(int processHandle, ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(processHandle);

            if (targetProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(dst, size) ||
                targetProcess.MemoryManager.OutsideAddrSpace(src, size) ||
                targetProcess.MemoryManager.InsideAliasRegion(dst, size) ||
                targetProcess.MemoryManager.InsideHeapRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            if (size + dst <= dst || size + src <= src)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            return CheckResult(targetProcess.MemoryManager.MapProcessCodeMemory(dst, src, size));
        }

        public Result UnmapProcessCodeMemory(int handle, ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(dst, size) ||
                targetProcess.MemoryManager.OutsideAddrSpace(src, size) ||
                targetProcess.MemoryManager.InsideAliasRegion(dst, size) ||
                targetProcess.MemoryManager.InsideHeapRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            if (size + dst <= dst || size + src <= src)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            return CheckResult(targetProcess.MemoryManager.UnmapProcessCodeMemory(dst, src, size));
        }

        public Result SetProcessMemoryPermission(int handle, ulong src, ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (permission != KMemoryPermission.None &&
                permission != KMemoryPermission.Read &&
                permission != KMemoryPermission.ReadAndWrite &&
                permission != KMemoryPermission.ReadAndExecute)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            return CheckResult(targetProcess.MemoryManager.SetProcessMemoryPermission(src, size, permission));
        }

        private static bool PageAligned(ulong address)
        {
            return(address & (KMemoryManager.PageSize - 1)) == 0;
        }

        // System

        public Result TerminateProcess(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            process = process.HandleTable.GetObject<KProcess>(handle);

            Result result;

            if (process != null)
            {
                if (process == _context.Scheduler.GetCurrentProcess())
                {
                    result = Result.Success;
                    process.DecrementToZeroWhileTerminatingCurrent();
                }
                else
                {
                    result = process.Terminate();
                    process.DecrementReferenceCount();
                }
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            return CheckResult(result);
        }

        public void ExitProcess()
        {
            _context.Scheduler.GetCurrentProcess().TerminateCurrentProcess();
        }

        public Result SignalEvent(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KWritableEvent writableEvent = process.HandleTable.GetObject<KWritableEvent>(handle);

            Result result;

            if (writableEvent != null)
            {
                writableEvent.Signal();

                result = Result.Success;
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            return CheckResult(result);
        }

        public Result ClearEvent(int handle)
        {
            Result result;

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KWritableEvent writableEvent = process.HandleTable.GetObject<KWritableEvent>(handle);

            if (writableEvent == null)
            {
                KReadableEvent readableEvent = process.HandleTable.GetObject<KReadableEvent>(handle);

                result = readableEvent?.Clear() ?? KernelResult.InvalidHandle;
            }
            else
            {
                result = writableEvent.Clear();
            }

            return CheckResult(result);
        }

        public Result CloseHandle(int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return CheckResult(currentProcess.HandleTable.CloseHandle(handle) ? Result.Success : KernelResult.InvalidHandle);
        }

        public Result ResetSignal(int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KReadableEvent readableEvent = currentProcess.HandleTable.GetObject<KReadableEvent>(handle);

            Result result;

            if (readableEvent != null)
            {
                result = readableEvent.ClearIfSignaled();
            }
            else
            {
                KProcess process = currentProcess.HandleTable.GetKProcess(handle);

                if (process != null)
                {
                    result = process.ClearIfNotExited();
                }
                else
                {
                    result = KernelResult.InvalidHandle;
                }
            }

            return CheckResult(result);
        }

        public ulong GetSystemTick()
        {
            ulong sytemTick = _context.Scheduler.GetCurrentThread().Context.Counter;

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);

            return sytemTick;
        }

        public void Break(ulong reason)
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            if ((reason & (1UL << 31)) == 0)
            {
                currentThread.PrintGuestStackTrace();

                // As the process is exiting, this is probably caused by emulation termination.
                if (currentThread.Owner.State == KProcessState.Exiting)
                {
                    return;
                }

                // TODO: Debug events.
                currentThread.Owner.TerminateCurrentProcess();

                // TODO: Proper exception.
                throw new Exception("Guest program broke execution");
            }
            else
            {
                Logger.Debug?.Print(LogClass.KernelSvc, "Debugger triggered.");
            }
        }

        public Result OutputDebugString(ulong strPtr, ulong size)
        {
            if (size == 0)
            {
                return CheckResult(Result.Success);
            }

            if (!KernelTransfer.UserToKernelString(_context, strPtr, size, out string debugString))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            Logger.Warning?.Print(LogClass.KernelSvc, debugString);

            return CheckResult(Result.Success);
        }

        public Result GetInfo(InfoType id, int handle, long subId, out ulong value)
        {
            value = 0;

            switch (id)
            {
                case InfoType.CoreMask:
                case InfoType.PriorityMask:
                case InfoType.AliasRegionAddress:
                case InfoType.AliasRegionSize:
                case InfoType.HeapRegionAddress:
                case InfoType.HeapRegionSize:
                case InfoType.TotalMemorySize:
                case InfoType.UsedMemorySize:
                case InfoType.AslrRegionAddress:
                case InfoType.AslrRegionSize:
                case InfoType.StackRegionAddress:
                case InfoType.StackRegionSize:
                case InfoType.SystemResourceSizeTotal:
                case InfoType.SystemResourceSizeUsed:
                case InfoType.ProgramId:
                case InfoType.UserExceptionContextAddress:
                case InfoType.TotalNonSystemMemorySize:
                case InfoType.UsedNonSystemMemorySize:
                    {
                        if (subId != 0)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                        KProcess process = currentProcess.HandleTable.GetKProcess(handle);

                        if (process == null)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        switch (id)
                        {
                            case InfoType.CoreMask: value = (ulong)process.Capabilities.AllowedCpuCoresMask; break;
                            case InfoType.PriorityMask: value = (ulong)process.Capabilities.AllowedThreadPriosMask; break;

                            case InfoType.AliasRegionAddress: value = process.MemoryManager.AliasRegionStart; break;
                            case InfoType.AliasRegionSize:
                                value = process.MemoryManager.AliasRegionEnd - process.MemoryManager.AliasRegionStart; break;

                            case InfoType.HeapRegionAddress: value = process.MemoryManager.HeapRegionStart; break;
                            case InfoType.HeapRegionSize:
                                value = process.MemoryManager.HeapRegionEnd - process.MemoryManager.HeapRegionStart; break;

                            case InfoType.TotalMemorySize: value = process.GetMemoryCapacity(); break;
                            case InfoType.UsedMemorySize: value = process.GetMemoryUsage(); break;

                            case InfoType.AslrRegionAddress: value = process.MemoryManager.GetAddrSpaceBaseAddr(); break;
                            case InfoType.AslrRegionSize: value = process.MemoryManager.GetAddrSpaceSize(); break;

                            case InfoType.StackRegionAddress: value = process.MemoryManager.StackRegionStart; break;
                            case InfoType.StackRegionSize:
                                value = process.MemoryManager.StackRegionEnd - process.MemoryManager.StackRegionStart; break;

                            case InfoType.SystemResourceSizeTotal: value = process.PersonalMmHeapPagesCount * KMemoryManager.PageSize; break;
                            case InfoType.SystemResourceSizeUsed:
                                if (process.PersonalMmHeapPagesCount != 0)
                                {
                                    value = process.MemoryManager.GetMmUsedPages() * KMemoryManager.PageSize;
                                }
                                break;

                            case InfoType.ProgramId: value = process.TitleId; break;

                            case InfoType.UserExceptionContextAddress: value = process.UserExceptionContextAddress; break;

                            case InfoType.TotalNonSystemMemorySize: value = process.GetMemoryCapacityWithoutPersonalMmHeap(); break;
                            case InfoType.UsedNonSystemMemorySize: value = process.GetMemoryUsageWithoutPersonalMmHeap(); break;
                        }

                        break;
                    }

                case InfoType.DebuggerAttached:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        if (subId != 0)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        value = _context.Scheduler.GetCurrentProcess().Debug ? 1u : 0u;

                        break;
                    }

                case InfoType.ResourceLimit:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        if (subId != 0)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                        if (currentProcess.ResourceLimit != null)
                        {
                            KHandleTable handleTable = currentProcess.HandleTable;
                            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

                            Result result = handleTable.GenerateHandle(resourceLimit, out int resLimHandle);

                            if (result != Result.Success)
                            {
                                return CheckResult(result);
                            }

                            value = (uint)resLimHandle;
                        }

                        break;
                    }

                case InfoType.IdleTickCount:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        int currentCore = _context.Scheduler.GetCurrentThread().CurrentCore;

                        if (subId != -1 && subId != currentCore)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        value = (ulong)_context.Scheduler.CoreContexts[currentCore].TotalIdleTimeTicks;

                        break;
                    }

                case InfoType.RandomEntropy:
                    {
                        if (handle != 0)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        if ((ulong)subId > 3)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                        value = currentProcess.RandomEntropy[subId];

                        break;
                    }

                case InfoType.ThreadTickCount:
                    {
                        if (subId < -1 || subId > 3)
                        {
                            return CheckResult(KernelResult.InvalidCombination);
                        }

                        KThread thread = _context.Scheduler.GetCurrentProcess().HandleTable.GetKThread(handle);

                        if (thread == null)
                        {
                            return CheckResult(KernelResult.InvalidHandle);
                        }

                        KThread currentThread = _context.Scheduler.GetCurrentThread();

                        int currentCore = currentThread.CurrentCore;

                        if (subId != -1 && subId != currentCore)
                        {
                            return CheckResult(Result.Success);
                        }

                        KCoreContext coreContext = _context.Scheduler.CoreContexts[currentCore];

                        long timeDelta = PerformanceCounter.ElapsedMilliseconds - coreContext.LastContextSwitchTime;

                        if (subId != -1)
                        {
                            value = (ulong)KTimeManager.ConvertMillisecondsToTicks(timeDelta);
                        }
                        else
                        {
                            long totalTimeRunning = thread.TotalTimeRunning;

                            if (thread == currentThread)
                            {
                                totalTimeRunning += timeDelta;
                            }

                            value = (ulong)KTimeManager.ConvertMillisecondsToTicks(totalTimeRunning);
                        }

                        break;
                    }

                default: return CheckResult(KernelResult.InvalidEnumValue);
            }

            return CheckResult(Result.Success);
        }

        public Result CreateEvent(out int wEventHandle, out int rEventHandle)
        {
            KEvent Event = new KEvent(_context);

            KProcess process = _context.Scheduler.GetCurrentProcess();

            Result result = process.HandleTable.GenerateHandle(Event.WritableEvent, out wEventHandle);

            if (result == Result.Success)
            {
                result = process.HandleTable.GenerateHandle(Event.ReadableEvent, out rEventHandle);

                if (result != Result.Success)
                {
                    process.HandleTable.CloseHandle(wEventHandle);
                }
            }
            else
            {
                rEventHandle = 0;
            }

            return CheckResult(result);
        }

        public Result GetProcessList(ulong address, int maxCount, out int count)
        {
            count = 0;

            if ((maxCount >> 28) != 0)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            if (maxCount != 0)
            {
                KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                ulong copySize = (ulong)maxCount * 8;

                if (address + copySize <= address)
                {
                    return CheckResult(KernelResult.InvalidMemState);
                }

                if (currentProcess.MemoryManager.OutsideAddrSpace(address, copySize))
                {
                    return CheckResult(KernelResult.InvalidMemState);
                }
            }

            int copyCount = 0;

            lock (_context.Processes)
            {
                foreach (KProcess process in _context.Processes.Values)
                {
                    if (copyCount < maxCount)
                    {
                        if (!KernelTransfer.KernelToUserInt64(_context, address + (ulong)copyCount * 8, process.Pid))
                        {
                            return CheckResult(KernelResult.UserCopyFailed);
                        }
                    }

                    copyCount++;
                }
            }

            count = copyCount;

            return CheckResult(Result.Success);
        }

        public Result GetSystemInfo(uint id, int handle, long subId, out long value)
        {
            value = 0;

            if (id > 2)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            if (handle != 0)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (id < 2)
            {
                if ((ulong)subId > 3)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }

                KMemoryRegionManager region = _context.MemoryRegions[subId];

                switch (id)
                {
                    // Memory region capacity.
                    case 0: value = (long)region.Size; break;

                    // Memory region free space.
                    case 1:
                        {
                            ulong freePagesCount = region.GetFreePages();

                            value = (long)(freePagesCount * KMemoryManager.PageSize);

                            break;
                        }
                }
            }
            else /* if (Id == 2) */
            {
                if ((ulong)subId > 1)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }

                switch (subId)
                {
                    case 0: value = _context.PrivilegedProcessLowestId; break;
                    case 1: value = _context.PrivilegedProcessHighestId; break;
                }
            }

            return CheckResult(Result.Success);
        }

        // Thread

        public Result CreateThread(
            ulong entrypoint,
            ulong argsPtr,
            ulong stackTop,
            int priority,
            int cpuCore,
            out int handle)
        {
            handle = 0;

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (cpuCore == -2)
            {
                cpuCore = currentProcess.DefaultCpuCore;
            }

            if ((uint)cpuCore >= KScheduler.CpuCoresCount || !currentProcess.IsCpuCoreAllowed(cpuCore))
            {
                return CheckResult(KernelResult.InvalidCpuCore);
            }

            if ((uint)priority >= KScheduler.PrioritiesCount || !currentProcess.IsPriorityAllowed(priority))
            {
                return CheckResult(KernelResult.InvalidPriority);
            }

            long timeout = KTimeManager.ConvertMillisecondsToNanoseconds(100);

            if (currentProcess.ResourceLimit != null &&
               !currentProcess.ResourceLimit.Reserve(LimitableResource.Thread, 1, timeout))
            {
                return CheckResult(KernelResult.ResLimitExceeded);
            }

            KThread thread = new KThread(_context);

            Result result = currentProcess.InitializeThread(
                thread,
                entrypoint,
                argsPtr,
                stackTop,
                priority,
                cpuCore);

            if (result == Result.Success)
            {
                KProcess process = _context.Scheduler.GetCurrentProcess();

                result = process.HandleTable.GenerateHandle(thread, out handle);
            }
            else
            {
                currentProcess.ResourceLimit?.Release(LimitableResource.Thread, 1);
            }

            thread.DecrementReferenceCount();

            return CheckResult(result);
        }

        public Result StartThread(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                thread.IncrementReferenceCount();

                Result result = thread.Start();

                if (result == Result.Success)
                {
                    thread.IncrementReferenceCount();
                }

                thread.DecrementReferenceCount();

                return CheckResult(result);
            }
            else
            {
                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public void ExitThread()
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            _context.Scheduler.ExitThread(currentThread);

            currentThread.Exit();
        }

        public void SleepThread(long timeout)
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            if (timeout < 1)
            {
                switch (timeout)
                {
                    case 0: currentThread.Yield(); break;
                    case -1: currentThread.YieldWithLoadBalancing(); break;
                    case -2: currentThread.YieldAndWaitForLoadBalancing(); break;
                }
            }
            else
            {
                currentThread.Sleep(timeout);
            }
        }

        public Result GetThreadPriority(int handle, out int priority)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                priority = thread.DynamicPriority;

                return CheckResult(Result.Success);
            }
            else
            {
                priority = 0;

                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public Result SetThreadPriority(int handle, int priority)
        {
            // TODO: NPDM check.

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            thread.SetPriority(priority);

            return CheckResult(Result.Success);
        }

        public Result GetThreadCoreMask(int handle, out int preferredCore, out long affinityMask)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                preferredCore = thread.PreferredCore;
                affinityMask = thread.AffinityMask;

                return CheckResult(Result.Success);
            }
            else
            {
                preferredCore = 0;
                affinityMask = 0;

                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public Result SetThreadCoreMask(int handle, int preferredCore, long affinityMask)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (preferredCore == -2)
            {
                preferredCore = currentProcess.DefaultCpuCore;

                affinityMask = 1 << preferredCore;
            }
            else
            {
                if ((currentProcess.Capabilities.AllowedCpuCoresMask | affinityMask) !=
                     currentProcess.Capabilities.AllowedCpuCoresMask)
                {
                    return CheckResult(KernelResult.InvalidCpuCore);
                }

                if (affinityMask == 0)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }

                if ((uint)preferredCore > 3)
                {
                    if ((preferredCore | 2) != -1)
                    {
                        return CheckResult(KernelResult.InvalidCpuCore);
                    }
                }
                else if ((affinityMask & (1 << preferredCore)) == 0)
                {
                    return CheckResult(KernelResult.InvalidCombination);
                }
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            return CheckResult(thread.SetCoreAndAffinityMask(preferredCore, affinityMask));
        }

        public int GetCurrentProcessorNumber()
        {
            int currentCore = _context.Scheduler.GetCurrentThread().CurrentCore;

            // We need to call this so that it handles post-syscall work.
            CheckResult(Result.Success);

            return currentCore;
        }

        public Result GetThreadId(int handle, out long threadUid)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                threadUid = thread.ThreadUid;

                return CheckResult(Result.Success);
            }
            else
            {
                threadUid = 0;

                return CheckResult(KernelResult.InvalidHandle);
            }
        }

        public Result SetThreadActivity(int handle, bool pause)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetObject<KThread>(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (thread.Owner != process)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (thread == _context.Scheduler.GetCurrentThread())
            {
                return CheckResult(KernelResult.InvalidThread);
            }

            return CheckResult(thread.SetActivity(pause));
        }

        public Result GetThreadContext3(ulong address, int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            KThread thread = currentProcess.HandleTable.GetObject<KThread>(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (thread.Owner != currentProcess)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (currentThread == thread)
            {
                return CheckResult(KernelResult.InvalidThread);
            }

            IAddressSpaceManager memory = currentProcess.CpuMemory;

            memory.Write(address + 0x0, thread.Context.GetX(0));
            memory.Write(address + 0x8, thread.Context.GetX(1));
            memory.Write(address + 0x10, thread.Context.GetX(2));
            memory.Write(address + 0x18, thread.Context.GetX(3));
            memory.Write(address + 0x20, thread.Context.GetX(4));
            memory.Write(address + 0x28, thread.Context.GetX(5));
            memory.Write(address + 0x30, thread.Context.GetX(6));
            memory.Write(address + 0x38, thread.Context.GetX(7));
            memory.Write(address + 0x40, thread.Context.GetX(8));
            memory.Write(address + 0x48, thread.Context.GetX(9));
            memory.Write(address + 0x50, thread.Context.GetX(10));
            memory.Write(address + 0x58, thread.Context.GetX(11));
            memory.Write(address + 0x60, thread.Context.GetX(12));
            memory.Write(address + 0x68, thread.Context.GetX(13));
            memory.Write(address + 0x70, thread.Context.GetX(14));
            memory.Write(address + 0x78, thread.Context.GetX(15));
            memory.Write(address + 0x80, thread.Context.GetX(16));
            memory.Write(address + 0x88, thread.Context.GetX(17));
            memory.Write(address + 0x90, thread.Context.GetX(18));
            memory.Write(address + 0x98, thread.Context.GetX(19));
            memory.Write(address + 0xa0, thread.Context.GetX(20));
            memory.Write(address + 0xa8, thread.Context.GetX(21));
            memory.Write(address + 0xb0, thread.Context.GetX(22));
            memory.Write(address + 0xb8, thread.Context.GetX(23));
            memory.Write(address + 0xc0, thread.Context.GetX(24));
            memory.Write(address + 0xc8, thread.Context.GetX(25));
            memory.Write(address + 0xd0, thread.Context.GetX(26));
            memory.Write(address + 0xd8, thread.Context.GetX(27));
            memory.Write(address + 0xe0, thread.Context.GetX(28));
            memory.Write(address + 0xe8, thread.Context.GetX(29));
            memory.Write(address + 0xf0, thread.Context.GetX(30));
            memory.Write(address + 0xf8, thread.Context.GetX(31));

            memory.Write(address + 0x100, thread.LastPc);

            memory.Write(address + 0x108, thread.Context.Cpsr);

            memory.Write(address + 0x110, thread.Context.GetV(0));
            memory.Write(address + 0x120, thread.Context.GetV(1));
            memory.Write(address + 0x130, thread.Context.GetV(2));
            memory.Write(address + 0x140, thread.Context.GetV(3));
            memory.Write(address + 0x150, thread.Context.GetV(4));
            memory.Write(address + 0x160, thread.Context.GetV(5));
            memory.Write(address + 0x170, thread.Context.GetV(6));
            memory.Write(address + 0x180, thread.Context.GetV(7));
            memory.Write(address + 0x190, thread.Context.GetV(8));
            memory.Write(address + 0x1a0, thread.Context.GetV(9));
            memory.Write(address + 0x1b0, thread.Context.GetV(10));
            memory.Write(address + 0x1c0, thread.Context.GetV(11));
            memory.Write(address + 0x1d0, thread.Context.GetV(12));
            memory.Write(address + 0x1e0, thread.Context.GetV(13));
            memory.Write(address + 0x1f0, thread.Context.GetV(14));
            memory.Write(address + 0x200, thread.Context.GetV(15));
            memory.Write(address + 0x210, thread.Context.GetV(16));
            memory.Write(address + 0x220, thread.Context.GetV(17));
            memory.Write(address + 0x230, thread.Context.GetV(18));
            memory.Write(address + 0x240, thread.Context.GetV(19));
            memory.Write(address + 0x250, thread.Context.GetV(20));
            memory.Write(address + 0x260, thread.Context.GetV(21));
            memory.Write(address + 0x270, thread.Context.GetV(22));
            memory.Write(address + 0x280, thread.Context.GetV(23));
            memory.Write(address + 0x290, thread.Context.GetV(24));
            memory.Write(address + 0x2a0, thread.Context.GetV(25));
            memory.Write(address + 0x2b0, thread.Context.GetV(26));
            memory.Write(address + 0x2c0, thread.Context.GetV(27));
            memory.Write(address + 0x2d0, thread.Context.GetV(28));
            memory.Write(address + 0x2e0, thread.Context.GetV(29));
            memory.Write(address + 0x2f0, thread.Context.GetV(30));
            memory.Write(address + 0x300, thread.Context.GetV(31));

            memory.Write(address + 0x310, thread.Context.Fpcr);
            memory.Write(address + 0x314, thread.Context.Fpsr);
            memory.Write(address + 0x318, thread.Context.TlsAddress);

            return CheckResult(Result.Success);
        }

        // Thread synchronization

        public Result WaitSynchronization(out int handleIndex, ulong handlesPtr, int handlesCount, long timeout)
        {
            handleIndex = 0;

            if ((uint)handlesCount > KThread.MaxWaitSyncObjects)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KThread currentThread = _context.Scheduler.GetCurrentThread();

            if (handlesCount != 0)
            {
                KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                if (currentProcess.MemoryManager.AddrSpaceStart > handlesPtr)
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                long handlesSize = handlesCount * 4;

                if (handlesPtr + (ulong)handlesSize <= handlesPtr)
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                if (handlesPtr + (ulong)handlesSize - 1 > currentProcess.MemoryManager.AddrSpaceEnd - 1)
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                Span<int> handles = new Span<int>(currentThread.WaitSyncHandles).Slice(0, handlesCount);

                if (!KernelTransfer.UserToKernelInt32Array(_context, handlesPtr, handles))
                {
                    return CheckResult(KernelResult.UserCopyFailed);
                }

                return CheckResult(WaitSynchronization(out handleIndex, handles, timeout));
            }
            else
            {
                return CheckResult(WaitSynchronization(out handleIndex, Span<int>.Empty, timeout));
            }
        }

        public Result WaitSynchronization(out int handleIndex, ReadOnlySpan<int> handles, long timeout)
        {
            handleIndex = 0;

            if (handles.Length > KThread.MaxWaitSyncObjects)
            {
                return CheckResult(KernelResult.MaximumExceeded);
            }

            KThread currentThread = _context.Scheduler.GetCurrentThread();

            var syncObjs = new Span<KSynchronizationObject>(currentThread.WaitSyncObjects).Slice(0, handles.Length);

            if (handles.Length != 0)
            {
                KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

                int processedHandles = 0;

                for (; processedHandles < handles.Length; processedHandles++)
                {
                    KSynchronizationObject syncObj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[processedHandles]);

                    if (syncObj == null)
                    {
                        break;
                    }

                    syncObjs[processedHandles] = syncObj;

                    syncObj.IncrementReferenceCount();
                }

                if (processedHandles != handles.Length)
                {
                    // One or more handles are invalid.
                    for (int index = 0; index < processedHandles; index++)
                    {
                        currentThread.WaitSyncObjects[index].DecrementReferenceCount();
                    }

                    return CheckResult(KernelResult.InvalidHandle);
                }
            }

            Result result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex);

            if (result == KernelResult.PortRemoteClosed)
            {
                result = Result.Success;
            }

            for (int index = 0; index < handles.Length; index++)
            {
                currentThread.WaitSyncObjects[index].DecrementReferenceCount();
            }

            return CheckResult(result);
        }

        public Result CancelSynchronization(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            thread.CancelSynchronization();

            return CheckResult(Result.Success);
        }

        public Result ArbitrateLock(int ownerHandle, ulong mutexAddress, int requesterHandle)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return CheckResult(currentProcess.AddressArbiter.ArbitrateLock(ownerHandle, mutexAddress, requesterHandle));
        }

        public Result ArbitrateUnlock(ulong mutexAddress)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return CheckResult(currentProcess.AddressArbiter.ArbitrateUnlock(mutexAddress));
        }

        public Result WaitProcessWideKeyAtomic(
            ulong mutexAddress,
            ulong condVarAddress,
            int handle,
            long timeout)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return currentProcess.AddressArbiter.WaitProcessWideKeyAtomic(
                mutexAddress,
                condVarAddress,
                handle,
                timeout);
        }

        public Result SignalProcessWideKey(ulong address, int count)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            currentProcess.AddressArbiter.SignalProcessWideKey(address, count);

            return CheckResult(Result.Success);
        }

        public Result WaitForAddress(ulong address, ArbitrationType type, int value, long timeout)
        {
            if (IsPointingInsideKernel(address))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return type switch
            {
                ArbitrationType.WaitIfLessThan
                    => currentProcess.AddressArbiter.WaitForAddressIfLessThan(address, value, false, timeout),
                ArbitrationType.DecrementAndWaitIfLessThan
                    => currentProcess.AddressArbiter.WaitForAddressIfLessThan(address, value, true, timeout),
                ArbitrationType.WaitIfEqual
                    => currentProcess.AddressArbiter.WaitForAddressIfEqual(address, value, timeout),
                _ => KernelResult.InvalidEnumValue,
            };
        }

        public Result SignalToAddress(ulong address, SignalType type, int value, int count)
        {
            if (IsPointingInsideKernel(address))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (IsAddressNotWordAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            return type switch
            {
                SignalType.Signal
                    => currentProcess.AddressArbiter.Signal(address, count),
                SignalType.SignalAndIncrementIfEqual
                    => currentProcess.AddressArbiter.SignalAndIncrementIfEqual(address, value, count),
                SignalType.SignalAndModifyIfEqual
                    => currentProcess.AddressArbiter.SignalAndModifyIfEqual(address, value, count),
                _ => KernelResult.InvalidEnumValue
            };
        }

        private static bool IsPointingInsideKernel(ulong address)
        {
            return (address + 0x1000000000) < 0xffffff000;
        }

        private static bool IsAddressNotWordAligned(ulong address)
        {
            return (address & 3) != 0;
        }

        // Resource limit.

        public Result GetResourceLimitLimitValue(out long limit, int handle, LimitableResource resource)
        {
            limit = 0;

            if ((uint)resource >= (uint)LimitableResource.Count)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            KResourceLimit resourceLimit = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            limit = resourceLimit.GetLimitValue(resource);

            return CheckResult(Result.Success);
        }

        public Result GetResourceLimitCurrentValue(out long value, int handle, LimitableResource resource)
        {
            value = 0;

            if ((uint)resource >= (uint)LimitableResource.Count)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            KResourceLimit resourceLimit = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            value = resourceLimit.GetCurrentValue(resource);

            return CheckResult(Result.Success);
        }

        public Result CreateResourceLimit(out int handle)
        {
            KResourceLimit resourceLimit = new KResourceLimit(_context);

            return CheckResult(_context.Scheduler.GetCurrentProcess().HandleTable.GenerateHandle(resourceLimit, out handle));
        }

        public Result SetResourceLimitLimitValue(int handle, LimitableResource resource, long limit)
        {
            if ((uint)resource >= (uint)LimitableResource.Count)
            {
                return CheckResult(KernelResult.InvalidEnumValue);
            }

            KResourceLimit resourceLimit = _context.Scheduler.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            return CheckResult(resourceLimit.SetLimitValue(resource, limit));
        }
    }
}
