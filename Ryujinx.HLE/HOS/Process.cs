using ChocolArm64;
using ChocolArm64.Events;
using ChocolArm64.Memory;
using ChocolArm64.State;
using LibHac;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Diagnostics.Demangler;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Services.Nv;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.Loaders;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Loaders.Npdm;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.HOS
{
    internal class Process : IDisposable
    {
        private const int TickFreq = 19_200_000;

        public Switch Device { get; private set; }

        public bool NeedsHbAbi { get; private set; }

        public long HbAbiDataPosition { get; private set; }

        public int ProcessId { get; private set; }

        private ATranslator _translator;

        public AMemory Memory { get; private set; }

        public KMemoryManager MemoryManager { get; private set; }

        private List<KTlsPageManager> _tlsPages;

        public Npdm MetaData { get; private set; }

        public Nacp ControlData { get; set; }

        public KProcessHandleTable HandleTable { get; private set; }

        public AppletStateMgr AppletState { get; private set; }

        private SvcHandler _svcHandler;

        private ConcurrentDictionary<long, KThread> _threads;

        private List<Executable> _executables;

        private long _imageBase;

        private bool _disposed;

        public Process(Switch device, int processId, Npdm metaData)
        {
            this.Device    = device;
            this.MetaData  = metaData;
            this.ProcessId = processId;

            Memory = new AMemory(device.Memory.RamPointer);

            Memory.InvalidAccess += CpuInvalidAccessHandler;

            MemoryManager = new KMemoryManager(this);

            _tlsPages = new List<KTlsPageManager>();

            int handleTableSize = 1024;

            if (metaData != null)
                foreach (KernelAccessControlItem item in metaData.Aci0.KernelAccessControl.Items)
                    if (item.HasHandleTableSize)
                    {
                        handleTableSize = item.HandleTableSize;

                        break;
                    }

            HandleTable = new KProcessHandleTable(device.System, handleTableSize);

            AppletState = new AppletStateMgr(device.System);

            _svcHandler = new SvcHandler(device, this);

            _threads = new ConcurrentDictionary<long, KThread>();

            _executables = new List<Executable>();

            _imageBase = MemoryManager.CodeRegionStart;
        }

        public void LoadProgram(IExecutable program)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Process));

            long imageEnd = LoadProgram(program, _imageBase);

            _imageBase = IntUtils.AlignUp(imageEnd, KMemoryManager.PageSize);
        }

        public long LoadProgram(IExecutable program, long executableBase)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Process));

            Logger.PrintInfo(LogClass.Loader, $"Image base at 0x{executableBase:x16}.");

            Executable executable = new Executable(program, MemoryManager, Memory, executableBase);

            _executables.Add(executable);

            return executable.ImageEnd;
        }

        public void RemoveProgram(long executableBase)
        {
            foreach (Executable executable in _executables)
                if (executable.ImageBase == executableBase)
                {
                    _executables.Remove(executable);
                    break;
                }
        }

        public void SetEmptyArgs()
        {
            //TODO: This should be part of Run.
            _imageBase += KMemoryManager.PageSize;
        }

        public bool Run(bool needsHbAbi = false)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Process));

            this.NeedsHbAbi = needsHbAbi;

            if (_executables.Count == 0) return false;

            long mainStackTop = MemoryManager.CodeRegionEnd - KMemoryManager.PageSize;

            long mainStackSize = 1 * 1024 * 1024;

            long mainStackBottom = mainStackTop - mainStackSize;

            MemoryManager.HleMapCustom(
                mainStackBottom,
                mainStackSize,
                MemoryState.MappedMemory,
                MemoryPermission.ReadAndWrite);

            int handle = MakeThread(_executables[0].ImageBase, mainStackTop, 0, 44, 0);

            if (handle == -1) return false;

            KThread mainThread = HandleTable.GetKThread(handle);

            if (needsHbAbi)
            {
                HbAbiDataPosition = IntUtils.AlignUp(_executables[0].ImageEnd, KMemoryManager.PageSize);

                const long hbAbiDataSize = KMemoryManager.PageSize;

                MemoryManager.HleMapCustom(
                    HbAbiDataPosition,
                    hbAbiDataSize,
                    MemoryState.MappedMemory,
                    MemoryPermission.ReadAndWrite);

                string switchPath = Device.FileSystem.SystemPathToSwitchPath(_executables[0].FilePath);

                Homebrew.WriteHbAbiData(Memory, HbAbiDataPosition, handle, switchPath);

                mainThread.Context.ThreadState.X0 = (ulong)HbAbiDataPosition;
                mainThread.Context.ThreadState.X1 = ulong.MaxValue;
            }

            mainThread.TimeUp();

            return true;
        }

        private int _threadIdCtr = 1;

        public int MakeThread(
            long entryPoint,
            long stackTop,
            long argsPtr,
            int  priority,
            int  processorId)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Process));

            AThread cpuThread = new AThread(GetTranslator(), Memory, entryPoint);

            long tpidr = GetFreeTls();

            int threadId = _threadIdCtr++; //(int)((Tpidr - MemoryManager.TlsIoRegionStart) / 0x200) + 1;

            KThread thread = new KThread(cpuThread, this, Device.System, processorId, priority, threadId);

            thread.LastPc = entryPoint;

            HandleTable.GenerateHandle(thread, out int handle);

            cpuThread.ThreadState.CntfrqEl0 = TickFreq;
            cpuThread.ThreadState.Tpidr     = tpidr;

            cpuThread.ThreadState.X0  = (ulong)argsPtr;
            cpuThread.ThreadState.X1  = (ulong)handle;
            cpuThread.ThreadState.X31 = (ulong)stackTop;

            cpuThread.ThreadState.Interrupt += InterruptHandler;
            cpuThread.ThreadState.Break     += BreakHandler;
            cpuThread.ThreadState.SvcCall   += _svcHandler.SvcCall;
            cpuThread.ThreadState.Undefined += UndefinedHandler;

            cpuThread.WorkFinished += ThreadFinished;

            _threads.TryAdd(cpuThread.ThreadState.Tpidr, thread);

            return handle;
        }

        private long GetFreeTls()
        {
            long position;

            lock (_tlsPages)
            {
                for (int index = 0; index < _tlsPages.Count; index++)
                    if (_tlsPages[index].TryGetFreeTlsAddr(out position)) return position;

                long pagePosition = MemoryManager.HleMapTlsPage();

                KTlsPageManager tlsPage = new KTlsPageManager(pagePosition);

                _tlsPages.Add(tlsPage);

                tlsPage.TryGetFreeTlsAddr(out position);
            }

            return position;
        }

        private void InterruptHandler(object sender, EventArgs e)
        {
            Device.System.Scheduler.ContextSwitch();
        }

        private void BreakHandler(object sender, AInstExceptionEventArgs e)
        {
            PrintStackTraceForCurrentThread();

            throw new GuestBrokeExecutionException();
        }

        private void UndefinedHandler(object sender, AInstUndefinedEventArgs e)
        {
            PrintStackTraceForCurrentThread();

            throw new UndefinedInstructionException(e.Position, e.RawOpCode);
        }

        public void EnableCpuTracing()
        {
            _translator.EnableCpuTrace = true;
        }

        public void DisableCpuTracing()
        {
            _translator.EnableCpuTrace = false;
        }

        private void CpuTraceHandler(object sender, ACpuTraceEventArgs e)
        {
            Executable exe = GetExecutable(e.Position);

            if (exe == null) return;

            if (!TryGetSubName(exe, e.Position, out string subName)) subName = string.Empty;

            long offset = e.Position - exe.ImageBase;

            string exeNameWithAddr = $"{exe.Name}:0x{offset:x8}";

            Logger.PrintDebug(LogClass.Cpu, exeNameWithAddr + " " + subName);
        }

        private ATranslator GetTranslator()
        {
            if (_translator == null)
            {
                _translator = new ATranslator();

                _translator.CpuTrace += CpuTraceHandler;
            }

            return _translator;
        }

        private void CpuInvalidAccessHandler(object sender, AInvalidAccessEventArgs e)
        {
            PrintStackTraceForCurrentThread();
        }

        private void PrintStackTraceForCurrentThread()
        {
            foreach (KThread thread in _threads.Values)
                if (thread.Context.IsCurrentThread())
                {
                    PrintStackTrace(thread.Context.ThreadState);

                    break;
                }
        }

        public void PrintStackTrace(AThreadState threadState)
        {
            StringBuilder trace = new StringBuilder();

            trace.AppendLine("Guest stack trace:");

            void AppendTrace(long position)
            {
                Executable exe = GetExecutable(position);

                if (exe == null) return;

                if (!TryGetSubName(exe, position, out string subName))
                    subName = $"Sub{position:x16}";
                else if (subName.StartsWith("_Z")) subName = Demangler.Parse(subName);

                long offset = position - exe.ImageBase;

                string exeNameWithAddr = $"{exe.Name}:0x{offset:x8}";

                trace.AppendLine(" " + exeNameWithAddr + " " + subName);
            }

            long framePointer = (long)threadState.X29;

            while (framePointer != 0)
            {
                AppendTrace(Memory.ReadInt64(framePointer + 8));

                framePointer = Memory.ReadInt64(framePointer);
            }

            Logger.PrintInfo(LogClass.Cpu, trace.ToString());
        }

        private bool TryGetSubName(Executable exe, long position, out string name)
        {
            position -= exe.ImageBase;

            int left  = 0;
            int right = exe.SymbolTable.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                ElfSym symbol = exe.SymbolTable[middle];

                long endPosition = symbol.Value + symbol.Size;

                if ((ulong)position >= (ulong)symbol.Value && (ulong)position < (ulong)endPosition)
                {
                    name = symbol.Name;

                    return true;
                }

                if ((ulong)position < (ulong)symbol.Value)
                    right = middle - 1;
                else
                    left = middle + 1;
            }

            name = null;

            return false;
        }

        private Executable GetExecutable(long position)
        {
            string name = string.Empty;

            for (int index = _executables.Count - 1; index >= 0; index--)
                if ((ulong)position >= (ulong)_executables[index].ImageBase) return _executables[index];

            return null;
        }

        private void ThreadFinished(object sender, EventArgs e)
        {
            if (sender is AThread thread)
                if (_threads.TryRemove(thread.ThreadState.Tpidr, out KThread kernelThread)) Device.System.Scheduler.RemoveThread(kernelThread);

            if (_threads.Count == 0) Device.System.ExitProcess(ProcessId);
        }

        public KThread GetThread(long tpidr)
        {
            if (!_threads.TryGetValue(tpidr, out KThread thread)) throw new InvalidOperationException();

            return thread;
        }

        private void Unload()
        {
            if (_disposed || _threads.Count > 0) return;

            _disposed = true;

            HandleTable.Destroy();

            INvDrvServices.UnloadProcess(this);

            if (NeedsHbAbi && _executables.Count > 0 && _executables[0].FilePath.EndsWith(Homebrew.TemporaryNroSuffix)) File.Delete(_executables[0].FilePath);

            Logger.PrintInfo(LogClass.Loader, $"Process {ProcessId} exiting...");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_threads.Count > 0)
                    foreach (KThread thread in _threads.Values) Device.System.Scheduler.StopThread(thread);
                else
                    Unload();
            }
        }
    }
}