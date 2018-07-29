using ChocolArm64.Memory;
using Ryujinx.HLE.Memory;
using Ryujinx.HLE.OsHle.Kernel;
using System;
using System.Collections.Generic;

using static Ryujinx.HLE.OsHle.ErrorCode;

namespace Ryujinx.HLE.OsHle.Handles
{
    class KMemoryManager
    {
        public const int PageSize = 0x1000;

        private LinkedList<KMemoryBlock> Blocks;

        private AMemory CpuMemory;

        private ArenaAllocator Allocator;

        public long AddrSpaceStart { get; private set; }
        public long AddrSpaceEnd   { get; private set; }

        public long CodeRegionStart { get; private set; }
        public long CodeRegionEnd   { get; private set; }

        public long MapRegionStart { get; private set; }
        public long MapRegionEnd   { get; private set; }

        public long HeapRegionStart { get; private set; }
        public long HeapRegionEnd   { get; private set; }

        public long NewMapRegionStart { get; private set; }
        public long NewMapRegionEnd   { get; private set; }

        public long TlsIoRegionStart { get; private set; }
        public long TlsIoRegionEnd   { get; private set; }

        private long CurrentHeapAddr;

        public KMemoryManager(AMemory CpuMemory, ArenaAllocator Allocator, AddressSpaceType AddrType)
        {
            this.CpuMemory = CpuMemory;
            this.Allocator = Allocator;

            long CodeRegionSize;
            long MapRegionSize;
            long HeapRegionSize;
            long NewMapRegionSize;
            long TlsIoRegionSize;
            int  AddrSpaceWidth;

            switch (AddrType)
            {
                case AddressSpaceType.Addr32Bits:
                    CodeRegionStart  = 0x200000;
                    CodeRegionSize   = 0x3fe00000;
                    AddrSpaceStart   = 0x200000;
                    MapRegionSize    = 0x40000000;
                    HeapRegionSize   = 0x40000000;
                    NewMapRegionSize = 0;
                    TlsIoRegionSize  = 0;
                    AddrSpaceWidth   = 32;
                    break;

                case AddressSpaceType.Addr36Bits:
                    CodeRegionStart  = 0x8000000;
                    CodeRegionSize   = 0x78000000;
                    AddrSpaceStart   = 0x8000000;
                    MapRegionSize    = 0x180000000;
                    HeapRegionSize   = 0x180000000;
                    NewMapRegionSize = 0;
                    TlsIoRegionSize  = 0;
                    AddrSpaceWidth   = 36;
                    break;

                case AddressSpaceType.Addr36BitsNoMap:
                    CodeRegionStart  = 0x200000;
                    CodeRegionSize   = 0x3fe00000;
                    AddrSpaceStart   = 0x200000;
                    MapRegionSize    = 0;
                    HeapRegionSize   = 0x80000000;
                    NewMapRegionSize = 0;
                    TlsIoRegionSize  = 0;
                    AddrSpaceWidth   = 36;
                    break;

                case AddressSpaceType.Addr39Bits:
                    CodeRegionStart  = 0;
                    CodeRegionSize   = 0x80000000;
                    AddrSpaceStart   = 0;
                    MapRegionSize    = 0x1000000000;
                    HeapRegionSize   = 0x180000000;
                    NewMapRegionSize = 0x80000000;
                    TlsIoRegionSize  = 0x1000000000;
                    AddrSpaceWidth   = 39;
                    break;

                default: throw new ArgumentException(nameof(AddrType));
            }

            AddrSpaceEnd = 1L << AddrSpaceWidth;

            CodeRegionEnd     = CodeRegionStart + CodeRegionSize;
            MapRegionStart    = CodeRegionEnd;
            MapRegionEnd      = CodeRegionEnd   + MapRegionSize;
            HeapRegionStart   = MapRegionEnd;
            HeapRegionEnd     = MapRegionEnd    + HeapRegionSize;
            NewMapRegionStart = HeapRegionEnd;
            NewMapRegionEnd   = HeapRegionEnd   + NewMapRegionSize;
            TlsIoRegionStart  = NewMapRegionEnd;
            TlsIoRegionEnd    = NewMapRegionEnd + TlsIoRegionSize;

            CurrentHeapAddr = HeapRegionStart;

            Blocks = new LinkedList<KMemoryBlock>();

            long AddrSpacePagesCount = (AddrSpaceEnd - AddrSpaceStart) / PageSize;

            InsertBlock(AddrSpaceStart, AddrSpacePagesCount, MemoryState.Unmapped);
        }

