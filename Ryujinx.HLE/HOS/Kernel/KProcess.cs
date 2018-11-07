using ChocolArm64;
using ChocolArm64.Memory;
using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KProcess : KSynchronizationObject
    {
        public const int KernelVersionMajor    = 10;
        public const int KernelVersionMinor    = 4;
        public const int KernelVersionRevision = 0;

        public const int KernelVersionPacked =
            (KernelVersionMajor    << 19) |
            (KernelVersionMinor    << 15) |
            (KernelVersionRevision << 0);

        public KMemoryManager MemoryManager { get; private set; }

        private long TotalMemoryUsage;

        private SortedDictionary<long, KTlsPageInfo> FullTlsPages;
        private SortedDictionary<long, KTlsPageInfo> FreeTlsPages;

        public int DefaultCpuCore { get; private set; }

        public KResourceLimit ResourceLimit { get; private set; }

        private long PersonalMmHeapBase;
        private long PersonalMmHeapPagesCount;

        private ProcessState State;

        private object ProcessLock;
        private object ThreadingLock;

        public KAddressArbiter AddressArbiter { get; private set; }

        private long[] RandomEntropy;

        private bool Signaled;
        private bool UseSystemMemBlocks;

        private string Name;

        private int ThreadCount;

        public int MmuFlags { get; private set; }

        private MemoryRegion MemRegion;

        public KProcessCapabilities Capabilities { get; private set; }

        public long TitleId { get; private set; }
        public long Pid     { get; private set; }

        private long CreationTimestamp;
        private long Entrypoint;
        private long ImageSize;
        private long MainThreadStackSize;
        private long MemoryUsageCapacity;
        private int  Category;

        public KProcessHandleTable HandleTable { get; private set; }

        private long TlsAddress;

        private LinkedList<KThread> Threads;

        public bool IsPaused { get; private set; }

        public Translator Translator { get; private set; }

        public MemoryManager CpuMemory { get; private set; }

        private SvcHandler SvcHandler;

        public KProcess(Horizon System) : base(System)
        {
            ProcessLock   = new object();
            ThreadingLock = new object();

            CpuMemory = new MemoryManager(System.Device.Memory.RamPointer);

            AddressArbiter = new KAddressArbiter(System);

            MemoryManager = new KMemoryManager(System, CpuMemory);

            FullTlsPages = new SortedDictionary<long, KTlsPageInfo>();
            FreeTlsPages = new SortedDictionary<long, KTlsPageInfo>();

            ResourceLimit = new KResourceLimit();

            Capabilities = new KProcessCapabilities();

            RandomEntropy = new long[KScheduler.CpuCoresCount];

            Threads = new LinkedList<KThread>();

            Translator = new Translator();

            SvcHandler = new SvcHandler(System.Device, this);
        }

        public KernelResult InitializeKip(
            ProcessCreationInfo CreationInfo,
            int[]               Caps,
            KPageList           PageList,
            KResourceLimit      ResourceLimit,
            MemoryRegion        MemRegion)
        {
            AddressSpaceType AddrSpaceType = (AddressSpaceType)((CreationInfo.MmuFlags >> 1) & 7);

            bool AslrEnabled = ((CreationInfo.MmuFlags >> 5) & 1) != 0;

            long CodeAddress = CreationInfo.CodeAddress;

            long CodeSize = (long)(uint)CreationInfo.CodePagesCount * KMemoryManager.PageSize;

            KernelResult Result = MemoryManager.InitializeForProcess(
                AddrSpaceType,
                AslrEnabled,
                !AslrEnabled,
                MemRegion,
                CodeAddress,
                CodeSize);

            if (Result != KernelResult.Success)
            {
                return Result;
            }

            if (!ValidateCodeAddressAndSize(CodeAddress, CodeSize))
            {
                return KernelResult.InvalidMemRange;
            }

            Result = MemoryManager.MapPages(
                CodeAddress,
                PageList,
                MemoryState.CodeStatic,
                MemoryPermission.None);

            if (Result != KernelResult.Success)
            {
                return Result;
            }

            Result = Capabilities.InitializeForKernel(Caps, MemoryManager);

            if (Result != KernelResult.Success)
            {
                return Result;
            }

            Pid = System.GetKipId();

            if (Pid == 0 || (ulong)Pid >= Horizon.InitialProcessId)
            {
                throw new InvalidOperationException($"Invalid KIP Id {Pid}.");
            }

            Result = ParseProcessInfo(CreationInfo);

            return Result;
        }

        public KernelResult Initialize(
            ProcessCreationInfo CreationInfo,
            int[]               Caps,
            KResourceLimit      ResourceLimit,
            MemoryRegion        MemRegion)
        {
            long PersonalMmHeapSize = CreationInfo.PersonalMmHeapPagesCount * KMemoryManager.PageSize;

            long CodePagesCount = (long)(uint)CreationInfo.CodePagesCount;

            long NeededSizeForProcess = PersonalMmHeapSize + CodePagesCount * KMemoryManager.PageSize;

            if (NeededSizeForProcess != 0 && ResourceLimit != null)
            {
                if (!ResourceLimit.Reserve(LimitableResource.Memory, NeededSizeForProcess))
                {
                    return KernelResult.ResLimitExceeded;
                }
            }

            void CleanUpForError()
            {
                if (NeededSizeForProcess != 0 && ResourceLimit != null)
                {
                    ResourceLimit.Release(LimitableResource.Memory, NeededSizeForProcess);
                }
            }

            PersonalMmHeapPagesCount = CreationInfo.PersonalMmHeapPagesCount;

            if (PersonalMmHeapPagesCount != 0)
            {

            }

            AddressSpaceType AddrSpaceType = (AddressSpaceType)((CreationInfo.MmuFlags >> 1) & 7);

            bool AslrEnabled = ((CreationInfo.MmuFlags >> 5) & 1) != 0;

            long CodeAddress = CreationInfo.CodeAddress;

            long CodeSize = CodePagesCount * KMemoryManager.PageSize;

            KernelResult Result = MemoryManager.InitializeForProcess(
                AddrSpaceType,
                AslrEnabled,
                !AslrEnabled,
                MemRegion,
                CodeAddress,
                CodeSize);

            if (Result != KernelResult.Success)
            {
                CleanUpForError();

                return Result;
            }

            if (!ValidateCodeAddressAndSize(CodeAddress, CodeSize))
            {
                CleanUpForError();

                return KernelResult.InvalidMemRange;
            }

            Result = MemoryManager.MapNewProcessCode(
                CodeAddress,
                CodePagesCount,
                MemoryState.CodeStatic,
                MemoryPermission.None);

            if (Result != KernelResult.Success)
            {
                CleanUpForError();

                return Result;
            }

            Result = Capabilities.InitializeForUser(Caps, MemoryManager);

            if (Result != KernelResult.Success)
            {
                CleanUpForError();

                return Result;
            }

            Pid = System.GetProcessId();

            if (Pid == -1 || (ulong)Pid < Horizon.InitialProcessId)
            {
                throw new InvalidOperationException($"Invalid Process Id {Pid}.");
            }

            Result = ParseProcessInfo(CreationInfo);

            if (Result != KernelResult.Success)
            {
                CleanUpForError();
            }

            return Result;
        }

        private bool ValidateCodeAddressAndSize(long CodeAddress, long CodeSize)
        {
            long CodeRegionStart;
            long CodeRegionSize;

            switch (MemoryManager.AddrSpaceWidth)
            {
                case 32:
                    CodeRegionStart = 0x200000;
                    CodeRegionSize  = 0x3fe00000;
                    break;

                case 36:
                    CodeRegionStart = 0x8000000;
                    CodeRegionSize  = 0x78000000;
                    break;

                case 39:
                    CodeRegionStart = 0x8000000;
                    CodeRegionSize  = 0x7ff8000000;
                    break;

                default: throw new InvalidOperationException("Invalid address space width on memory manager.");
            }

            long CodeEndAddr = CodeAddress + CodeSize;

            long CodeRegionEnd = CodeRegionStart + CodeRegionSize;

            if ((ulong)CodeEndAddr     <= (ulong)CodeAddress ||
                (ulong)CodeEndAddr - 1 >  (ulong)CodeRegionEnd - 1)
            {
                return false;
            }

            if (MemoryManager.InsideHeapRegion(CodeAddress, CodeSize) ||
                MemoryManager.InsideMapRegion (CodeAddress, CodeSize))
            {
                return false;
            }

            return true;
        }

        private KernelResult ParseProcessInfo(ProcessCreationInfo CreationInfo)
        {
            //Ensure that the current kernel version is equal or above to the minimum required.
            uint RequiredKernelVersionMajor =  (uint)Capabilities.KernelReleaseVersion >> 19;
            uint RequiredKernelVersionMinor = ((uint)Capabilities.KernelReleaseVersion >> 15) & 0xf;

            if (RequiredKernelVersionMajor > KernelVersionMajor)
            {
                return KernelResult.InvalidCombination;
            }

            if (RequiredKernelVersionMajor != KernelVersionMajor && RequiredKernelVersionMajor < 3)
            {
                return KernelResult.InvalidCombination;
            }

            if (RequiredKernelVersionMinor > KernelVersionMinor)
            {
                return KernelResult.InvalidCombination;
            }

            KernelResult Result = AllocateThreadLocalStorage(out TlsAddress);

            if (Result != KernelResult.Success)
            {
                return Result;
            }

            MemoryHelper.FillWithZeros(CpuMemory, TlsAddress, KTlsPageInfo.TlsEntrySize);

            Name = CreationInfo.Name;

            State = ProcessState.Created;

            CreationTimestamp = PerformanceCounter.ElapsedMilliseconds;

            MmuFlags   = CreationInfo.MmuFlags;
            Category   = CreationInfo.Category;
            TitleId    = CreationInfo.TitleId;
            Entrypoint = CreationInfo.CodeAddress;
            ImageSize  = CreationInfo.CodePagesCount * KMemoryManager.PageSize;

            UseSystemMemBlocks = ((MmuFlags >> 6) & 1) != 0;

            switch ((AddressSpaceType)((MmuFlags >> 1) & 7))
            {
                case AddressSpaceType.Addr32Bits:
                case AddressSpaceType.Addr36Bits:
                case AddressSpaceType.Addr39Bits:
                    MemoryUsageCapacity = MemoryManager.HeapRegionEnd -
                                          MemoryManager.HeapRegionStart;
                    break;

                case AddressSpaceType.Addr32BitsNoMap:
                    MemoryUsageCapacity = MemoryManager.HeapRegionEnd -
                                          MemoryManager.HeapRegionStart +
                                          MemoryManager.AliasRegionEnd -
                                          MemoryManager.AliasRegionStart;
                    break;

                default: throw new InvalidOperationException($"Invalid MMU flags value 0x{MmuFlags:x2}.");
            }

            GenerateRandomEntropy();

            return KernelResult.Success;
        }

        public KernelResult AllocateThreadLocalStorage(out long Address)
        {
            System.CriticalSection.Enter();

            KernelResult Result;

            if (FreeTlsPages.Count > 0)
            {
                //If we have free TLS pages available, just use the first one.
                KTlsPageInfo PageInfo = FreeTlsPages.Values.First();

                if (!PageInfo.TryGetFreePage(out Address))
                {
                    throw new InvalidOperationException("Unexpected failure getting free TLS page!");
                }

                if (PageInfo.IsFull())
                {
                    FreeTlsPages.Remove(PageInfo.PageAddr);

                    FullTlsPages.Add(PageInfo.PageAddr, PageInfo);
                }

                Result = KernelResult.Success;
            }
            else
            {
                //Otherwise, we need to create a new one.
                Result = AllocateTlsPage(out KTlsPageInfo PageInfo);

                if (Result == KernelResult.Success)
                {
                    if (!PageInfo.TryGetFreePage(out Address))
                    {
                        throw new InvalidOperationException("Unexpected failure getting free TLS page!");
                    }

                    FreeTlsPages.Add(PageInfo.PageAddr, PageInfo);
                }
                else
                {
                    Address = 0;
                }
            }

            System.CriticalSection.Leave();

            return Result;
        }

        private KernelResult AllocateTlsPage(out KTlsPageInfo PageInfo)
        {
            PageInfo = default(KTlsPageInfo);

            if (!System.UserSlabHeapPages.TryGetItem(out long TlsPagePa))
            {
                return KernelResult.OutOfMemory;
            }

            long RegionStart = MemoryManager.TlsIoRegionStart;

            long RegionPagesCount = (MemoryManager.TlsIoRegionEnd - RegionStart) / KMemoryManager.PageSize;

            KernelResult Result = MemoryManager.AllocateOrMapPa(
                1,
                KMemoryManager.PageSize,
                TlsPagePa,
                true,
                RegionStart,
                RegionPagesCount,
                MemoryState.ThreadLocal,
                MemoryPermission.ReadAndWrite,
                out long TlsPageVa);

            if (Result != KernelResult.Success)
            {
                System.UserSlabHeapPages.Free(TlsPagePa);
            }
            else
            {
                PageInfo = new KTlsPageInfo(TlsPageVa);

                MemoryHelper.FillWithZeros(CpuMemory, TlsPageVa, KMemoryManager.PageSize);
            }

            return Result;
        }

        public KernelResult FreeThreadLocalStorage(long TlsSlotAddr)
        {
            long TlsPageAddr = BitUtils.AlignDown(TlsSlotAddr, KMemoryManager.PageSize);

            System.CriticalSection.Enter();

            KernelResult Result = KernelResult.Success;

            KTlsPageInfo PageInfo = null;

            if (FullTlsPages.TryGetValue(TlsPageAddr, out PageInfo))
            {
                //TLS page was full, free slot and move to free pages tree.
                FullTlsPages.Remove(TlsPageAddr);

                FreeTlsPages.Add(TlsPageAddr, PageInfo);
            }
            else if (!FreeTlsPages.TryGetValue(TlsPageAddr, out PageInfo))
            {
                Result = KernelResult.InvalidAddress;
            }

            if (PageInfo != null)
            {
                PageInfo.FreeTlsSlot(TlsSlotAddr);

                if (PageInfo.IsEmpty())
                {
                    //TLS page is now empty, we should ensure it is removed
                    //from all trees, and free the memory it was using.
                    FreeTlsPages.Remove(TlsPageAddr);

                    System.CriticalSection.Leave();

                    FreeTlsPage(PageInfo);

                    return KernelResult.Success;
                }
            }

            System.CriticalSection.Leave();

            return Result;
        }

        private KernelResult FreeTlsPage(KTlsPageInfo PageInfo)
        {
            KernelResult Result = MemoryManager.ConvertVaToPa(PageInfo.PageAddr, out long TlsPagePa);

            if (Result != KernelResult.Success)
            {
                throw new InvalidOperationException("Unexpected failure translating virtual address to physical.");
            }

            Result = MemoryManager.UnmapForKernel(PageInfo.PageAddr, 1, MemoryState.ThreadLocal);

            if (Result == KernelResult.Success)
            {
                System.UserSlabHeapPages.Free(TlsPagePa);
            }

            return Result;
        }

        private void GenerateRandomEntropy()
        {
            //TODO.
        }

        public KernelResult Start(int MainThreadPriority, long StackSize)
        {
            lock (ProcessLock)
            {
                if (State > ProcessState.CreatedAttached)
                {
                    return KernelResult.InvalidState;
                }

                if (ResourceLimit != null && !ResourceLimit.Reserve(LimitableResource.Thread, 1))
                {
                    return KernelResult.ResLimitExceeded;
                }

                KResourceLimit ThreadResourceLimit = ResourceLimit;
                KResourceLimit MemoryResourceLimit = null;

                if (MainThreadStackSize != 0)
                {
                    throw new InvalidOperationException("Trying to start a process with a invalid state!");
                }

                long StackSizeRounded = BitUtils.AlignUp(StackSize, KMemoryManager.PageSize);

                long NeededSize = StackSizeRounded + ImageSize;

                //Check if the needed size for the code and the stack will fit on the
                //memory usage capacity of this Process. Also check for possible overflow
                //on the above addition.
                if ((ulong)NeededSize > (ulong)MemoryUsageCapacity ||
                    (ulong)NeededSize < (ulong)StackSizeRounded)
                {
                    ThreadResourceLimit?.Release(LimitableResource.Thread, 1);

                    return KernelResult.OutOfMemory;
                }

                if (StackSizeRounded != 0 && ResourceLimit != null)
                {
                    MemoryResourceLimit = ResourceLimit;

                    if (!MemoryResourceLimit.Reserve(LimitableResource.Memory, StackSizeRounded))
                    {
                        ThreadResourceLimit?.Release(LimitableResource.Thread, 1);

                        return KernelResult.ResLimitExceeded;
                    }
                }

                KernelResult Result;

                KThread MainThread = null;

                long StackTop = 0;

                void CleanUpForError()
                {
                    MainThread?.Terminate();
                    HandleTable.Destroy();

                    if (MainThreadStackSize != 0)
                    {
                        long StackBottom = StackTop - MainThreadStackSize;

                        long StackPagesCount = MainThreadStackSize / KMemoryManager.PageSize;

                        MemoryManager.UnmapForKernel(StackBottom, StackPagesCount, MemoryState.Stack);
                    }

                    MemoryResourceLimit?.Release(LimitableResource.Memory, StackSizeRounded);
                    ThreadResourceLimit?.Release(LimitableResource.Thread, 1);
                }

                if (StackSizeRounded != 0)
                {
                    long StackPagesCount = StackSizeRounded / KMemoryManager.PageSize;

                    long RegionStart      =  MemoryManager.StackRegionStart;
                    long RegionPagesCount = (MemoryManager.StackRegionEnd - RegionStart) / KMemoryManager.PageSize;

                    Result = MemoryManager.AllocateOrMapPa(
                        StackPagesCount,
                        KMemoryManager.PageSize,
                        0,
                        false,
                        RegionStart,
                        RegionPagesCount,
                        MemoryState.Stack,
                        MemoryPermission.ReadAndWrite,
                        out long StackBottom);

                    if (Result != KernelResult.Success)
                    {
                        CleanUpForError();

                        return Result;
                    }

                    MainThreadStackSize += StackSizeRounded;

                    StackTop = StackBottom + StackSizeRounded;
                }

                long HeapCapacity = MemoryUsageCapacity - MainThreadStackSize - ImageSize;

                Result = MemoryManager.SetHeapCapacity(HeapCapacity);

                if (Result != KernelResult.Success)
                {
                    CleanUpForError();

                    return Result;
                }

                HandleTable = new KProcessHandleTable(System);

                Result = HandleTable.Initialize(Capabilities.HandleTableSize);

                if (Result != KernelResult.Success)
                {
                    CleanUpForError();

                    return Result;
                }

                MainThread = new KThread(System);

                Result = MainThread.Initialize(
                    Entrypoint,
                    0,
                    StackTop,
                    MainThreadPriority,
                    DefaultCpuCore,
                    this);

                if (Result != KernelResult.Success)
                {
                    CleanUpForError();

                    return Result;
                }

                Result = HandleTable.GenerateHandle(MainThread, out int MainThreadHandle);

                if (Result != KernelResult.Success)
                {
                    CleanUpForError();

                    return Result;
                }

                MainThread.SetEntryArguments(0, MainThreadHandle);

                ProcessState OldState = State;
                ProcessState NewState = State != ProcessState.Created
                    ? ProcessState.Attached
                    : ProcessState.Started;

                SetState(NewState);

                //TODO: We can't call KThread.Start from a non-guest thread.
                //We will need to make some changes to allow the creation of
                //dummy threads that will be used to initialize the current
                //thread on KCoreContext so that GetCurrentThread doesn't fail.
                /* Result = MainThread.Start();

                if (Result != KernelResult.Success)
                {
                    SetState(OldState);

                    CleanUpForError();
                } */

                MainThread.TimeUp();

                return Result;
            }
        }

        private void SetState(ProcessState NewState)
        {
            if (State != NewState)
            {
                State    = NewState;
                Signaled = true;

                Signal();
            }
        }

        public KernelResult InitializeThread(
            KThread Thread,
            long    Entrypoint,
            long    ArgsPtr,
            long    StackTop,
            int     Priority,
            int     CpuCore)
        {
            lock (ProcessLock)
            {
                return Thread.Initialize(Entrypoint, ArgsPtr, StackTop, Priority, CpuCore, this);
            }
        }

        public void SubscribeThreadEventHandlers(CpuThread Context)
        {
            Context.ThreadState.Interrupt += InterruptHandler;
            Context.ThreadState.SvcCall   += SvcHandler.SvcCall;
        }

        private void InterruptHandler(object sender, EventArgs e)
        {
            System.Scheduler.ContextSwitch();
        }

        public void IncrementThreadCount()
        {
            Interlocked.Increment(ref ThreadCount);
        }

        public void DecrementThreadCountAndTerminateIfZero()
        {
            if (Interlocked.Decrement(ref ThreadCount) == 0)
            {
                Terminate();
            }
        }

        public long GetMemoryCapacity()
        {
            //TODO: Personal Mm Heap.
            return 0xcd500000;
        }

        public long GetMemoryUsage()
        {
            //TODO: Personal Mm Heap.
            return ImageSize + MainThreadStackSize + MemoryManager.GetTotalHeapSize();
        }

        public void AddThread(KThread Thread)
        {
            lock (ThreadingLock)
            {
                Thread.ProcessListNode = Threads.AddLast(Thread);
            }
        }

        public void RemoveThread(KThread Thread)
        {
            lock (ThreadingLock)
            {
                Threads.Remove(Thread.ProcessListNode);
            }
        }

        public bool IsAllowedCpuCore(int Core)
        {
            return (Capabilities.AllowedCpuCoresMask & (1L << Core)) != 0;
        }

        public bool IsAllowedPriority(int Priority)
        {
            return (Capabilities.AllowedThreadPriosMask & (1L << Priority)) != 0;
        }

        public override bool IsSignaled()
        {
            return Signaled;
        }

        public KernelResult Terminate()
        {
            KernelResult Result;

            bool ShallTerminate = false;

            lock (ProcessLock)
            {
                if (State >= ProcessState.Started)
                {
                    System.CriticalSection.Enter();

                    if (State == ProcessState.Started  ||
                        State == ProcessState.Crashed  ||
                        State == ProcessState.Attached ||
                        State == ProcessState.DebugSuspended)
                    {
                        SetState(ProcessState.Exiting);

                        ShallTerminate = true;
                    }

                    System.CriticalSection.Leave();

                    Result = KernelResult.Success;
                }
                else
                {
                    Result = KernelResult.InvalidState;
                }
            }

            if (ShallTerminate)
            {
                UnpauseAndTerminateAllThreadsExcept(System.Scheduler.GetCurrentThread());

                HandleTable.Destroy();

                SignalExitForDebugEvent();
                SignalExit();
            }

            return Result;
        }

        private void UnpauseAndTerminateAllThreadsExcept(KThread Thread)
        {
            //TODO.
        }

        private void SignalExitForDebugEvent()
        {
            //TODO: Debug events.
        }

        private void SignalExit()
        {
            if (ResourceLimit != null)
            {
                ResourceLimit.Release(LimitableResource.Memory, GetMemoryUsage());
            }

            System.CriticalSection.Enter();

            SetState(ProcessState.Exited);

            System.CriticalSection.Leave();
        }
    }
}