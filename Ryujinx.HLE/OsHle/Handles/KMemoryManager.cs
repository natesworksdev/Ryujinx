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

        private AMemory CpuMemoryManager;

        private ArenaAllocator Allocator;

        public long AddrSpaceStart { get; private set; }
        public long AddrSpaceEnd   { get; private set; }

        public long HeapRegionStart { get; private set; }
        public long HeapRegionEnd   { get; private set; }

        public long TlsIoRegionStart { get; private set; }
        public long TlsIoRegionEnd   { get; private set; }

        private long CurrentHeapAddr;

        public KMemoryManager(AMemory CpuMemoryManager, ArenaAllocator Allocator)
        {
            this.CpuMemoryManager = CpuMemoryManager;
            this.Allocator        = Allocator;

            AddrSpaceStart = 0;
            AddrSpaceEnd   = MemoryRegions.AddrSpaceStart + MemoryRegions.AddrSpaceSize;

            HeapRegionStart = MemoryRegions.HeapRegionAddress;
            HeapRegionEnd   = MemoryRegions.HeapRegionAddress + MemoryRegions.HeapRegionSize;

            CurrentHeapAddr = HeapRegionStart;

            Blocks = new LinkedList<KMemoryBlock>();

            long AddrSpacePagesCount = (AddrSpaceEnd - AddrSpaceStart) / PageSize;

            InsertBlockUnsafe(
                AddrSpaceStart,
                AddrSpacePagesCount,
                MemoryState.Unmapped,
                MemoryPermission.None,
                MemoryAttribute.None);
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
                InsertBlockUnsafe(
                    Position,
                    PagesCount,
                    MemoryState.CodeStatic,
                    MemoryPermission.ReadAndExecute,
                    MemoryAttribute.None);

                CpuMemoryManager.Map(Position, PA, Size);
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
                InsertBlockUnsafe(Position, PagesCount, State, Permission, MemoryAttribute.None);

                CpuMemoryManager.Map(Position, PA, Size);
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

                if (!Allocator.TryAllocate(DiffSize, out long PA))
                {
                    return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
                }

                lock (Blocks)
                {
                    if (Success = CheckUnmappedUnsafe(CurrentHeapAddr, DiffSize))
                    {
                        long PagesCount = DiffSize / PageSize;

                        InsertBlockUnsafe(
                            CurrentHeapAddr,
                            PagesCount,
                            MemoryState.Heap,
                            MemoryPermission.ReadAndWrite,
                            MemoryAttribute.None);

                        CpuMemoryManager.Map(CurrentHeapAddr, PA, DiffSize);
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
                    Success = CheckRangeUnsafe(
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

                        InsertBlockUnsafe(
                            FreeAddr,
                            PagesCount,
                            MemoryState.Unmapped,
                            MemoryPermission.None,
                            MemoryAttribute.None);

                        CpuMemoryManager.Unmap(FreeAddr, DiffSize);
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
                if (CheckRangeUnsafe(
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

                    InsertBlockUnsafe(Position, PagesCount, State, Permission, Attribute);

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
                    return FindBlockUnsafe(Position).GetInfo();
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
            long PagesCount = Size / PageSize;

            bool Success;

            lock (Blocks)
            {
                Success = CheckRangeUnsafe(
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

                Success &= CheckUnmappedUnsafe(Dst, Size);

                if (Success)
                {
                    InsertBlockUnsafe(
                        Src,
                        PagesCount,
                        SrcState,
                        MemoryPermission.None,
                        MemoryAttribute.Borrowed);

                    InsertBlockUnsafe(
                        Dst,
                        PagesCount,
                        MemoryState.MappedMemory,
                        MemoryPermission.ReadAndWrite,
                        MemoryAttribute.None);

                    long PA = CpuMemoryManager.GetPhysicalAddress(Src);

                    CpuMemoryManager.Map(Dst, PA, Size);
                }
            }

            return Success ? 0 : MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long Unmap(long Src, long Dst, long Size)
        {
            long PagesCount = Size / PageSize;

            bool Success;

            lock (Blocks)
            {
                Success = CheckRangeUnsafe(
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

                Success &= CheckRangeUnsafe(
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
                    InsertBlockUnsafe(
                        Src,
                        PagesCount,
                        SrcState,
                        MemoryPermission.ReadAndWrite,
                        MemoryAttribute.None);

                    InsertBlockUnsafe(
                        Dst,
                        PagesCount,
                        MemoryState.Unmapped,
                        MemoryPermission.None,
                        MemoryAttribute.None);

                    CpuMemoryManager.Unmap(Dst, Size);
                }
            }

            return Success ? 0 : MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long MapSharedMemory(KSharedMemory SharedMemory, MemoryPermission Permission, long Position)
        {
            lock (Blocks)
            {
                if (CheckUnmappedUnsafe(Position, SharedMemory.Size))
                {
                    long PagesCount = SharedMemory.Size / PageSize;

                    InsertBlockUnsafe(
                        Position,
                        PagesCount,
                        MemoryState.SharedMemory,
                        Permission,
                        MemoryAttribute.None);

                    CpuMemoryManager.Map(Position, SharedMemory.PA, SharedMemory.Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long UnmapSharedMemory(long Position, long Size)
        {
            lock (Blocks)
            {
                if (CheckRangeUnsafe(
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

                    InsertBlockUnsafe(
                        Position,
                        PagesCount,
                        MemoryState.Unmapped,
                        MemoryPermission.None,
                        MemoryAttribute.None);

                    CpuMemoryManager.Unmap(Position, Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ReserveTransferMemory(long Position, long Size, MemoryPermission Permission)
        {
            lock (Blocks)
            {
                if (CheckRangeUnsafe(
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

                    InsertBlockUnsafe(
                        Position,
                        PagesCount,
                        State,
                        Permission,
                        Attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ResetTransferMemory(long Position, long Size)
        {
            lock (Blocks)
            {
                if (CheckRangeUnsafe(
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

                    InsertBlockUnsafe(
                        Position,
                        PagesCount,
                        State,
                        MemoryPermission.ReadAndWrite,
                        MemoryAttribute.None);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long SetProcessMemoryPermission(long Position, long Size, MemoryPermission Permission)
        {
            lock (Blocks)
            {
                if (CheckRangeUnsafe(
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

                    InsertBlockUnsafe(Position, PagesCount, State, Permission, MemoryAttribute.None);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        private bool CheckUnmappedUnsafe(long Dst, long Size)
        {
            return CheckRangeUnsafe(
                Dst,
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

        private bool CheckRangeUnsafe(
            long                 Dst,
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
            KMemoryInfo BlkInfo = FindBlockUnsafe(Dst).GetInfo();

            ulong Start = (ulong)Dst;
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

        private void InsertBlockUnsafe(
            long             BasePosition,
            long             PagesCount,
            MemoryState      State,
            MemoryPermission Permission,
            MemoryAttribute  Attribute)
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

                        Node = Blocks.AddAfter(Node, Block);

                        KMemoryBlock NewBlock = new KMemoryBlock(
                            (long)End,
                            (long)((CurrEnd - End) / PageSize),
                            CurrBlock.State,
                            CurrBlock.Permission,
                            CurrBlock.Attribute);

                        Blocks.AddAfter(Node, NewBlock);

                        return;
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

        private KMemoryBlock FindBlockUnsafe(long Position)
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