        public void HleMapProcessCode(long Position, long Size)
        {
            long PagesCount = Size / PageSize;

            if (!Allocator.TryAllocate(Size, out long PA))
            {
                throw new InvalidOperationException();
            }

            lock (Blocks)
            {
                InsertBlock(Position, PagesCount, MemoryState.CodeStatic, MemoryPermission.ReadAndExecute);

                CpuMemory.Map(Position, PA, Size);
            }
        }

        public void HleMapCustom(long Position, long Size, MemoryState State, MemoryPermission Permission)
        {
            long PagesCount = Size / PageSize;

            if (!Allocator.TryAllocate(Size, out long PA))
            {
                throw new InvalidOperationException();
            }

            lock (Blocks)
            {
                InsertBlock(Position, PagesCount, State, Permission);

                CpuMemory.Map(Position, PA, Size);
            }
        }

        public long TrySetHeapSize(long Size, out long Position)
        {
            Position = 0;

            if ((ulong)Size > (ulong)(HeapRegionEnd - HeapRegionStart))
            {
                return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
            }

            bool Success = false;

            long CurrentHeapSize = GetHeapSize();

            if ((ulong)CurrentHeapSize <= (ulong)Size)
            {
                //Expand.
                long DiffSize = Size - CurrentHeapSize;

                lock (Blocks)
                {
                    if (Success = IsUnmapped(CurrentHeapAddr, DiffSize))
                    {
                        if (!Allocator.TryAllocate(DiffSize, out long PA))
                        {
                            return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
                        }

                        long PagesCount = DiffSize / PageSize;

                        InsertBlock(CurrentHeapAddr, PagesCount, MemoryState.Heap, MemoryPermission.ReadAndWrite);

                        CpuMemory.Map(CurrentHeapAddr, PA, DiffSize);
                    }
                }
            }
            else
            {
                //Shrink.
                long FreeAddr = HeapRegionStart + Size;
                long DiffSize = CurrentHeapSize - Size;

                lock (Blocks)
                {
                    Success = CheckRange(
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
                        out _);

                    if (Success)
                    {
                        long PagesCount = DiffSize / PageSize;

                        InsertBlock(FreeAddr, PagesCount, MemoryState.Unmapped);

                        CpuMemory.Unmap(FreeAddr, DiffSize);
                    }
                }
            }

            CurrentHeapAddr = HeapRegionStart + Size;

            if (Success)
            {
                Position = HeapRegionStart;

                return 0;
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long GetHeapSize()
        {
            return CurrentHeapAddr - HeapRegionStart;
        }

        public long SetMemoryAttribute(
            long            Position,
            long            Size,
            MemoryAttribute AttributeMask,
            MemoryAttribute AttributeValue)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Position,
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
                    long PagesCount = Size / PageSize;

                    Attribute &= ~AttributeMask;
                    Attribute |=  AttributeMask & AttributeValue;

                    InsertBlock(Position, PagesCount, State, Permission, Attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public KMemoryInfo QueryMemory(long Position)
        {
            if ((ulong)Position >= (ulong)AddrSpaceStart &&
                (ulong)Position <  (ulong)AddrSpaceEnd)
            {
                lock (Blocks)
                {
                    return FindBlock(Position).GetInfo();
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

        public long Map(long Src, long Dst, long Size)
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
                    long PagesCount = Size / PageSize;

                    InsertBlock(Src, PagesCount, SrcState, MemoryPermission.None, MemoryAttribute.Borrowed);

                    InsertBlock(Dst, PagesCount, MemoryState.MappedMemory, MemoryPermission.ReadAndWrite);

                    long PA = CpuMemory.GetPhysicalAddress(Src);

                    CpuMemory.Map(Dst, PA, Size);
                }
            }

            return Success ? 0 : MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long Unmap(long Src, long Dst, long Size)
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
                    MemoryState.MappedMemory,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out _,
                    out _,
                    out _);

                if (Success)
                {
                    long PagesCount = Size / PageSize;

                    InsertBlock(Src, PagesCount, SrcState, MemoryPermission.ReadAndWrite);

                    InsertBlock(Dst, PagesCount, MemoryState.Unmapped);

                    CpuMemory.Unmap(Dst, Size);
                }
            }

            return Success ? 0 : MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long MapSharedMemory(KSharedMemory SharedMemory, MemoryPermission Permission, long Position)
        {
            lock (Blocks)
            {
                if (IsUnmapped(Position, SharedMemory.Size))
                {
                    long PagesCount = SharedMemory.Size / PageSize;

                    InsertBlock(Position, PagesCount, MemoryState.SharedMemory, Permission);

                    CpuMemory.Map(Position, SharedMemory.PA, SharedMemory.Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long UnmapSharedMemory(long Position, long Size)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Position,
                    Size,
                    MemoryState.Mask,
                    MemoryState.SharedMemory,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState State,
                    out _,
                    out _))
                {
                    long PagesCount = Size / PageSize;

                    InsertBlock(Position, PagesCount, MemoryState.Unmapped);

                    CpuMemory.Unmap(Position, Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ReserveTransferMemory(long Position, long Size, MemoryPermission Permission)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Position,
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
                    long PagesCount = Size / PageSize;

                    Attribute |= MemoryAttribute.Borrowed;

                    InsertBlock(Position, PagesCount, State, Permission, Attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ResetTransferMemory(long Position, long Size)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Position,
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
                    long PagesCount = Size / PageSize;

                    InsertBlock(Position, PagesCount, State, MemoryPermission.ReadAndWrite);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long SetProcessMemoryPermission(long Position, long Size, MemoryPermission Permission)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Position,
                    Size,
                    MemoryState.ProcessPermissionChangeAllowed,
                    MemoryState.ProcessPermissionChangeAllowed,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState State,
                    out _,
                    out _))
                {
                    if (State == MemoryState.CodeStatic)
                    {
                        State = MemoryState.CodeMutable;
                    }
                    else if (State == MemoryState.ModCodeStatic)
                    {
                        State = MemoryState.ModCodeMutable;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    long PagesCount = Size / PageSize;

                    InsertBlock(Position, PagesCount, State, Permission);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long MapPhysicalMemory(long Position, long Size)
        {
            lock (Blocks)
            {
                if (IsUnmapped(Position, Size))
                {
                    if (!Allocator.TryAllocate(Size, out long PA))
                    {
                        return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
                    }

                    long PagesCount = Size / PageSize;

                    InsertBlock(Position, PagesCount, MemoryState.Heap, MemoryPermission.ReadAndWrite);

                    CpuMemory.Map(Position, PA, Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long UnmapPhysicalMemory(long Position, long Size)
        {
            lock (Blocks)
            {
                if (CheckRange(
                    Position,
                    Size,
                    MemoryState.Mask,
                    MemoryState.Heap,
                    MemoryPermission.Mask,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState State,
                    out _,
                    out _))
                {
                    long PagesCount = Size / PageSize;

                    InsertBlock(Position, PagesCount, MemoryState.Unmapped);

                    CpuMemory.Unmap(Position, Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        private bool IsUnmapped(long Position, long Size)
        {
            return CheckRange(
                Position,
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
            long                 Position,
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
            KMemoryInfo BlkInfo = FindBlock(Position).GetInfo();

            ulong Start = (ulong)Position;
            ulong End   = (ulong)Size + Start;

            if (End <= (ulong)(BlkInfo.Position + BlkInfo.Size))
            {
                if ((BlkInfo.Attribute  & AttributeMask)  == AttributeExpected &&
                    (BlkInfo.State      & StateMask)      == StateExpected     &&
                    (BlkInfo.Permission & PermissionMask) == PermissionExpected)
                {
                    OutState      = BlkInfo.State;
                    OutPermission = BlkInfo.Permission;
                    OutAttribute  = BlkInfo.Attribute & ~AttributeIgnoreMask;

                    return true;
                }
            }

            OutState      = MemoryState.Unmapped;
            OutPermission = MemoryPermission.None;
            OutAttribute  = MemoryAttribute.None;

            return false;
        }

        private void InsertBlock(
            long             BasePosition,
            long             PagesCount,
            MemoryState      State,
            MemoryPermission Permission = MemoryPermission.None,
            MemoryAttribute  Attribute  = MemoryAttribute.None)
        {
            KMemoryBlock Block = new KMemoryBlock(
                BasePosition,
                PagesCount,
                State,
                Permission,
                Attribute);

            ulong Start = (ulong)BasePosition;
            ulong End   = (ulong)PagesCount * PageSize + Start;

            LinkedListNode<KMemoryBlock> NewNode = null;

            LinkedListNode<KMemoryBlock> Node = Blocks.First;

            while (Node != null)
            {
                KMemoryBlock CurrBlock = Node.Value;

                LinkedListNode<KMemoryBlock> NextNode = Node.Next;

                ulong CurrStart = (ulong)CurrBlock.BasePosition;
                ulong CurrEnd   = (ulong)CurrBlock.PagesCount * PageSize + CurrStart;

                if (Start < CurrEnd && CurrStart < End)
                {
                    if (Start >= CurrStart && End <= CurrEnd)
                    {
                        Block.Attribute |= CurrBlock.Attribute & MemoryAttribute.IpcAndDeviceMapped;
                    }

                    if (Start > CurrStart && End < CurrEnd)
                    {
                        CurrBlock.PagesCount = (long)((Start - CurrStart) / PageSize);

                        NewNode = Blocks.AddAfter(Node, Block);

                        KMemoryBlock NewBlock = new KMemoryBlock(
                            (long)End,
                            (long)((CurrEnd - End) / PageSize),
                            CurrBlock.State,
                            CurrBlock.Permission,
                            CurrBlock.Attribute);

                        Blocks.AddAfter(NewNode, NewBlock);

                        break;
                    }
                    else if (Start <= CurrStart && End < CurrEnd)
                    {
                        CurrBlock.BasePosition = (long)End;

                        CurrBlock.PagesCount = (long)((CurrEnd - End) / PageSize);

                        if (NewNode == null)
                        {
                            NewNode = Blocks.AddBefore(Node, Block);
                        }
                    }
                    else if (Start > CurrStart && End >= CurrEnd)
                    {
                        CurrBlock.PagesCount = (long)((Start - CurrStart) / PageSize);

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

            if (NewNode.Previous != null)
            {
                KMemoryBlock Previous = NewNode.Previous.Value;

                if (BlockStateEquals(Block, Previous))
                {
                    Blocks.Remove(NewNode.Previous);

                    Block.BasePosition = Previous.BasePosition;

                    Start = (ulong)Block.BasePosition;
                }
            }

            if (NewNode.Next != null)
            {
                KMemoryBlock Next = NewNode.Next.Value;

                if (BlockStateEquals(Block, Next))
                {
                    Blocks.Remove(NewNode.Next);

                    End = (ulong)(Next.BasePosition + Next.PagesCount * PageSize);
                }
            }

            Block.PagesCount = (long)((End - Start) / PageSize);
        }

        private static bool BlockStateEquals(KMemoryBlock LHS, KMemoryBlock RHS)
        {
            return LHS.State          == RHS.State          &&
                   LHS.Permission     == RHS.Permission     &&
                   LHS.Attribute      == RHS.Attribute      &&
                   LHS.DeviceRefCount == RHS.DeviceRefCount &&
                   LHS.IpcRefCount    == RHS.IpcRefCount;
        }

        private KMemoryBlock FindBlock(long Position)
        {
            ulong Addr = (ulong)Position;

            lock (Blocks)
            {
                LinkedListNode<KMemoryBlock> Node = Blocks.First;

                while (Node != null)
                {
                    KMemoryBlock Block = Node.Value;

                    ulong Start = (ulong)Block.BasePosition;
                    ulong End   = (ulong)Block.PagesCount * PageSize + Start;

                    if (Start <= Addr && End - 1 >= Addr)
                    {
                        return Block;
                    }

                    Node = Node.Next;
                }
            }

            return null;
        }
    }
}