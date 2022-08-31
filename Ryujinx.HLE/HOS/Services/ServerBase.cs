using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Ipc;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.SupervisorCall;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services
{
    class ServerBase : IDisposable
    {
        // Must be the maximum value used by services (highest one know is the one used by nvservices = 0x8000).
        // Having a size that is too low will cause failures as data copy will fail if the receiving buffer is
        // not large enough.
        private const int PointerBufferSize = 0x8000;

        private readonly static int[] DefaultCapabilities = new int[]
        {
            0x030363F7,
            0x1FFFFFCF,
            0x207FFFEF,
            0x47E0060F,
            0x0048BFFF,
            0x01007FFF
        };

        private readonly KernelContext _context;
        private KProcess _selfProcess;

        private object _registryLock = new object();
        private object _waitRequestLock = new object();
        private readonly List<int> _sessionHandles = new List<int>();
        private readonly List<int> _portHandles = new List<int>();
        private readonly Dictionary<int, IpcService> _sessions = new Dictionary<int, IpcService>();
        private readonly Dictionary<int, Func<IpcService>> _ports = new Dictionary<int, Func<IpcService>>();

        public ManualResetEvent InitDone { get; }
        public string Name { get; }
        public Func<IpcService> SmObjectFactory { get; }

        private int _threadCount;
        private ulong _heapBaseAddress;
        private ulong _heapSize;

        public ServerBase(KernelContext context, string name, Func<IpcService> smObjectFactory = null, int threadCount = 1)
        {
            InitDone = new ManualResetEvent(false);
            _context = context;
            Name = name;
            SmObjectFactory = smObjectFactory;
            _threadCount = threadCount;
            _heapSize = BitUtils.AlignUp((ulong)_threadCount * PointerBufferSize, 0x200000);

            const ProcessCreationFlags flags =
                ProcessCreationFlags.EnableAslr |
                ProcessCreationFlags.AddressSpace64Bit |
                ProcessCreationFlags.Is64Bit |
                ProcessCreationFlags.PoolPartitionSystem;

            ProcessCreationInfo creationInfo = new ProcessCreationInfo("Service", 1, 0, 0x8000000, 1, flags, 0, 0);

            KernelStatic.StartInitialProcess(context, creationInfo, DefaultCapabilities, 44, Main);
        }

        private void AddPort(int serverPortHandle, Func<IpcService> objectFactory)
        {
            using var registryScopedLock = new ServerManagedScopedLock(_context, _registryLock);

            RegisterPortHandleForProcessingLocked(serverPortHandle);
            _ports.Add(serverPortHandle, objectFactory);
        }

        public void AddSessionObj(KServerSession serverSession, IpcService obj)
        {
            // Ensure that the sever loop is running.
            InitDone.WaitOne();

            _selfProcess.HandleTable.GenerateHandle(serverSession, out int serverSessionHandle);
            AddSessionObj(serverSessionHandle, obj);
        }

        public void AddSessionObj(int serverSessionHandle, IpcService obj)
        {
            using var registryScopedLock = new ServerManagedScopedLock(_context, _registryLock);

            RegisterSessionHandleForProcessingLocked(serverSessionHandle);
            _sessions.Add(serverSessionHandle, obj);
        }

        private void RegisterSessionHandleForProcessingLocked(int serverSessionHandle)
        {
            _sessionHandles.Add(serverSessionHandle);
        }

        private void UnregisterSessionHandleForProcessingLocked(int serverSessionHandle)
        {
            _sessionHandles.Remove(serverSessionHandle);
        }

        private void RegisterPortHandleForProcessingLocked(int serverPortHandle)
        {
            _portHandles.Add(serverPortHandle);
        }

        private void UnregisterPortHandleForProcessingLocked(int serverPortHandle)
        {
            _portHandles.Remove(serverPortHandle);
        }

        private void Main()
        {
            _context.Syscall.SetHeapSize(out _heapBaseAddress, _heapSize);

            for (int i = 1; i < _threadCount; i++)
            {
                KernelResult result = _context.Syscall.CreateThread(out int threadHandle, 0UL, 0UL, 0UL, 44, 3, () => ServerLoop(i));

                if (result == KernelResult.Success)
                {
                    result = _context.Syscall.StartThread(threadHandle);

                    if (result != KernelResult.Success)
                    {
                        Logger.Error?.Print(LogClass.Service, $"Failed to start thread on {Name}: {result}");
                    }

                    _context.Syscall.CloseHandle(threadHandle);
                }
                else
                {
                    Logger.Error?.Print(LogClass.Service, $"Failed to create thread on {Name}: {result}");
                }
            }

            ServerLoop(0);
        }

        // TODO: This would be better if we had a proper kernel lock primitive here
        private class ServerManagedScopedLock : IDisposable
        {
            private object _obj;

            public ServerManagedScopedLock(KernelContext context, object obj)
            {
                while (!Monitor.TryEnter(obj))
                {
                    context.Syscall.SleepThread(0);
                }

                _obj = obj;
            }

            public void Dispose()
            {
                Debug.Assert(Monitor.IsEntered(_obj));

                Monitor.Exit(_obj);
            }
        }

        private void ServerLoop(int threadIndex)
        {
            _selfProcess = KernelStatic.GetCurrentProcess();

            if (SmObjectFactory != null)
            {
                _context.Syscall.ManageNamedPort(out int serverPortHandle, "sm:", 50);

                AddPort(serverPortHandle, SmObjectFactory);
            }

            InitDone.Set();

            KThread thread = KernelStatic.GetCurrentThread();
            ulong messagePtr = thread.TlsAddress;
            ulong heapAddr = (_heapBaseAddress + ((ulong)threadIndex * PointerBufferSize));

            _selfProcess.CpuMemory.Write(messagePtr + 0x0, 0);
            _selfProcess.CpuMemory.Write(messagePtr + 0x4, 2 << 10);
            _selfProcess.CpuMemory.Write(messagePtr + 0x8, heapAddr | ((ulong)PointerBufferSize << 48));

            int replyTargetHandle = 0;

            while (true)
            {
                KernelResult rc;
                bool isSession = false;
                int signaledHandle = -1;
                IpcService service = null;

                // We ensure that only one ReplyAndReceive can go at a time to avoid an handle being processed by two different threads.
                {
                    using var waitRequestScopedLock = new ServerManagedScopedLock(_context, _waitRequestLock);

                    int portHandlesLength;
                    int[] handles;

                    {
                        using var registryScopedLock = new ServerManagedScopedLock(_context, _registryLock);

                        if (replyTargetHandle != 0)
                        {
                            RegisterSessionHandleForProcessingLocked(replyTargetHandle);
                        }

                        int[] portHandles = _portHandles.ToArray();
                        int[] sessionHandles = _sessionHandles.ToArray();

                        portHandlesLength = portHandles.Length;

                        handles = new int[portHandlesLength + sessionHandles.Length];

                        portHandles.CopyTo(handles, 0);
                        sessionHandles.CopyTo(handles, portHandles.Length);
                    }

                    // We still need a timeout here to allow the service to pick up and listen new sessions...
                    rc = _context.Syscall.ReplyAndReceive(out int signaledIndex, handles, replyTargetHandle, 1000000L);

                    // Unregister the handle we just got and register a possible one that we just replied to.
                    if (rc == KernelResult.Success)
                    {
                        signaledHandle = handles[signaledIndex];

                        isSession = signaledIndex >= portHandlesLength;

                        {
                            using var registryScopedLock = new ServerManagedScopedLock(_context, _registryLock);

                            if (isSession)
                            {
                                UnregisterSessionHandleForProcessingLocked(signaledHandle);
                                service = _sessions[signaledHandle];
                            }
                            else
                            {
                                UnregisterPortHandleForProcessingLocked(signaledHandle);
                            }
                        }
                    }
                }

                thread.HandlePostSyscall();

                if (!thread.Context.Running)
                {
                    break;
                }

                replyTargetHandle = 0;

                if (rc == KernelResult.Success && isSession)
                {
                    if (Process(signaledHandle, service, heapAddr))
                    {
                        replyTargetHandle = signaledHandle;
                    }
                }
                else
                {
                    if (rc == KernelResult.Success)
                    {
                        // We got a new connection, accept the session to allow servicing future requests.
                        if (_context.Syscall.AcceptSession(out int serverSessionHandle, signaledHandle) == KernelResult.Success)
                        {
                            IpcService obj = _ports[signaledHandle].Invoke();

                            AddSessionObj(serverSessionHandle, obj);
                        }

                        {
                            using var registryScopedLock = new ServerManagedScopedLock(_context, _registryLock);

                            RegisterPortHandleForProcessingLocked(signaledHandle);
                        }
                    }

                    _selfProcess.CpuMemory.Write(messagePtr + 0x0, 0);
                    _selfProcess.CpuMemory.Write(messagePtr + 0x4, 2 << 10);
                    _selfProcess.CpuMemory.Write(messagePtr + 0x8, heapAddr | ((ulong)PointerBufferSize << 48));
                }
            }

            Dispose();
        }

        private bool Process(int serverSessionHandle, IpcService service, ulong recvListAddr)
        {
            KProcess process = KernelStatic.GetCurrentProcess();
            KThread thread = KernelStatic.GetCurrentThread();
            ulong messagePtr = thread.TlsAddress;
            ulong messageSize = 0x100;

            byte[] reqData = new byte[messageSize];

            process.CpuMemory.Read(messagePtr, reqData);

            IpcMessage request = new IpcMessage(reqData, (long)messagePtr);
            IpcMessage response = new IpcMessage();

            ulong tempAddr = recvListAddr;
            int sizesOffset = request.RawData.Length - ((request.RecvListBuff.Count * 2 + 3) & ~3);

            bool noReceive = true;

            for (int i = 0; i < request.ReceiveBuff.Count; i++)
            {
                noReceive &= (request.ReceiveBuff[i].Position == 0);
            }

            if (noReceive)
            {
                for (int i = 0; i < request.RecvListBuff.Count; i++)
                {
                    ulong size = (ulong)BinaryPrimitives.ReadInt16LittleEndian(request.RawData.AsSpan(sizesOffset + i * 2, 2));

                    response.PtrBuff.Add(new IpcPtrBuffDesc(tempAddr, (uint)i, size));

                    request.RecvListBuff[i] = new IpcRecvListBuffDesc(tempAddr, size);

                    tempAddr += size;
                }
            }

            bool shouldReply = true;
            bool isTipcCommunication = false;

            using (MemoryStream raw = new MemoryStream(request.RawData))
            {
                BinaryReader reqReader = new BinaryReader(raw);

                if (request.Type == IpcMessageType.HipcRequest ||
                    request.Type == IpcMessageType.HipcRequestWithContext)
                {
                    response.Type = IpcMessageType.HipcResponse;

                    using (MemoryStream resMs = new MemoryStream())
                    {
                        BinaryWriter resWriter = new BinaryWriter(resMs);

                        ServiceCtx context = new ServiceCtx(
                            _context.Device,
                            process,
                            process.CpuMemory,
                            thread,
                            request,
                            response,
                            reqReader,
                            resWriter);

                        service.CallHipcMethod(context);

                        response.RawData = resMs.ToArray();
                    }
                }
                else if (request.Type == IpcMessageType.HipcControl ||
                         request.Type == IpcMessageType.HipcControlWithContext)
                {
                    uint magic = (uint)reqReader.ReadUInt64();
                    uint cmdId = (uint)reqReader.ReadUInt64();

                    switch (cmdId)
                    {
                        case 0:
                            request = FillResponse(response, 0, service.ConvertToDomain());
                            break;

                        case 3:
                            request = FillResponse(response, 0, PointerBufferSize);
                            break;

                        // TODO: Whats the difference between IpcDuplicateSession/Ex?
                        case 2:
                        case 4:
                            int unknown = reqReader.ReadInt32();

                            _context.Syscall.CreateSession(out int dupServerSessionHandle, out int dupClientSessionHandle, false, 0);

                            AddSessionObj(dupServerSessionHandle, service);

                            response.HandleDesc = IpcHandleDesc.MakeMove(dupClientSessionHandle);

                            request = FillResponse(response, 0);

                            break;

                        default: throw new NotImplementedException(cmdId.ToString());
                    }
                }
                else if (request.Type == IpcMessageType.HipcCloseSession || request.Type == IpcMessageType.TipcCloseSession)
                {
                    _context.Syscall.CloseHandle(serverSessionHandle);
                    if (service is IDisposable disposableObj)
                    {
                        disposableObj.Dispose();
                    }

                    {
                        using var registryScopedLock = new ServerManagedScopedLock(_context, _registryLock);

                        _sessions.Remove(serverSessionHandle);
                    }

                    shouldReply = false;
                }
                // If the type is past 0xF, we are using TIPC
                else if (request.Type > IpcMessageType.TipcCloseSession)
                {
                    isTipcCommunication = true;

                    // Response type is always the same as request on TIPC.
                    response.Type = request.Type;

                    using (MemoryStream resMs = new MemoryStream())
                    {
                        BinaryWriter resWriter = new BinaryWriter(resMs);

                        ServiceCtx context = new ServiceCtx(
                            _context.Device,
                            process,
                            process.CpuMemory,
                            thread,
                            request,
                            response,
                            reqReader,
                            resWriter);

                        service.CallTipcMethod(context);

                        response.RawData = resMs.ToArray();
                    }

                    process.CpuMemory.Write(messagePtr, response.GetBytesTipc());
                }
                else
                {
                    throw new NotImplementedException(request.Type.ToString());
                }

                if (!isTipcCommunication)
                {
                    process.CpuMemory.Write(messagePtr, response.GetBytes((long)messagePtr, recvListAddr | ((ulong)PointerBufferSize << 48)));
                }

                return shouldReply;
            }
        }

        private static IpcMessage FillResponse(IpcMessage response, long result, params int[] values)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);

                foreach (int value in values)
                {
                    writer.Write(value);
                }

                return FillResponse(response, result, ms.ToArray());
            }
        }

        private static IpcMessage FillResponse(IpcMessage response, long result, byte[] data = null)
        {
            response.Type = IpcMessageType.HipcResponse;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);

                writer.Write(IpcMagic.Sfco);
                writer.Write(result);

                if (data != null)
                {
                    writer.Write(data);
                }

                response.RawData = ms.ToArray();
            }

            return response;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_registryLock)
                {
                    foreach (IpcService service in _sessions.Values)
                    {
                        if (service is IDisposable disposableObj)
                        {
                            disposableObj.Dispose();
                        }

                        service.DestroyAtExit();
                    }

                    _sessions.Clear();
                }

                InitDone.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}