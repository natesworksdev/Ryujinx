using ChocolArm64.Memory;
using Ryujinx.Common;
using System;
using System.Collections.Generic;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryManager
    {
        public const int PageSize = 0x1000;

        private const int KMemoryBlockSize = 0x40;

        private LinkedList<KMemoryBlock> Blocks;

        private MemoryManager CpuMemory;

        private Horizon System;

        public long AddrSpaceStart { get; private set; }
        public long AddrSpaceEnd   { get; private set; }

        public long CodeRegionStart { get; private set; }
        public long CodeRegionEnd   { get; private set; }

        public long HeapRegionStart { get; private set; }
        public long HeapRegionEnd   { get; private set; }

        private long CurrentHeapAddr;

        public long AliasRegionStart { get; private set; }
        public long AliasRegionEnd   { get; private set; }

        public long StackRegionStart { get; private set; }
        public long StackRegionEnd   { get; private set; }

        public long TlsIoRegionStart { get; private set; }
        public long TlsIoRegionEnd   { get; private set; }

        private long HeapCapacity;

        public long PersonalMmHeapUsage { get; private set; }

        private MemoryRegion MemRegion;

        private bool AslrDisabled;

        public int AddrSpaceWidth { get; private set; }

        private bool IsKernel;
        private bool AslrEnabled;

        private int ContextId;

        private MersenneTwister RandomNumberGenerator;

        public KMemoryManager(Horizon System, MemoryManager CpuMemory)
        {
            this.System    = System;
            this.CpuMemory = CpuMemory;

            Blocks = new LinkedList<KMemoryBlock>();
        }

        private static readonly int[] AddrSpaceSizes = new int[] { 32, 36, 32, 39 };

        public KernelResult InitializeForProcess(
            AddressSpaceType AddrSpaceType,
            bool             AslrEnabled,
            bool             AslrDisabled,
            MemoryRegion     MemRegion,
            long             Address,
            long             Size)
        {
            if ((uint)AddrSpaceType > (uint)AddressSpaceType.Addr39Bits)
            {
                throw new ArgumentException(nameof(AddrSpaceType));
            }

            ContextId = System.ContextIdManager.GetId();

            long AddrSpaceBase = 0;
            long AddrSpaceSize = 1L << AddrSpaceSizes[(int)AddrSpaceType];

            KernelResult Result = CreateUserAddressSpace(
                AddrSpaceType,
                AslrEnabled,
                AslrDisabled,
                AddrSpaceBase,
                AddrSpaceSize,
                MemRegion,
                Address,
                Size);

            if (Result != KernelResult.Success)
            {
                System.ContextIdManager.PutId(ContextId);
            }

            return Result;
        }

        private class Region
        {
            public long Start;
            public long End;
            public long Size;
            public long AslrOffset;
        }

        private KernelResult CreateUserAddressSpace(
            AddressSpaceType AddrSpaceType,
            bool             AslrEnabled,
            bool             AslrDisabled,
            long             AddrSpaceStart,
            long             AddrSpaceEnd,
            MemoryRegion     MemRegion,
            long             Address,
            long             Size)
        {
            long EndAddr = Address + Size;

            Region AliasRegion = new Region();
            Region HeapRegion  = new Region();
            Region StackRegion = new Region();
            Region TlsIoRegion = new Region();

            long CodeRegionSize;
            long StackAndTlsIoStart;
            long StackAndTlsIoEnd;
            long BaseAddress;

            switch (AddrSpaceType)
            {
                case AddressSpaceType.Addr32Bits:
                    AliasRegion.Size   = 0x40000000;
                    HeapRegion.Size    = 0x40000000;
                    StackRegion.Size   = 0;
                    TlsIoRegion.Size   = 0;
                    CodeRegionStart    = 0x200000;
                    CodeRegionSize     = 0x3fe00000;
                    StackAndTlsIoStart = 0x200000;
                    StackAndTlsIoEnd   = 0x40000000;
                    BaseAddress        = 0x200000;
                    AddrSpaceWidth     = 32;
                    break;

                case AddressSpaceType.Addr36Bits:
                    AliasRegion.Size   = 0x180000000;
                    HeapRegion.Size    = 0x180000000;
                    StackRegion.Size   = 0;
                    TlsIoRegion.Size   = 0;
                    CodeRegionStart    = 0x8000000;
                    CodeRegionSize     = 0x78000000;
                    StackAndTlsIoStart = 0x8000000;
                    StackAndTlsIoEnd   = 0x80000000;
                    BaseAddress        = 0x8000000;
                    AddrSpaceWidth     = 36;
                    break;

                case AddressSpaceType.Addr32BitsNoMap:
                    AliasRegion.Size   = 0;
                    HeapRegion.Size    = 0x80000000;
                    StackRegion.Size   = 0;
                    TlsIoRegion.Size   = 0;
                    CodeRegionStart    = 0x200000;
                    CodeRegionSize     = 0x3fe00000;
                    StackAndTlsIoStart = 0x200000;
                    StackAndTlsIoEnd   = 0x40000000;
                    BaseAddress        = 0x200000;
                    AddrSpaceWidth     = 32;
                    break;

                case AddressSpaceType.Addr39Bits:
                    AliasRegion.Size   = 0x1000000000;
                    HeapRegion.Size    = 0x180000000;
                    StackRegion.Size   = 0x80000000;
                    TlsIoRegion.Size   = 0x1000000000;
                    CodeRegionStart    = BitUtils.AlignDown(Address, 0x200000);
                    CodeRegionSize     = BitUtils.AlignUp  (EndAddr, 0x200000) - CodeRegionStart;
                    StackAndTlsIoStart = 0;
                    StackAndTlsIoEnd   = 0;
                    BaseAddress        = 0x8000000;
                    AddrSpaceWidth     = 39;
                    break;

                default: throw new ArgumentException(nameof(AddrSpaceType));
            }

            CodeRegionEnd = CodeRegionStart + CodeRegionSize;

            long MapBaseAddress;
            long MapAvailableSize;

            if (CodeRegionStart - BaseAddress >= AddrSpaceEnd - CodeRegionEnd)
            {
                //Has more space before the start of the code region.
                MapBaseAddress   = BaseAddress;
                MapAvailableSize = CodeRegionStart - BaseAddress;
            }
            else
            {
                //Has more space after the end of the code region.
                MapBaseAddress   = CodeRegionEnd;
                MapAvailableSize = AddrSpaceEnd - CodeRegionEnd;
            }

            long MapTotalSize = AliasRegion.Size + HeapRegion.Size + StackRegion.Size + TlsIoRegion.Size;

            long AslrMaxOffset = MapAvailableSize - MapTotalSize;

            this.AddrSpaceStart = AddrSpaceStart;
            this.AddrSpaceEnd   = AddrSpaceEnd;

            if (MapAvailableSize < MapTotalSize)
            {
                return KernelResult.OutOfMemory;
            }

            if (AslrEnabled)
            {
                AliasRegion.AslrOffset = GetRandomValue(0, AslrMaxOffset >> 21) << 21;
                HeapRegion.AslrOffset  = GetRandomValue(0, AslrMaxOffset >> 21) << 21;
                StackRegion.AslrOffset = GetRandomValue(0, AslrMaxOffset >> 21) << 21;
                TlsIoRegion.AslrOffset = GetRandomValue(0, AslrMaxOffset >> 21) << 21;
            }

            //Regions are sorted based on ASLR offset.
            //When ASLR is disabled, the order is Map, Heap, NewMap and TlsIo.
            AliasRegion.Start = MapBaseAddress    + AliasRegion.AslrOffset;
            AliasRegion.End   = AliasRegion.Start + AliasRegion.Size;
            HeapRegion.Start  = MapBaseAddress    + HeapRegion.AslrOffset;
            HeapRegion.End    = HeapRegion.Start  + HeapRegion.Size;
            StackRegion.Start = MapBaseAddress    + StackRegion.AslrOffset;
            StackRegion.End   = StackRegion.Start + StackRegion.Size;
            TlsIoRegion.Start = MapBaseAddress    + TlsIoRegion.AslrOffset;
            TlsIoRegion.End   = TlsIoRegion.Start + TlsIoRegion.Size;

            SortRegion(HeapRegion, AliasRegion);

            if (StackRegion.Size != 0)
            {
                SortRegion(StackRegion, AliasRegion);
                SortRegion(StackRegion, HeapRegion);
            }
            else
            {
                StackRegion.Start = StackAndTlsIoStart;
                StackRegion.End   = StackAndTlsIoEnd;
            }

            if (TlsIoRegion.Size != 0)
            {
                SortRegion(TlsIoRegion, AliasRegion);
                SortRegion(TlsIoRegion, HeapRegion);
                SortRegion(TlsIoRegion, StackRegion);
            }
            else
            {
                TlsIoRegion.Start = StackAndTlsIoStart;
                TlsIoRegion.End   = StackAndTlsIoEnd;
            }

            AliasRegionStart = AliasRegion.Start;
            AliasRegionEnd   = AliasRegion.End;
            HeapRegionStart  = HeapRegion.Start;
            HeapRegionEnd    = HeapRegion.End;
            StackRegionStart = StackRegion.Start;
            StackRegionEnd   = StackRegion.End;
            TlsIoRegionStart = TlsIoRegion.Start;
            TlsIoRegionEnd   = TlsIoRegion.End;

            CurrentHeapAddr     = HeapRegionStart;
            HeapCapacity        = 0;
            PersonalMmHeapUsage = 0;

            this.MemRegion    = MemRegion;
            this.AslrDisabled = AslrDisabled;

            InitializeBlocks(AddrSpaceStart, AddrSpaceEnd);

            return KernelResult.Success;
        }

        private long GetRandomValue(long Min, long Max)
        {
            if (RandomNumberGenerator == null)
            {
                RandomNumberGenerator = new MersenneTwister(0);
            }

            return RandomNumberGenerator.GenRandomNumber(Min, Max);
        }

        private static void SortRegion(Region Lhs, Region Rhs)
        {
            if (Lhs.AslrOffset < Rhs.AslrOffset)
            {
                Rhs.Start += Lhs.Size;
                Rhs.End   += Lhs.Size;
            }
            else
            {
                Lhs.Start += Rhs.Size;
                Lhs.End   += Rhs.Size;
            }
        }

        private void InitializeBlocks(long AddrSpaceStart, long AddrSpaceEnd)
        {
            long AddrSpacePagesCount = (long)((ulong)(AddrSpaceEnd - AddrSpaceStart) / PageSize);

            InsertBlock(AddrSpaceStart, AddrSpacePagesCount, MemoryState.Unmapped);
        }

        public KernelResult MapPages(
            long             Address,
            KPageList        PageList,
            MemoryState      State,
            MemoryPermission Permission)
        {
            long PagesCount = PageList.GetPagesCount();

            long Size = PagesCount * PageSize;

            if (!ValidateRegionForState(Address, Size, State))
            {
                return KernelResult.InvalidMemState;
            }

            lock (Blocks)
            {
                if (!IsUnmapped(Address, PagesCount * PageSize))
                {
                    return KernelResult.InvalidMemState;
                }

                KernelResult Result = MapPages(Address, PageList, Permission);

                if (Result == KernelResult.Success)
                {
                    InsertBlock(Address, PagesCount, State, Permission);
                }

                return Result;
            }
        }

        public KernelResult UnmapPages(long Address, KPageList PageList, MemoryState StateExpected)
        {
            long PagesCount = PageList.GetPagesCount();

            long Size = PagesCount * PageSize;

            long EndAddr = Address + Size;

            long AddrSpacePagesCount = (long)((ulong)(AddrSpaceEnd - AddrSpaceStart) / PageSize);

            if ((ulong)AddrSpaceStart > (ulong)Address)
            {
                return KernelResult.InvalidMemState;
            }

            if ((ulong)AddrSpacePagesCount < (ulong)PagesCount)
            {
                return KernelResult.InvalidMemState;
            }

            if ((ulong)(EndAddr - 1) > (ulong)(AddrSpaceEnd - 1))
            {
                return KernelResult.InvalidMemState;
            }

            lock (Blocks)
            {
                KPageList CurrentPageList = new KPageList();

                AddVaRangeToPageList(CurrentPageList, Address, Address + Size);

                if (!CurrentPageList.IsEqual(PageList))
                {
                    return KernelResult.InvalidMemRange;
                }

                if (CheckRange(
                    Address,
                    Size,
                    MemoryState.Mask,
                    StateExpected,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState State,
                    out _,
                    out _))
                {
                    KernelResult Result = MmuUnmap(Address, PagesCount);

                    if (Result == KernelResult.Success)
                    {
                        InsertBlock(Address, PagesCount, MemoryState.Unmapped);
                    }

                    return Result;
                }
                else
                {
                    return KernelResult.InvalidMemState;
                }
            }
        }

        public KernelResult MapNormalMemory(long Address, long Size, MemoryPermission Permission)
        {
            //TODO.
            return KernelResult.Success;
        }

        public KernelResult MapIoMemory(long Address, long Size, MemoryPermission Permission)
        {
            //TODO.
            return KernelResult.Success;
        }

        public KernelResult AllocateOrMapPa(
            long             NeededPagesCount,
            int              Alignment,
            long             SrcPa,
            bool             Map,
            long             RegionStart,
            long             RegionPagesCount,
            MemoryState      State,
            MemoryPermission Permission,
            out long         Address)
        {
            Address = 0;

            long RegionSize = RegionPagesCount * PageSize;

            long RegionEndAddr = RegionStart + RegionSize;

            if (!ValidateRegionForState(RegionStart, RegionSize, State))
            {
                return KernelResult.InvalidMemState;
            }

            if ((ulong)RegionPagesCount <= (ulong)NeededPagesCount)
            {
                return KernelResult.OutOfMemory;
            }

            long ReservedPagesCount = IsKernel ? 1 : 4;

            lock (Blocks)
            {
                if (AslrEnabled)
                {
                    long TotalNeededSize = (ReservedPagesCount + NeededPagesCount) * PageSize;

                    long RemainingPages = RegionPagesCount - NeededPagesCount;

                    long AslrMaxOffset = (long)((ulong)((RemainingPages + ReservedPagesCount) * PageSize) / (ulong)Alignment);

                    for (int Attempt = 0; Attempt < 8; Attempt++)
                    {
                        Address = BitUtils.AlignDown(RegionStart + GetRandomValue(0, AslrMaxOffset) * Alignment, Alignment);

                        long EndAddr = Address + TotalNeededSize;

                        KMemoryInfo Info = FindBlock(Address).GetInfo();

                        if (Info.State != MemoryState.Unmapped)
                        {
                            continue;
                        }

                        long CurrBaseAddr = Info.Address + ReservedPagesCount * PageSize;
                        long CurrEndAddr  = Info.Address + Info.Size;

                        if ((ulong)Address     >= (ulong)RegionStart       &&
                            (ulong)Address     >= (ulong)CurrBaseAddr      &&
                            (ulong)EndAddr - 1 <= (ulong)RegionEndAddr - 1 &&
                            (ulong)EndAddr - 1 <= (ulong)CurrEndAddr   - 1)
                        {
                            break;
                        }
                    }

                    if (Address == 0)
                    {
                        long AslrPage = GetRandomValue(0, AslrMaxOffset);

                        Address = FindFirstFit(
                            RegionStart      + AslrPage * PageSize,
                            RegionPagesCount - AslrPage,
                            NeededPagesCount,
                            Alignment,
                            0,
                            ReservedPagesCount);
                    }
                }

                if (Address == 0)
                {
                    Address = FindFirstFit(
                        RegionStart,
                        RegionPagesCount,
                        NeededPagesCount,
                        Alignment,
                        0,
                        ReservedPagesCount);
                }

                if (Address == 0)
                {
                    return KernelResult.OutOfMemory;
                }

                MemoryOperation Operation = Map
                    ? MemoryOperation.MapPa
                    : MemoryOperation.Allocate;

                KernelResult Result = DoMmuOperation(
                    Address,
                    NeededPagesCount,
                    SrcPa,
                    Map,
                    Permission,
                    Operation);

                if (Result != KernelResult.Success)
                {
                    return Result;
                }

                InsertBlock(Address, NeededPagesCount, State, Permission);
            }

            return KernelResult.Success;
        }

        public KernelResult MapNewProcessCode(
            long             Address,
            long             PagesCount,
            MemoryState      State,
            MemoryPermission Permission)
        {
            long Size = PagesCount * PageSize;

            if (!ValidateRegionForState(Address, Size, State))
            {
                return KernelResult.InvalidMemState;
            }

            lock (Blocks)
            {
                if (!IsUnmapped(Address, Size))
                {
                    return KernelResult.InvalidMemState;
                }

                KernelResult Result = DoMmuOperation(
                    Address,
                    PagesCount,
                    0,
                    false,
                    Permission,
                    MemoryOperation.Allocate);

                if (Result == KernelResult.Success)
                {
                    InsertBlock(Address, PagesCount, State, Permission);
                }

                return Result;
            }
        }

        public KernelResult UnmapProcessCodeMemory(long Dst, long Src, long Size)
        {
            long PagesCount = (long)((ulong)Size / PageSize);

            lock (Blocks)
            {
                bool Success = CheckRange(
                    Dst,
                    Size,
                    MemoryState.Mask,
                    MemoryState.CodeStatic,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out _,
                    out _,
                    out _);

                Success &= CheckRange(
                    Src,
                    Size,
                    MemoryState.Mask,
                    MemoryState.Heap,
                    MemoryPermission.Mask,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out _,
                    out _,
                    out _);

                if (Success)
                {
                    KernelResult Result = MmuUnmap(Dst, PagesCount);

                    if (Result != KernelResult.Success)
                    {
                        return Result;
                    }

                    InsertBlock(Dst, PagesCount, MemoryState.Unmapped);
                    InsertBlock(Src, PagesCount, MemoryState.Heap, MemoryPermission.ReadAndWrite);

                    return KernelResult.Success;
                }
                else
                {
                    return KernelResult.InvalidMemState;
                }
            }
        }

        public KernelResult SetHeapSize(long Size, out long Address)
        {
            Address = 0;

            if ((ulong)Size > (ulong)(HeapRegionEnd - HeapRegionStart))
            {
                return KernelResult.OutOfMemory;
            }

            long CurrentHeapSize = GetHeapSize();

            if ((ulong)CurrentHeapSize <= (ulong)Size)
            {
                //Expand.
                long DiffSize = Size - CurrentHeapSize;

                lock (Blocks)
                {
                    long PagesCount = (long)((ulong)DiffSize / PageSize);

                    KMemoryRegionManager Region = GetMemoryRegionManager();

                    KernelResult Result = Region.AllocatePages(PagesCount, AslrDisabled, out KPageList PageList);

                    if (Result != KernelResult.Success)
                    {
                        return Result;
                    }

                    if (!IsUnmapped(CurrentHeapAddr, DiffSize))
                    {
                        return KernelResult.InvalidMemState;
                    }

                    Result = DoMmuOperation(
                        CurrentHeapAddr,
                        PagesCount,
                        PageList,
                        MemoryPermission.ReadAndWrite,
                        MemoryOperation.MapVa);

                    if (Result != KernelResult.Success)
                    {
                        return Result;
                    }

                    InsertBlock(CurrentHeapAddr, PagesCount, MemoryState.Heap, MemoryPermission.ReadAndWrite);
                }
            }
            else
            {
                //Shrink.
                long FreeAddr = HeapRegionStart + Size;
                long DiffSize = CurrentHeapSize - Size;

                lock (Blocks)
                {
                    if (!CheckRange(
                        FreeAddr,
                        DiffSize,
                        MemoryState.Mask,
                        MemoryState.Heap,
                        MemoryPermission.Mask,
                        MemoryPermission.ReadAndWrite,
                        MemoryAttribute.Mask,
                        MemoryAttribute.None,
                        MemoryAttribute.IpcAndDeviceMapped,
                        out _,
                        out _,
                        out _))
                    {
                        return KernelResult.InvalidMemState;
                    }

                    long PagesCount = (long)((ulong)DiffSize / PageSize);

                    KernelResult Result = MmuUnmap(FreeAddr, PagesCount);

                    if (Result != KernelResult.Success)
                    {
                        return Result;
                    }

                    InsertBlock(FreeAddr, PagesCount, MemoryState.Unmapped);
                }
            }

            CurrentHeapAddr = HeapRegionStart + Size;

            Address = HeapRegionStart;

            return KernelResult.Success;
        }

        public long GetTotalHeapSize()
        {
            lock (Blocks)
            {
                return GetHeapSize() + PersonalMmHeapUsage;
            }
        }

        private long GetHeapSize()
        {
            return CurrentHeapAddr - HeapRegionStart;
        }

        public KernelResult SetHeapCapacity(long Capacity)
        {
            lock (Blocks)
            {
                HeapCapacity = Capacity;
            }

            return KernelResult.Success;
        }

        public long SetMemoryAttribute(
            long            Address,
            long            Size,
            MemoryAttribute AttributeMask,
            MemoryAttribute AttributeValue)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Address,
                    Size,
                    MemoryState.AttributeChangeAllowed,
                    MemoryState.AttributeChangeAllowed,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.BorrowedAndIpcMapped,
                    MemoryAttribute.None,
                    MemoryAttribute.DeviceMappedAndUncached,
                    out MemoryState      State,
                    out MemoryPermission Permission,
                    out MemoryAttribute  Attribute))
                {
                    long PagesCount = (long)((ulong)Size / PageSize);

                    Attribute &= ~AttributeMask;
                    Attribute |=  AttributeMask & AttributeValue;

                    InsertBlock(Address, PagesCount, State, Permission, Attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public KMemoryInfo QueryMemory(long Address)
        {
            if ((ulong)Address >= (ulong)AddrSpaceStart &&
                (ulong)Address <  (ulong)AddrSpaceEnd)
            {
                lock (Blocks)
                {
                    return FindBlock(Address).GetInfo();
                }
            }
            else
            {
                return new KMemoryInfo(
                    AddrSpaceEnd,
                    -AddrSpaceEnd,
                    MemoryState.Reserved,
                    MemoryPermission.None,
                    MemoryAttribute.None,
                    0,
                    0);
            }
        }

        public KernelResult Map(long Dst, long Src, long Size)
        {
            bool Success;

            lock (Blocks)
            {
                Success = CheckRange(
                    Src,
                    Size,
                    MemoryState.MapAllowed,
                    MemoryState.MapAllowed,
                    MemoryPermission.Mask,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState SrcState,
                    out _,
                    out _);

                Success &= IsUnmapped(Dst, Size);

                if (Success)
                {
                    long PagesCount = (long)((ulong)Size / PageSize);

                    KPageList PageList = new KPageList();

                    AddVaRangeToPageList(PageList, Src, Src + Size);

                    KernelResult Result = MmuChangePermission(Src, PagesCount, MemoryPermission.None);

                    if (Result != KernelResult.Success)
                    {
                        return Result;
                    }

                    Result = MapPages(Dst, PageList, MemoryPermission.ReadAndWrite);

                    if (Result != KernelResult.Success)
                    {
                        if (MmuChangePermission(Src, PagesCount, MemoryPermission.ReadAndWrite) != KernelResult.Success)
                        {
                            throw new InvalidOperationException("Unexpected failure reverting memory permission.");
                        }

                        return Result;
                    }

                    InsertBlock(Src, PagesCount, SrcState, MemoryPermission.None, MemoryAttribute.Borrowed);
                    InsertBlock(Dst, PagesCount, MemoryState.Stack, MemoryPermission.ReadAndWrite);

                    return KernelResult.Success;
                }
                else
                {
                    return KernelResult.InvalidMemState;
                }
            }
        }

        public KernelResult UnmapForKernel(long Address, long PagesCount, MemoryState StateExpected)
        {
            long Size = PagesCount * PageSize;

            lock (Blocks)
            {
                if (CheckRange(
                    Address,
                    Size,
                    MemoryState.Mask,
                    StateExpected,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out _,
                    out _,
                    out _))
                {
                    KernelResult Result = MmuUnmap(Address, PagesCount);

                    if (Result == KernelResult.Success)
                    {
                        InsertBlock(Address, PagesCount, MemoryState.Unmapped);
                    }

                    return KernelResult.Success;
                }
                else
                {
                    return KernelResult.InvalidMemState;
                }
            }
        }

        public KernelResult Unmap(long Dst, long Src, long Size)
        {
            bool Success;

            lock (Blocks)
            {
                Success = CheckRange(
                    Src,
                    Size,
                    MemoryState.MapAllowed,
                    MemoryState.MapAllowed,
                    MemoryPermission.Mask,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.Borrowed,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState SrcState,
                    out _,
                    out _);

                Success &= CheckRange(
                    Dst,
                    Size,
                    MemoryState.Mask,
                    MemoryState.Stack,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out _,
                    out MemoryPermission DstPermission,
                    out _);

                if (Success)
                {
                    long PagesCount = (long)((ulong)Size / PageSize);

                    KPageList SrcPageList = new KPageList();
                    KPageList DstPageList = new KPageList();

                    AddVaRangeToPageList(SrcPageList, Src, Src + Size);
                    AddVaRangeToPageList(DstPageList, Dst, Dst + Size);

                    if (!DstPageList.IsEqual(SrcPageList))
                    {
                        return KernelResult.InvalidMemRange;
                    }

                    KernelResult Result = MmuUnmap(Dst, PagesCount);

                    if (Result != KernelResult.Success)
                    {
                        return Result;
                    }

                    Result = MmuChangePermission(Src, PagesCount, MemoryPermission.ReadAndWrite);

                    if (Result != KernelResult.Success)
                    {
                        MapPages(Dst, DstPageList, DstPermission);

                        return Result;
                    }

                    InsertBlock(Src, PagesCount, SrcState, MemoryPermission.ReadAndWrite);
                    InsertBlock(Dst, PagesCount, MemoryState.Unmapped);

                    return KernelResult.Success;
                }
                else
                {
                    return KernelResult.InvalidMemState;
                }
            }
        }

        public long ReserveTransferMemory(long Address, long Size, MemoryPermission Permission)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Address,
                    Size,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryPermission.Mask,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState State,
                    out _,
                    out MemoryAttribute Attribute))
                {
                    long PagesCount = (long)((ulong)Size / PageSize);

                    Attribute |= MemoryAttribute.Borrowed;

                    InsertBlock(Address, PagesCount, State, Permission, Attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ResetTransferMemory(long Address, long Size)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Address,
                    Size,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.Borrowed,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState State,
                    out _,
                    out _))
                {
                    long PagesCount = (long)((ulong)Size / PageSize);

                    InsertBlock(Address, PagesCount, State, MemoryPermission.ReadAndWrite);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public KernelResult SetProcessMemoryPermission(long Address, long Size, MemoryPermission Permission)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Address,
                    Size,
                    MemoryState.ProcessPermissionChangeAllowed,
                    MemoryState.ProcessPermissionChangeAllowed,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState      OldState,
                    out MemoryPermission OldPermission,
                    out _))
                {
                    MemoryState NewState = OldState;

                    //If writing into the code region is allowed, then we need
                    //to change it to mutable.
                    if ((Permission & MemoryPermission.Write) != 0)
                    {
                        if (OldState == MemoryState.CodeStatic)
                        {
                            NewState = MemoryState.CodeMutable;
                        }
                        else if (OldState == MemoryState.ModCodeStatic)
                        {
                            NewState = MemoryState.ModCodeMutable;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Memory state \"{OldState}\" not valid for this operation.");
                        }
                    }

                    if (NewState != OldState || Permission != OldPermission)
                    {
                        long PagesCount = (long)((ulong)Size / PageSize);

                        MemoryOperation Operation = (Permission & MemoryPermission.Execute) != 0
                            ? MemoryOperation.ChangePermsAndAttributes
                            : MemoryOperation.ChangePermRw;

                        KernelResult Result = DoMmuOperation(Address, PagesCount, 0, false, Permission, Operation);

                        if (Result != KernelResult.Success)
                        {
                            return Result;
                        }

                        InsertBlock(Address, PagesCount, NewState, Permission);
                    }

                    return KernelResult.Success;
                }
                else
                {
                    return KernelResult.InvalidMemState;
                }
            }
        }

        public KernelResult MapPhysicalMemory(long Address, long Size)
        {
            long EndAddr = Address + Size;

            lock (Blocks)
            {
                long MappedSize = 0;

                KMemoryInfo Info;

                LinkedListNode<KMemoryBlock> Node = FindBlockNode(Address);

                do
                {
                    Info = Node.Value.GetInfo();

                    if (Info.State != MemoryState.Unmapped)
                    {
                        MappedSize += GetSizeInRange(Info, Address, EndAddr);
                    }

                    Node = Node.Next;
                }
                while ((ulong)(Info.Address + Info.Size) < (ulong)EndAddr && Node != null);

                if (MappedSize == Size)
                {
                    return KernelResult.Success;
                }

                long RemainingSize = Size - MappedSize;

                long RemainingPages = (long)((ulong)RemainingSize / PageSize);

                KProcess CurrentProcess = System.Scheduler.GetCurrentProcess();

                if (CurrentProcess.ResourceLimit != null &&
                   !CurrentProcess.ResourceLimit.Reserve(LimitableResource.Memory, RemainingSize))
                {
                    return KernelResult.ResLimitExceeded;
                }

                KMemoryRegionManager Region = GetMemoryRegionManager();

                KernelResult Result = Region.AllocatePages(RemainingPages, AslrDisabled, out KPageList PageList);

                if (Result != KernelResult.Success)
                {
                    CurrentProcess.ResourceLimit?.Release(LimitableResource.Memory, RemainingSize);

                    return Result;
                }

                MapPhysicalMemory(PageList, Address, EndAddr);

                PersonalMmHeapUsage += RemainingSize;

                long PagesCount = (long)((ulong)Size / PageSize);

                InsertBlock(
                    Address,
                    PagesCount,
                    MemoryState.Unmapped,
                    MemoryPermission.None,
                    MemoryAttribute.None,
                    MemoryState.Heap,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.None);
            }

            return KernelResult.Success;
        }

        public KernelResult UnmapPhysicalMemory(long Address, long Size)
        {
            long EndAddr = Address + Size;

            lock (Blocks)
            {
                //Scan, ensure that the region can be unmapped (all blocks are heap or
                //already unmapped), fill pages list for freeing memory.
                long HeapMappedSize = 0;

                KPageList PageList = new KPageList();

                KMemoryInfo Info;

                LinkedListNode<KMemoryBlock> BaseNode = FindBlockNode(Address);

                LinkedListNode<KMemoryBlock> Node = BaseNode;

                do
                {
                    Info = Node.Value.GetInfo();

                    if (Info.State == MemoryState.Heap)
                    {
                        if (Info.Attribute != MemoryAttribute.None)
                        {
                            return KernelResult.InvalidMemState;
                        }

                        long BlockSize    = GetSizeInRange(Info, Address, EndAddr);
                        long BlockAddress = GetAddrInRange(Info, Address);

                        AddVaRangeToPageList(PageList, BlockAddress, BlockAddress + BlockSize);

                        HeapMappedSize += BlockSize;
                    }
                    else if (Info.State != MemoryState.Unmapped)
                    {
                        return KernelResult.InvalidMemState;
                    }

                    Node = Node.Next;
                }
                while ((ulong)(Info.Address + Info.Size) < (ulong)EndAddr && Node != null);

                if (HeapMappedSize == 0)
                {
                    return KernelResult.Success;
                }

                //Try to unmap all the heap mapped memory inside range.
                KernelResult Result = KernelResult.Success;

                Node = BaseNode;

                do
                {
                    Info = Node.Value.GetInfo();

                    if (Info.State == MemoryState.Heap)
                    {
                        long BlockSize    = GetSizeInRange(Info, Address, EndAddr);
                        long BlockAddress = GetAddrInRange(Info, Address);

                        long BlockPagesCount = (long)((ulong)BlockSize / PageSize);

                        Result = MmuUnmap(BlockAddress, BlockPagesCount);

                        if (Result != KernelResult.Success)
                        {
                            //If we failed to unmap, we need to remap everything back again.
                            MapPhysicalMemory(PageList, Address, BlockAddress + BlockSize);

                            break;
                        }
                    }

                    Node = Node.Next;
                }
                while ((ulong)(Info.Address + Info.Size) < (ulong)EndAddr && Node != null);

                if (Result == KernelResult.Success)
                {
                    GetMemoryRegionManager().FreePages(PageList);

                    PersonalMmHeapUsage -= HeapMappedSize;

                    KProcess CurrentProcess = System.Scheduler.GetCurrentProcess();

                    CurrentProcess.ResourceLimit?.Release(LimitableResource.Memory, HeapMappedSize);

                    long PagesCount = (long)((ulong)Size / PageSize);

                    InsertBlock(Address, PagesCount, MemoryState.Unmapped);
                }

                return Result;
            }
        }

        private void MapPhysicalMemory(KPageList PageList, long Address, long EndAddr)
        {
            KMemoryInfo Info;

            LinkedListNode<KMemoryBlock> Node = FindBlockNode(Address);

            LinkedListNode<KPageNode> PageListNode = PageList.Nodes.First;

            KPageNode PageNode = PageListNode.Value;

            long SrcPa      = PageNode.Address;
            long SrcPaPages = PageNode.PagesCount;

            do
            {
                Info = Node.Value.GetInfo();

                if (Info.State == MemoryState.Unmapped)
                {
                    long BlockSize = GetSizeInRange(Info, Address, EndAddr);

                    long DstVaPages = (long)((ulong)BlockSize / PageSize);

                    long DstVa = GetAddrInRange(Info, Address);

                    while (DstVaPages > 0)
                    {
                        if (PageNode.PagesCount == 0)
                        {
                            PageListNode = PageListNode.Next;

                            PageNode = PageListNode.Value;

                            SrcPa      = PageNode.Address;
                            SrcPaPages = PageNode.PagesCount;
                        }

                        long PagesCount = PageNode.PagesCount;

                        if (PagesCount > DstVaPages)
                        {
                            PagesCount = DstVaPages;
                        }

                        DoMmuOperation(
                            DstVa,
                            PagesCount,
                            SrcPa,
                            true,
                            MemoryPermission.ReadAndWrite,
                            MemoryOperation.MapPa);

                        DstVa      += PagesCount * PageSize;
                        SrcPa      += PagesCount * PageSize;
                        SrcPaPages -= PagesCount;
                        DstVaPages -= PagesCount;
                    }
                }

                Node = Node.Next;
            }
            while ((ulong)(Info.Address + Info.Size) < (ulong)EndAddr && Node != null);
        }

        private static long GetSizeInRange(KMemoryInfo Info, long Start, long End)
        {
            long EndAddr = Info.Size + Info.Address;
            long Size    = Info.Size;

            if ((ulong)Info.Address < (ulong)Start)
            {
                Size -= Start - Info.Address;
            }

            if ((ulong)EndAddr > (ulong)End)
            {
                Size -= EndAddr - End;
            }

            return Size;
        }

        private static long GetAddrInRange(KMemoryInfo Info, long Start)
        {
            if ((ulong)Info.Address < (ulong)Start)
            {
                return Start;
            }

            return Info.Address;
        }

        private void AddVaRangeToPageList(KPageList PageList, long Start, long End)
        {
            long Address = Start;

            while ((ulong)Address < (ulong)End)
            {
                KernelResult Result = ConvertVaToPa(Address, out long Pa);

                if (Result != KernelResult.Success)
                {
                    throw new InvalidOperationException("Unexpected failure translating virtual address.");
                }

                PageList.AddRange(Pa, 1);

                Address += PageSize;
            }
        }

        public bool HleIsUnmapped(long Address, long Size)
        {
            bool Result = false;

            lock (Blocks)
            {
                Result = IsUnmapped(Address, Size);
            }

            return Result;
        }

        private bool IsUnmapped(long Address, long Size)
        {
            return CheckRange(
                Address,
                Size,
                MemoryState.Mask,
                MemoryState.Unmapped,
                MemoryPermission.Mask,
                MemoryPermission.None,
                MemoryAttribute.Mask,
                MemoryAttribute.None,
                MemoryAttribute.IpcAndDeviceMapped,
                out _,
                out _,
                out _);
        }

        private bool CheckRange(
            long                 Address,
            long                 Size,
            MemoryState          StateMask,
            MemoryState          StateExpected,
            MemoryPermission     PermissionMask,
            MemoryPermission     PermissionExpected,
            MemoryAttribute      AttributeMask,
            MemoryAttribute      AttributeExpected,
            MemoryAttribute      AttributeIgnoreMask,
            out MemoryState      OutState,
            out MemoryPermission OutPermission,
            out MemoryAttribute  OutAttribute)
        {
            long EndAddr = Address + Size - 1;

            LinkedListNode<KMemoryBlock> Node = FindBlockNode(Address);

            KMemoryInfo Info = Node.Value.GetInfo();

            MemoryState      FirstState      = Info.State;
            MemoryPermission FirstPermission = Info.Permission;
            MemoryAttribute  FirstAttribute  = Info.Attribute;

            do
            {
                Info = Node.Value.GetInfo();

                //Check if the block state matches what we expect.
                if ( FirstState                             != Info.State                             ||
                     FirstPermission                        != Info.Permission                        ||
                    (Info.Attribute  & AttributeMask)       != AttributeExpected                      ||
                    (FirstAttribute  | AttributeIgnoreMask) != (Info.Attribute | AttributeIgnoreMask) ||
                    (FirstState      & StateMask)           != StateExpected                          ||
                    (FirstPermission & PermissionMask)      != PermissionExpected)
                {
                    break;
                }

                //Check if this is the last block on the range, if so return success.
                if ((ulong)EndAddr <= (ulong)(Info.Address + Info.Size - 1))
                {
                    OutState      = FirstState;
                    OutPermission = FirstPermission;
                    OutAttribute  = FirstAttribute & ~AttributeIgnoreMask;

                    return true;
                }

                Node = Node.Next;
            }
            while (Node != null);

            OutState      = MemoryState.Unmapped;
            OutPermission = MemoryPermission.None;
            OutAttribute  = MemoryAttribute.None;

            return false;
        }

        private void InsertBlock(
            long             BaseAddress,
            long             PagesCount,
            MemoryState      OldState,
            MemoryPermission OldPermission,
            MemoryAttribute  OldAttribute,
            MemoryState      NewState,
            MemoryPermission NewPermission,
            MemoryAttribute  NewAttribute)
        {
            //Insert new block on the list only on areas where the state
            //of the block matches the state specified on the Old* state
            //arguments, otherwise leave it as is.
            OldAttribute |= MemoryAttribute.IpcAndDeviceMapped;

            ulong BaseAddr = (ulong)BaseAddress;
            ulong EndAddr  = (ulong)PagesCount * PageSize + BaseAddr;

            LinkedListNode<KMemoryBlock> Node = Blocks.First;

            while (Node != null)
            {
                LinkedListNode<KMemoryBlock> NewNode  = Node;
                LinkedListNode<KMemoryBlock> NextNode = Node.Next;

                KMemoryBlock CurrBlock = Node.Value;

                ulong CurrBaseAddr = (ulong)CurrBlock.BaseAddress;
                ulong CurrEndAddr  = (ulong)CurrBlock.PagesCount * PageSize + CurrBaseAddr;

                if (BaseAddr < CurrEndAddr && CurrBaseAddr < EndAddr)
                {
                    MemoryAttribute CurrBlockAttr = CurrBlock.Attribute | MemoryAttribute.IpcAndDeviceMapped;

                    if (CurrBlock.State      != OldState      ||
                        CurrBlock.Permission != OldPermission ||
                        CurrBlockAttr        != OldAttribute)
                    {
                        Node = NextNode;

                        continue;
                    }

                    if (CurrBaseAddr >= BaseAddr && CurrEndAddr <= EndAddr)
                    {
                        CurrBlock.State      = NewState;
                        CurrBlock.Permission = NewPermission;
                        CurrBlock.Attribute &= ~MemoryAttribute.IpcAndDeviceMapped;
                        CurrBlock.Attribute |= NewAttribute;
                    }
                    else if (CurrBaseAddr >= BaseAddr)
                    {
                        CurrBlock.BaseAddress = (long)EndAddr;

                        CurrBlock.PagesCount = (long)((CurrEndAddr - EndAddr) / PageSize);

                        long NewPagesCount = (long)((EndAddr - CurrBaseAddr) / PageSize);

                        NewNode = Blocks.AddBefore(Node, new KMemoryBlock(
                            (long)CurrBaseAddr,
                            NewPagesCount,
                            NewState,
                            NewPermission,
                            NewAttribute));
                    }
                    else if (CurrEndAddr <= EndAddr)
                    {
                        CurrBlock.PagesCount = (long)((BaseAddr - CurrBaseAddr) / PageSize);

                        long NewPagesCount = (long)((CurrEndAddr - BaseAddr) / PageSize);

                        NewNode = Blocks.AddAfter(Node, new KMemoryBlock(
                            BaseAddress,
                            NewPagesCount,
                            NewState,
                            NewPermission,
                            NewAttribute));
                    }
                    else
                    {
                        CurrBlock.PagesCount = (long)((BaseAddr - CurrBaseAddr) / PageSize);

                        long NextPagesCount = (long)((CurrEndAddr - EndAddr) / PageSize);

                        NewNode = Blocks.AddAfter(Node, new KMemoryBlock(
                            BaseAddress,
                            PagesCount,
                            NewState,
                            NewPermission,
                            NewAttribute));

                        Blocks.AddAfter(NewNode, new KMemoryBlock(
                            (long)EndAddr,
                            NextPagesCount,
                            CurrBlock.State,
                            CurrBlock.Permission,
                            CurrBlock.Attribute));

                        NextNode = null;
                    }

                    MergeEqualStateNeighbours(NewNode);
                }

                Node = NextNode;
            }
        }

        private void InsertBlock(
            long             BaseAddress,
            long             PagesCount,
            MemoryState      State,
            MemoryPermission Permission = MemoryPermission.None,
            MemoryAttribute  Attribute  = MemoryAttribute.None)
        {
            //Inserts new block at the list, replacing and spliting
            //existing blocks as needed.
            KMemoryBlock Block = new KMemoryBlock(BaseAddress, PagesCount, State, Permission, Attribute);

            ulong BaseAddr = (ulong)BaseAddress;
            ulong EndAddr  = (ulong)PagesCount * PageSize + BaseAddr;

            LinkedListNode<KMemoryBlock> NewNode = null;

            LinkedListNode<KMemoryBlock> Node = Blocks.First;

            while (Node != null)
            {
                KMemoryBlock CurrBlock = Node.Value;

                LinkedListNode<KMemoryBlock> NextNode = Node.Next;

                ulong CurrBaseAddr = (ulong)CurrBlock.BaseAddress;
                ulong CurrEndAddr  = (ulong)CurrBlock.PagesCount * PageSize + CurrBaseAddr;

                if (BaseAddr < CurrEndAddr && CurrBaseAddr < EndAddr)
                {
                    if (BaseAddr >= CurrBaseAddr && EndAddr <= CurrEndAddr)
                    {
                        Block.Attribute |= CurrBlock.Attribute & MemoryAttribute.IpcAndDeviceMapped;
                    }

                    if (BaseAddr > CurrBaseAddr && EndAddr < CurrEndAddr)
                    {
                        CurrBlock.PagesCount = (long)((BaseAddr - CurrBaseAddr) / PageSize);

                        long NextPagesCount = (long)((CurrEndAddr - EndAddr) / PageSize);

                        NewNode = Blocks.AddAfter(Node, Block);

                        Blocks.AddAfter(NewNode, new KMemoryBlock(
                            (long)EndAddr,
                            NextPagesCount,
                            CurrBlock.State,
                            CurrBlock.Permission,
                            CurrBlock.Attribute));

                        break;
                    }
                    else if (BaseAddr <= CurrBaseAddr && EndAddr < CurrEndAddr)
                    {
                        CurrBlock.BaseAddress = (long)EndAddr;

                        CurrBlock.PagesCount = (long)((CurrEndAddr - EndAddr) / PageSize);

                        if (NewNode == null)
                        {
                            NewNode = Blocks.AddBefore(Node, Block);
                        }
                    }
                    else if (BaseAddr > CurrBaseAddr && EndAddr >= CurrEndAddr)
                    {
                        CurrBlock.PagesCount = (long)((BaseAddr - CurrBaseAddr) / PageSize);

                        if (NewNode == null)
                        {
                            NewNode = Blocks.AddAfter(Node, Block);
                        }
                    }
                    else
                    {
                        if (NewNode == null)
                        {
                            NewNode = Blocks.AddBefore(Node, Block);
                        }

                        Blocks.Remove(Node);
                    }
                }

                Node = NextNode;
            }

            if (NewNode == null)
            {
                NewNode = Blocks.AddFirst(Block);
            }

            MergeEqualStateNeighbours(NewNode);
        }

        private void MergeEqualStateNeighbours(LinkedListNode<KMemoryBlock> Node)
        {
            KMemoryBlock Block = Node.Value;

            ulong BaseAddr = (ulong)Block.BaseAddress;
            ulong EndAddr  = (ulong)Block.PagesCount * PageSize + BaseAddr;

            if (Node.Previous != null)
            {
                KMemoryBlock Previous = Node.Previous.Value;

                if (BlockStateEquals(Block, Previous))
                {
                    Blocks.Remove(Node.Previous);

                    Block.BaseAddress = Previous.BaseAddress;

                    BaseAddr = (ulong)Block.BaseAddress;
                }
            }

            if (Node.Next != null)
            {
                KMemoryBlock Next = Node.Next.Value;

                if (BlockStateEquals(Block, Next))
                {
                    Blocks.Remove(Node.Next);

                    EndAddr = (ulong)(Next.BaseAddress + Next.PagesCount * PageSize);
                }
            }

            Block.PagesCount = (long)((EndAddr - BaseAddr) / PageSize);
        }

        private static bool BlockStateEquals(KMemoryBlock Lhs, KMemoryBlock Rhs)
        {
            return Lhs.State          == Rhs.State          &&
                   Lhs.Permission     == Rhs.Permission     &&
                   Lhs.Attribute      == Rhs.Attribute      &&
                   Lhs.DeviceRefCount == Rhs.DeviceRefCount &&
                   Lhs.IpcRefCount    == Rhs.IpcRefCount;
        }

        private long FindFirstFit(
            long RegionStart,
            long RegionPagesCount,
            long NeededPagesCount,
            int  Alignment,
            long ReservedStart,
            long ReservedPagesCount)
        {
            long ReservedSize = ReservedPagesCount * PageSize;

            long TotalNeededSize = ReservedSize + NeededPagesCount * PageSize;

            long RegionEndAddr = RegionStart + RegionPagesCount * PageSize;

            LinkedListNode<KMemoryBlock> Node = FindBlockNode(RegionStart);

            KMemoryInfo Info = Node.Value.GetInfo();

            while ((ulong)RegionEndAddr >= (ulong)Info.Address)
            {
                if (Info.State == MemoryState.Unmapped)
                {
                    long CurrBaseAddr = Info.Address + ReservedSize;
                    long CurrEndAddr  = Info.Address + Info.Size - 1;

                    long Address = BitUtils.AlignDown(CurrBaseAddr, Alignment) + ReservedStart;

                    if ((ulong)CurrBaseAddr > (ulong)Address)
                    {
                        Address += Alignment;
                    }

                    long AllocationEndAddr = Address + TotalNeededSize - 1;

                    if ((ulong)AllocationEndAddr <= (ulong)RegionEndAddr &&
                        (ulong)AllocationEndAddr <= (ulong)CurrEndAddr   &&
                        (ulong)Address           <  (ulong)AllocationEndAddr)
                    {
                        return Address;
                    }
                }

                Node = Node.Next;

                if (Node == null)
                {
                    break;
                }

                Info = Node.Value.GetInfo();
            }

            return 0;
        }

        private KMemoryBlock FindBlock(long Address)
        {
            return FindBlockNode(Address)?.Value;
        }

        private LinkedListNode<KMemoryBlock> FindBlockNode(long Address)
        {
            lock (Blocks)
            {
                LinkedListNode<KMemoryBlock> Node = Blocks.First;

                while (Node != null)
                {
                    KMemoryBlock Block = Node.Value;

                    ulong CurrBaseAddr = (ulong)Block.BaseAddress;
                    ulong CurrEndAddr  = (ulong)Block.PagesCount * PageSize + CurrBaseAddr;

                    if (CurrBaseAddr <= (ulong)Address && CurrEndAddr - 1 >= (ulong)Address)
                    {
                        return Node;
                    }

                    Node = Node.Next;
                }
            }

            return null;
        }

        private bool ValidateRegionForState(long Address, long Size, MemoryState State)
        {
            long EndAddr = Address + Size;

            long RegionBaseAddr = GetBaseAddrForState(State);

            long RegionEndAddr = RegionBaseAddr + GetSizeForState(State);

            bool InsideRegion()
            {
                return (ulong)RegionBaseAddr <= (ulong)Address &&
                       (ulong)EndAddr        >  (ulong)Address &&
                       (ulong)EndAddr - 1    <= (ulong)RegionEndAddr - 1;
            }

            bool OutsideHeapRegion()
            {
                return (ulong)EndAddr <= (ulong)HeapRegionStart ||
                       (ulong)Address >= (ulong)HeapRegionEnd;
            }

            bool OutsideMapRegion()
            {
                return (ulong)EndAddr <= (ulong)AliasRegionStart ||
                       (ulong)Address >= (ulong)AliasRegionEnd;
            }

            switch (State)
            {
                case MemoryState.Io:
                case MemoryState.Normal:
                case MemoryState.CodeStatic:
                case MemoryState.CodeMutable:
                case MemoryState.SharedMemory:
                case MemoryState.ModCodeStatic:
                case MemoryState.ModCodeMutable:
                case MemoryState.Stack:
                case MemoryState.ThreadLocal:
                case MemoryState.TransferMemoryIsolated:
                case MemoryState.TransferMemory:
                case MemoryState.ProcessMemory:
                case MemoryState.CodeReadOnly:
                case MemoryState.CodeWritable:
                    return InsideRegion() && OutsideHeapRegion() && OutsideMapRegion();

                case MemoryState.Heap:
                    return InsideRegion() && OutsideMapRegion();

                case MemoryState.IpcBuffer0:
                case MemoryState.IpcBuffer1:
                case MemoryState.IpcBuffer3:
                    return InsideRegion() && OutsideHeapRegion();

                case MemoryState.KernelStack:
                    return InsideRegion();
            }

            throw new ArgumentException($"Invalid state value \"{State}\".");
        }

        private long GetBaseAddrForState(MemoryState State)
        {
            switch (State)
            {
                case MemoryState.Io:
                case MemoryState.Normal:
                case MemoryState.ThreadLocal:
                    return TlsIoRegionStart;

                case MemoryState.CodeStatic:
                case MemoryState.CodeMutable:
                case MemoryState.SharedMemory:
                case MemoryState.ModCodeStatic:
                case MemoryState.ModCodeMutable:
                case MemoryState.TransferMemoryIsolated:
                case MemoryState.TransferMemory:
                case MemoryState.ProcessMemory:
                case MemoryState.CodeReadOnly:
                case MemoryState.CodeWritable:
                    return GetAddrSpaceBaseAddr();

                case MemoryState.Heap:
                    return HeapRegionStart;

                case MemoryState.IpcBuffer0:
                case MemoryState.IpcBuffer1:
                case MemoryState.IpcBuffer3:
                    return AliasRegionStart;

                case MemoryState.Stack:
                    return StackRegionStart;

                case MemoryState.KernelStack:
                    return AddrSpaceStart;
            }

            throw new ArgumentException($"Invalid state value \"{State}\".");
        }

        private long GetSizeForState(MemoryState State)
        {
            switch (State)
            {
                case MemoryState.Io:
                case MemoryState.Normal:
                case MemoryState.ThreadLocal:
                    return TlsIoRegionEnd - TlsIoRegionStart;

                case MemoryState.CodeStatic:
                case MemoryState.CodeMutable:
                case MemoryState.SharedMemory:
                case MemoryState.ModCodeStatic:
                case MemoryState.ModCodeMutable:
                case MemoryState.TransferMemoryIsolated:
                case MemoryState.TransferMemory:
                case MemoryState.ProcessMemory:
                case MemoryState.CodeReadOnly:
                case MemoryState.CodeWritable:
                    return GetAddrSpaceSize();

                case MemoryState.Heap:
                    return HeapRegionEnd - HeapRegionStart;

                case MemoryState.IpcBuffer0:
                case MemoryState.IpcBuffer1:
                case MemoryState.IpcBuffer3:
                    return AliasRegionEnd - AliasRegionStart;

                case MemoryState.Stack:
                    return StackRegionEnd - StackRegionStart;

                case MemoryState.KernelStack:
                    return AddrSpaceEnd - AddrSpaceStart;
            }

            throw new ArgumentException($"Invalid state value \"{State}\".");
        }

        public long GetAddrSpaceBaseAddr()
        {
            if (AddrSpaceWidth == 36 || AddrSpaceWidth == 39)
            {
                return 0x8000000;
            }
            else if (AddrSpaceWidth == 32)
            {
                return 0x200000;
            }
            else
            {
                throw new InvalidOperationException("Invalid address space width!");
            }
        }

        public long GetAddrSpaceSize()
        {
            if (AddrSpaceWidth == 36)
            {
                return 0xff8000000;
            }
            else if (AddrSpaceWidth == 39)
            {
                return 0x7ff8000000;
            }
            else if (AddrSpaceWidth == 32)
            {
                return 0xffe00000;
            }
            else
            {
                throw new InvalidOperationException("Invalid address space width!");
            }
        }

        private KernelResult MapPages(long Address, KPageList PageList, MemoryPermission Permission)
        {
            long CurrAddr = Address;

            KernelResult Result = KernelResult.Success;

            foreach (KPageNode PageNode in PageList)
            {
                Result = DoMmuOperation(
                    CurrAddr,
                    PageNode.PagesCount,
                    PageNode.Address,
                    true,
                    Permission,
                    MemoryOperation.MapPa);

                if (Result != KernelResult.Success)
                {
                    KMemoryInfo Info = FindBlock(CurrAddr).GetInfo();

                    long PagesCount = (long)((ulong)(Address - CurrAddr) / PageSize);

                    Result = MmuUnmap(Address, PagesCount);

                    break;
                }

                CurrAddr += PageNode.PagesCount * PageSize;
            }

            return Result;
        }

        private KernelResult MmuUnmap(long Address, long PagesCount)
        {
            return DoMmuOperation(
                Address,
                PagesCount,
                0,
                false,
                MemoryPermission.None,
                MemoryOperation.Unmap);
        }

        private KernelResult MmuChangePermission(long Address, long PagesCount, MemoryPermission Permission)
        {
            return DoMmuOperation(
                Address,
                PagesCount,
                0,
                false,
                Permission,
                MemoryOperation.ChangePermRw);
        }

        private KernelResult DoMmuOperation(
            long             DstVa,
            long             PagesCount,
            long             SrcPa,
            bool             Map,
            MemoryPermission Permission,
            MemoryOperation  Operation)
        {
            if (Map != (Operation == MemoryOperation.MapPa))
            {
                throw new ArgumentException(nameof(Map) + " value is invalid for this operation.");
            }

            KernelResult Result;

            switch (Operation)
            {
                case MemoryOperation.MapPa:
                {
                    long Size = PagesCount * PageSize;

                    CpuMemory.Map(DstVa, SrcPa - DramMemoryMap.DramBase, Size);

                    Result = KernelResult.Success;

                    break;
                }

                case MemoryOperation.Allocate:
                {
                    KMemoryRegionManager Region = GetMemoryRegionManager();

                    Result = Region.AllocatePages(PagesCount, AslrDisabled, out KPageList PageList);

                    if (Result == KernelResult.Success)
                    {
                        Result = MmuMapPages(DstVa, PageList);
                    }

                    break;
                }

                case MemoryOperation.Unmap:
                {
                    long Size = PagesCount * PageSize;

                    CpuMemory.Unmap(DstVa, Size);

                    Result = KernelResult.Success;

                    break;
                }

                case MemoryOperation.ChangePermRw:             Result = KernelResult.Success; break;
                case MemoryOperation.ChangePermsAndAttributes: Result = KernelResult.Success; break;

                default: throw new ArgumentException($"Invalid operation \"{Operation}\".");
            }

            return Result;
        }

        private KernelResult DoMmuOperation(
            long             Address,
            long             PagesCount,
            KPageList        PageList,
            MemoryPermission Permission,
            MemoryOperation  Operation)
        {
            if (Operation != MemoryOperation.MapVa)
            {
                throw new ArgumentException($"Invalid memory operation \"{Operation}\" specified.");
            }

            return MmuMapPages(Address, PageList);
        }

        private KMemoryRegionManager GetMemoryRegionManager()
        {
            return System.MemoryRegions[(int)MemRegion];
        }

        private KernelResult MmuMapPages(long Address, KPageList PageList)
        {
            foreach (KPageNode PageNode in PageList)
            {
                long Size = PageNode.PagesCount * PageSize;

                CpuMemory.Map(Address, PageNode.Address - DramMemoryMap.DramBase, Size);

                Address += Size;
            }

            return KernelResult.Success;
        }

        public KernelResult ConvertVaToPa(long Va, out long Pa)
        {
            Pa = DramMemoryMap.DramBase + CpuMemory.GetPhysicalAddress(Va);

            return KernelResult.Success;
        }

        public long GetMmUsedPages()
        {
            lock (Blocks)
            {
                return BitUtils.DivRoundUp(GetMmUsedSize(), PageSize);
            }
        }

        private long GetMmUsedSize()
        {
            return Blocks.Count * KMemoryBlockSize;
        }

        public bool InsideAddrSpace(long Address, long Size)
        {
            ulong Start = (ulong)Address;
            ulong End   = (ulong)Size + Start;

            return Start >= (ulong)AddrSpaceStart &&
                   End   <  (ulong)AddrSpaceEnd;
        }

        public bool InsideMapRegion(long Address, long Size)
        {
            ulong Start = (ulong)Address;
            ulong End   = (ulong)Size + Start;

            return Start >= (ulong)AliasRegionStart &&
                   End   <  (ulong)AliasRegionEnd;
        }

        public bool InsideHeapRegion(long Address, long Size)
        {
            ulong Start = (ulong)Address;
            ulong End   = (ulong)Size + Start;

            return Start >= (ulong)HeapRegionStart &&
                   End   <  (ulong)HeapRegionEnd;
        }

        public bool InsideNewMapRegion(long Address, long Size)
        {
            ulong Start = (ulong)Address;
            ulong End   = (ulong)Size + Start;

            return Start >= (ulong)StackRegionStart &&
                   End   <  (ulong)StackRegionEnd;
        }
    }
}