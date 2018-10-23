using ChocolArm64.Memory;
using Ryujinx.HLE.Memory;
using System;
using System.Collections.Generic;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryManager
    {
        public const int PageSize = 0x1000;

        private LinkedList<KMemoryBlock> _blocks;

        private AMemory _cpuMemory;

        private ArenaAllocator _allocator;

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

        public long PersonalMmHeapUsage { get; private set; }

        private long _currentHeapAddr;

        public KMemoryManager(Process process)
        {
            _cpuMemory = process.Memory;
            _allocator = process.Device.Memory.Allocator;

            long codeRegionSize;
            long mapRegionSize;
            long heapRegionSize;
            long newMapRegionSize;
            long tlsIoRegionSize;
            int  addrSpaceWidth;

            AddressSpaceType addrType = AddressSpaceType.Addr39Bits;

            if (process.MetaData != null)
            {
                addrType = (AddressSpaceType)process.MetaData.AddressSpaceWidth;
            }

            switch (addrType)
            {
                case AddressSpaceType.Addr32Bits:
                    CodeRegionStart  = 0x200000;
                    codeRegionSize   = 0x3fe00000;
                    mapRegionSize    = 0x40000000;
                    heapRegionSize   = 0x40000000;
                    newMapRegionSize = 0;
                    tlsIoRegionSize  = 0;
                    addrSpaceWidth   = 32;
                    break;

                case AddressSpaceType.Addr36Bits:
                    CodeRegionStart  = 0x8000000;
                    codeRegionSize   = 0x78000000;
                    mapRegionSize    = 0x180000000;
                    heapRegionSize   = 0x180000000;
                    newMapRegionSize = 0;
                    tlsIoRegionSize  = 0;
                    addrSpaceWidth   = 36;
                    break;

                case AddressSpaceType.Addr36BitsNoMap:
                    CodeRegionStart  = 0x200000;
                    codeRegionSize   = 0x3fe00000;
                    mapRegionSize    = 0;
                    heapRegionSize   = 0x80000000;
                    newMapRegionSize = 0;
                    tlsIoRegionSize  = 0;
                    addrSpaceWidth   = 36;
                    break;

                case AddressSpaceType.Addr39Bits:
                    CodeRegionStart  = 0x8000000;
                    codeRegionSize   = 0x80000000;
                    mapRegionSize    = 0x1000000000;
                    heapRegionSize   = 0x180000000;
                    newMapRegionSize = 0x80000000;
                    tlsIoRegionSize  = 0x1000000000;
                    addrSpaceWidth   = 39;
                    break;

                default: throw new InvalidOperationException();
            }

            AddrSpaceStart = 0;
            AddrSpaceEnd   = 1L << addrSpaceWidth;

            CodeRegionEnd     = CodeRegionStart + codeRegionSize;
            MapRegionStart    = CodeRegionEnd;
            MapRegionEnd      = CodeRegionEnd   + mapRegionSize;
            HeapRegionStart   = MapRegionEnd;
            HeapRegionEnd     = MapRegionEnd    + heapRegionSize;
            NewMapRegionStart = HeapRegionEnd;
            NewMapRegionEnd   = HeapRegionEnd   + newMapRegionSize;
            TlsIoRegionStart  = NewMapRegionEnd;
            TlsIoRegionEnd    = NewMapRegionEnd + tlsIoRegionSize;

            _currentHeapAddr = HeapRegionStart;

            if (newMapRegionSize == 0)
            {
                NewMapRegionStart = AddrSpaceStart;
                NewMapRegionEnd   = AddrSpaceEnd;
            }

            _blocks = new LinkedList<KMemoryBlock>();

            long addrSpacePagesCount = (AddrSpaceEnd - AddrSpaceStart) / PageSize;

            InsertBlock(AddrSpaceStart, addrSpacePagesCount, MemoryState.Unmapped);
        }

        public void HleMapProcessCode(long position, long size)
        {
            long pagesCount = size / PageSize;

            if (!_allocator.TryAllocate(size, out long pa))
            {
                throw new InvalidOperationException();
            }

            lock (_blocks)
            {
                InsertBlock(position, pagesCount, MemoryState.CodeStatic, MemoryPermission.ReadAndExecute);

                _cpuMemory.Map(position, pa, size);
            }
        }

        public long MapProcessCodeMemory(long dst, long src, long size)
        {
            lock (_blocks)
            {
                long pagesCount = size / PageSize;

                bool success = IsUnmapped(dst, size);

                success &= CheckRange(
                            src,
                            size,
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

                if (success)
                {
                    long pa = _cpuMemory.GetPhysicalAddress(src);

                    InsertBlock(dst, pagesCount, MemoryState.CodeStatic, MemoryPermission.ReadAndExecute);
                    InsertBlock(src, pagesCount, MemoryState.Heap, MemoryPermission.None);

                    _cpuMemory.Map(dst, pa, size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long UnmapProcessCodeMemory(long dst, long src, long size)
        {
            lock (_blocks)
            {
                long pagesCount = size / PageSize;

                bool success = CheckRange(
                            dst,
                            size,
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

                success &= CheckRange(
                            src,
                            size,
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

                if (success)
                {
                    InsertBlock(dst, pagesCount, MemoryState.Unmapped);
                    InsertBlock(src, pagesCount, MemoryState.Heap, MemoryPermission.ReadAndWrite);

                    _cpuMemory.Unmap(dst, size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public void HleMapCustom(long position, long size, MemoryState state, MemoryPermission permission)
        {
            long pagesCount = size / PageSize;

            if (!_allocator.TryAllocate(size, out long pa))
            {
                throw new InvalidOperationException();
            }

            lock (_blocks)
            {
                InsertBlock(position, pagesCount, state, permission);

                _cpuMemory.Map(position, pa, size);
            }
        }

        public long HleMapTlsPage()
        {
            bool hasTlsIoRegion = TlsIoRegionStart != TlsIoRegionEnd;

            long position = hasTlsIoRegion ? TlsIoRegionStart : CodeRegionStart;

            lock (_blocks)
            {
                while (position < (hasTlsIoRegion ? TlsIoRegionEnd : CodeRegionEnd))
                {
                    if (FindBlock(position).State == MemoryState.Unmapped)
                    {
                        InsertBlock(position, 1, MemoryState.ThreadLocal, MemoryPermission.ReadAndWrite);

                        if (!_allocator.TryAllocate(PageSize, out long pa))
                        {
                            throw new InvalidOperationException();
                        }

                        _cpuMemory.Map(position, pa, PageSize);

                        return position;
                    }

                    position += PageSize;
                }

                throw new InvalidOperationException();
            }
        }

        public long TrySetHeapSize(long size, out long position)
        {
            position = 0;

            if ((ulong)size > (ulong)(HeapRegionEnd - HeapRegionStart))
            {
                return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
            }

            bool success = false;

            long currentHeapSize = GetHeapSize();

            if ((ulong)currentHeapSize <= (ulong)size)
            {
                //Expand.
                long diffSize = size - currentHeapSize;

                lock (_blocks)
                {
                    if (success = IsUnmapped(_currentHeapAddr, diffSize))
                    {
                        if (!_allocator.TryAllocate(diffSize, out long pa))
                        {
                            return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
                        }

                        long pagesCount = diffSize / PageSize;

                        InsertBlock(_currentHeapAddr, pagesCount, MemoryState.Heap, MemoryPermission.ReadAndWrite);

                        _cpuMemory.Map(_currentHeapAddr, pa, diffSize);
                    }
                }
            }
            else
            {
                //Shrink.
                long freeAddr = HeapRegionStart + size;
                long diffSize = currentHeapSize - size;

                lock (_blocks)
                {
                    success = CheckRange(
                        freeAddr,
                        diffSize,
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

                    if (success)
                    {
                        long pagesCount = diffSize / PageSize;

                        InsertBlock(freeAddr, pagesCount, MemoryState.Unmapped);

                        FreePages(freeAddr, pagesCount);

                        _cpuMemory.Unmap(freeAddr, diffSize);
                    }
                }
            }

            _currentHeapAddr = HeapRegionStart + size;

            if (success)
            {
                position = HeapRegionStart;

                return 0;
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long GetHeapSize()
        {
            return _currentHeapAddr - HeapRegionStart;
        }

        public long SetMemoryAttribute(
            long            position,
            long            size,
            MemoryAttribute attributeMask,
            MemoryAttribute attributeValue)
        {
            lock (_blocks)
            {
                if (CheckRange(
                    position,
                    size,
                    MemoryState.AttributeChangeAllowed,
                    MemoryState.AttributeChangeAllowed,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.BorrowedAndIpcMapped,
                    MemoryAttribute.None,
                    MemoryAttribute.DeviceMappedAndUncached,
                    out MemoryState      state,
                    out MemoryPermission permission,
                    out MemoryAttribute  attribute))
                {
                    long pagesCount = size / PageSize;

                    attribute &= ~attributeMask;
                    attribute |=  attributeMask & attributeValue;

                    InsertBlock(position, pagesCount, state, permission, attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public KMemoryInfo QueryMemory(long position)
        {
            if ((ulong)position >= (ulong)AddrSpaceStart &&
                (ulong)position <  (ulong)AddrSpaceEnd)
            {
                lock (_blocks)
                {
                    return FindBlock(position).GetInfo();
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

        public long Map(long src, long dst, long size)
        {
            bool success;

            lock (_blocks)
            {
                success = CheckRange(
                    src,
                    size,
                    MemoryState.MapAllowed,
                    MemoryState.MapAllowed,
                    MemoryPermission.Mask,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState srcState,
                    out _,
                    out _);

                success &= IsUnmapped(dst, size);

                if (success)
                {
                    long pagesCount = size / PageSize;

                    InsertBlock(src, pagesCount, srcState, MemoryPermission.None, MemoryAttribute.Borrowed);

                    InsertBlock(dst, pagesCount, MemoryState.MappedMemory, MemoryPermission.ReadAndWrite);

                    long pa = _cpuMemory.GetPhysicalAddress(src);

                    _cpuMemory.Map(dst, pa, size);
                }
            }

            return success ? 0 : MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long Unmap(long src, long dst, long size)
        {
            bool success;

            lock (_blocks)
            {
                success = CheckRange(
                    src,
                    size,
                    MemoryState.MapAllowed,
                    MemoryState.MapAllowed,
                    MemoryPermission.Mask,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.Borrowed,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState srcState,
                    out _,
                    out _);

                success &= CheckRange(
                    dst,
                    size,
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

                if (success)
                {
                    long pagesCount = size / PageSize;

                    InsertBlock(src, pagesCount, srcState, MemoryPermission.ReadAndWrite);

                    InsertBlock(dst, pagesCount, MemoryState.Unmapped);

                    _cpuMemory.Unmap(dst, size);
                }
            }

            return success ? 0 : MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long MapSharedMemory(KSharedMemory sharedMemory, MemoryPermission permission, long position)
        {
            lock (_blocks)
            {
                if (IsUnmapped(position, sharedMemory.Size))
                {
                    long pagesCount = sharedMemory.Size / PageSize;

                    InsertBlock(position, pagesCount, MemoryState.SharedMemory, permission);

                    _cpuMemory.Map(position, sharedMemory.Pa, sharedMemory.Size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long UnmapSharedMemory(long position, long size)
        {
            lock (_blocks)
            {
                if (CheckRange(
                    position,
                    size,
                    MemoryState.Mask,
                    MemoryState.SharedMemory,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState state,
                    out _,
                    out _))
                {
                    long pagesCount = size / PageSize;

                    InsertBlock(position, pagesCount, MemoryState.Unmapped);

                    _cpuMemory.Unmap(position, size);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ReserveTransferMemory(long position, long size, MemoryPermission permission)
        {
            lock (_blocks)
            {
                if (CheckRange(
                    position,
                    size,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryPermission.Mask,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState state,
                    out _,
                    out MemoryAttribute attribute))
                {
                    long pagesCount = size / PageSize;

                    attribute |= MemoryAttribute.Borrowed;

                    InsertBlock(position, pagesCount, state, permission, attribute);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long ResetTransferMemory(long position, long size)
        {
            lock (_blocks)
            {
                if (CheckRange(
                    position,
                    size,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryState.TransferMemoryAllowed | MemoryState.IsPoolAllocated,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.Borrowed,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState state,
                    out _,
                    out _))
                {
                    long pagesCount = size / PageSize;

                    InsertBlock(position, pagesCount, state, MemoryPermission.ReadAndWrite);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long SetProcessMemoryPermission(long position, long size, MemoryPermission permission)
        {
            lock (_blocks)
            {
                if (CheckRange(
                    position,
                    size,
                    MemoryState.ProcessPermissionChangeAllowed,
                    MemoryState.ProcessPermissionChangeAllowed,
                    MemoryPermission.None,
                    MemoryPermission.None,
                    MemoryAttribute.Mask,
                    MemoryAttribute.None,
                    MemoryAttribute.IpcAndDeviceMapped,
                    out MemoryState state,
                    out _,
                    out _))
                {
                    if (state == MemoryState.CodeStatic)
                    {
                        state = MemoryState.CodeMutable;
                    }
                    else if (state == MemoryState.ModCodeStatic)
                    {
                        state = MemoryState.ModCodeMutable;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    long pagesCount = size / PageSize;

                    InsertBlock(position, pagesCount, state, permission);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
        }

        public long MapPhysicalMemory(long position, long size)
        {
            long end = position + size;

            lock (_blocks)
            {
                long mappedSize = 0;

                KMemoryInfo info;

                LinkedListNode<KMemoryBlock> baseNode = FindBlockNode(position);

                LinkedListNode<KMemoryBlock> node = baseNode;

                do
                {
                    info = node.Value.GetInfo();

                    if (info.State != MemoryState.Unmapped)
                    {
                        mappedSize += GetSizeInRange(info, position, end);
                    }

                    node = node.Next;
                }
                while ((ulong)(info.Position + info.Size) < (ulong)end && node != null);

                if (mappedSize == size)
                {
                    return 0;
                }

                long remainingSize = size - mappedSize;

                if (!_allocator.TryAllocate(remainingSize, out long pa))
                {
                    return MakeError(ErrorModule.Kernel, KernelErr.OutOfMemory);
                }

                node = baseNode;

                do
                {
                    info = node.Value.GetInfo();

                    if (info.State == MemoryState.Unmapped)
                    {
                        long currSize = GetSizeInRange(info, position, end);

                        long mapPosition = info.Position;

                        if ((ulong)mapPosition < (ulong)position)
                        {
                            mapPosition = position;
                        }

                        _cpuMemory.Map(mapPosition, pa, currSize);

                        pa += currSize;
                    }

                    node = node.Next;
                }
                while ((ulong)(info.Position + info.Size) < (ulong)end && node != null);

                PersonalMmHeapUsage += remainingSize;

                long pagesCount = size / PageSize;

                InsertBlock(
                    position,
                    pagesCount,
                    MemoryState.Unmapped,
                    MemoryPermission.None,
                    MemoryAttribute.None,
                    MemoryState.Heap,
                    MemoryPermission.ReadAndWrite,
                    MemoryAttribute.None);
            }

            return 0;
        }

        public long UnmapPhysicalMemory(long position, long size)
        {
            long end = position + size;

            lock (_blocks)
            {
                long heapMappedSize = 0;

                long currPosition = position;

                KMemoryInfo info;

                LinkedListNode<KMemoryBlock> node = FindBlockNode(currPosition);

                do
                {
                    info = node.Value.GetInfo();

                    if (info.State == MemoryState.Heap)
                    {
                        if (info.Attribute != MemoryAttribute.None)
                        {
                            return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
                        }

                        heapMappedSize += GetSizeInRange(info, position, end);
                    }
                    else if (info.State != MemoryState.Unmapped)
                    {
                        return MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);
                    }

                    node = node.Next;
                }
                while ((ulong)(info.Position + info.Size) < (ulong)end && node != null);

                if (heapMappedSize == 0)
                {
                    return 0;
                }

                PersonalMmHeapUsage -= heapMappedSize;

                long pagesCount = size / PageSize;

                InsertBlock(position, pagesCount, MemoryState.Unmapped);

                FreePages(position, pagesCount);

                _cpuMemory.Unmap(position, size);

                return 0;
            }
        }

        private long GetSizeInRange(KMemoryInfo info, long start, long end)
        {
            long currEnd  = info.Size + info.Position;
            long currSize = info.Size;

            if ((ulong)info.Position < (ulong)start)
            {
                currSize -= start - info.Position;
            }

            if ((ulong)currEnd > (ulong)end)
            {
                currSize -= currEnd - end;
            }

            return currSize;
        }

        private void FreePages(long position, long pagesCount)
        {
            for (long page = 0; page < pagesCount; page++)
            {
                long va = position + page * PageSize;

                if (!_cpuMemory.IsMapped(va))
                {
                    continue;
                }

                long pa = _cpuMemory.GetPhysicalAddress(va);

                _allocator.Free(pa, PageSize);
            }
        }

        public bool HleIsUnmapped(long position, long size)
        {
            bool result = false;

            lock (_blocks)
            {
                result = IsUnmapped(position, size);
            }

            return result;
        }

        private bool IsUnmapped(long position, long size)
        {
            return CheckRange(
                position,
                size,
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
            long                 position,
            long                 size,
            MemoryState          stateMask,
            MemoryState          stateExpected,
            MemoryPermission     permissionMask,
            MemoryPermission     permissionExpected,
            MemoryAttribute      attributeMask,
            MemoryAttribute      attributeExpected,
            MemoryAttribute      attributeIgnoreMask,
            out MemoryState      outState,
            out MemoryPermission outPermission,
            out MemoryAttribute  outAttribute)
        {
            KMemoryInfo blkInfo = FindBlock(position).GetInfo();

            ulong start = (ulong)position;
            ulong end   = (ulong)size + start;

            if (end <= (ulong)(blkInfo.Position + blkInfo.Size))
            {
                if ((blkInfo.Attribute  & attributeMask)  == attributeExpected &&
                    (blkInfo.State      & stateMask)      == stateExpected     &&
                    (blkInfo.Permission & permissionMask) == permissionExpected)
                {
                    outState      = blkInfo.State;
                    outPermission = blkInfo.Permission;
                    outAttribute  = blkInfo.Attribute & ~attributeIgnoreMask;

                    return true;
                }
            }

            outState      = MemoryState.Unmapped;
            outPermission = MemoryPermission.None;
            outAttribute  = MemoryAttribute.None;

            return false;
        }

        private void InsertBlock(
            long             basePosition,
            long             pagesCount,
            MemoryState      oldState,
            MemoryPermission oldPermission,
            MemoryAttribute  oldAttribute,
            MemoryState      newState,
            MemoryPermission newPermission,
            MemoryAttribute  newAttribute)
        {
            //Insert new block on the list only on areas where the state
            //of the block matches the state specified on the Old* state
            //arguments, otherwise leave it as is.
            oldAttribute |= MemoryAttribute.IpcAndDeviceMapped;

            ulong start = (ulong)basePosition;
            ulong end   = (ulong)pagesCount * PageSize + start;

            LinkedListNode<KMemoryBlock> node = _blocks.First;

            while (node != null)
            {
                LinkedListNode<KMemoryBlock> newNode  = node;
                LinkedListNode<KMemoryBlock> nextNode = node.Next;

                KMemoryBlock currBlock = node.Value;

                ulong currStart = (ulong)currBlock.BasePosition;
                ulong currEnd   = (ulong)currBlock.PagesCount * PageSize + currStart;

                if (start < currEnd && currStart < end)
                {
                    MemoryAttribute currBlockAttr = currBlock.Attribute | MemoryAttribute.IpcAndDeviceMapped;

                    if (currBlock.State      != oldState      ||
                        currBlock.Permission != oldPermission ||
                        currBlockAttr        != oldAttribute)
                    {
                        node = nextNode;

                        continue;
                    }

                    if (currStart >= start && currEnd <= end)
                    {
                        currBlock.State      = newState;
                        currBlock.Permission = newPermission;
                        currBlock.Attribute &= ~MemoryAttribute.IpcAndDeviceMapped;
                        currBlock.Attribute |= newAttribute;
                    }
                    else if (currStart >= start)
                    {
                        currBlock.BasePosition = (long)end;

                        currBlock.PagesCount = (long)((currEnd - end) / PageSize);

                        long newPagesCount = (long)((end - currStart) / PageSize);

                        newNode = _blocks.AddBefore(node, new KMemoryBlock(
                            (long)currStart,
                            newPagesCount,
                            newState,
                            newPermission,
                            newAttribute));
                    }
                    else if (currEnd <= end)
                    {
                        currBlock.PagesCount = (long)((start - currStart) / PageSize);

                        long newPagesCount = (long)((currEnd - start) / PageSize);

                        newNode = _blocks.AddAfter(node, new KMemoryBlock(
                            basePosition,
                            newPagesCount,
                            newState,
                            newPermission,
                            newAttribute));
                    }
                    else
                    {
                        currBlock.PagesCount = (long)((start - currStart) / PageSize);

                        long nextPagesCount = (long)((currEnd - end) / PageSize);

                        newNode = _blocks.AddAfter(node, new KMemoryBlock(
                            basePosition,
                            pagesCount,
                            newState,
                            newPermission,
                            newAttribute));

                        _blocks.AddAfter(newNode, new KMemoryBlock(
                            (long)end,
                            nextPagesCount,
                            currBlock.State,
                            currBlock.Permission,
                            currBlock.Attribute));

                        nextNode = null;
                    }

                    MergeEqualStateNeighbours(newNode);
                }

                node = nextNode;
            }
        }

        private void InsertBlock(
            long             basePosition,
            long             pagesCount,
            MemoryState      state,
            MemoryPermission permission = MemoryPermission.None,
            MemoryAttribute  attribute  = MemoryAttribute.None)
        {
            //Inserts new block at the list, replacing and spliting
            //existing blocks as needed.
            KMemoryBlock block = new KMemoryBlock(basePosition, pagesCount, state, permission, attribute);

            ulong start = (ulong)basePosition;
            ulong end   = (ulong)pagesCount * PageSize + start;

            LinkedListNode<KMemoryBlock> newNode = null;

            LinkedListNode<KMemoryBlock> node = _blocks.First;

            while (node != null)
            {
                KMemoryBlock currBlock = node.Value;

                LinkedListNode<KMemoryBlock> nextNode = node.Next;

                ulong currStart = (ulong)currBlock.BasePosition;
                ulong currEnd   = (ulong)currBlock.PagesCount * PageSize + currStart;

                if (start < currEnd && currStart < end)
                {
                    if (start >= currStart && end <= currEnd)
                    {
                        block.Attribute |= currBlock.Attribute & MemoryAttribute.IpcAndDeviceMapped;
                    }

                    if (start > currStart && end < currEnd)
                    {
                        currBlock.PagesCount = (long)((start - currStart) / PageSize);

                        long nextPagesCount = (long)((currEnd - end) / PageSize);

                        newNode = _blocks.AddAfter(node, block);

                        _blocks.AddAfter(newNode, new KMemoryBlock(
                            (long)end,
                            nextPagesCount,
                            currBlock.State,
                            currBlock.Permission,
                            currBlock.Attribute));

                        break;
                    }
                    else if (start <= currStart && end < currEnd)
                    {
                        currBlock.BasePosition = (long)end;

                        currBlock.PagesCount = (long)((currEnd - end) / PageSize);

                        if (newNode == null)
                        {
                            newNode = _blocks.AddBefore(node, block);
                        }
                    }
                    else if (start > currStart && end >= currEnd)
                    {
                        currBlock.PagesCount = (long)((start - currStart) / PageSize);

                        if (newNode == null)
                        {
                            newNode = _blocks.AddAfter(node, block);
                        }
                    }
                    else
                    {
                        if (newNode == null)
                        {
                            newNode = _blocks.AddBefore(node, block);
                        }

                        _blocks.Remove(node);
                    }
                }

                node = nextNode;
            }

            if (newNode == null)
            {
                newNode = _blocks.AddFirst(block);
            }

            MergeEqualStateNeighbours(newNode);
        }

        private void MergeEqualStateNeighbours(LinkedListNode<KMemoryBlock> node)
        {
            KMemoryBlock block = node.Value;

            ulong start = (ulong)block.BasePosition;
            ulong end   = (ulong)block.PagesCount * PageSize + start;

            if (node.Previous != null)
            {
                KMemoryBlock previous = node.Previous.Value;

                if (BlockStateEquals(block, previous))
                {
                    _blocks.Remove(node.Previous);

                    block.BasePosition = previous.BasePosition;

                    start = (ulong)block.BasePosition;
                }
            }

            if (node.Next != null)
            {
                KMemoryBlock next = node.Next.Value;

                if (BlockStateEquals(block, next))
                {
                    _blocks.Remove(node.Next);

                    end = (ulong)(next.BasePosition + next.PagesCount * PageSize);
                }
            }

            block.PagesCount = (long)((end - start) / PageSize);
        }

        private static bool BlockStateEquals(KMemoryBlock lhs, KMemoryBlock rhs)
        {
            return lhs.State          == rhs.State          &&
                   lhs.Permission     == rhs.Permission     &&
                   lhs.Attribute      == rhs.Attribute      &&
                   lhs.DeviceRefCount == rhs.DeviceRefCount &&
                   lhs.IpcRefCount    == rhs.IpcRefCount;
        }

        private KMemoryBlock FindBlock(long position)
        {
            return FindBlockNode(position)?.Value;
        }

        private LinkedListNode<KMemoryBlock> FindBlockNode(long position)
        {
            ulong addr = (ulong)position;

            lock (_blocks)
            {
                LinkedListNode<KMemoryBlock> node = _blocks.First;

                while (node != null)
                {
                    KMemoryBlock block = node.Value;

                    ulong start = (ulong)block.BasePosition;
                    ulong end   = (ulong)block.PagesCount * PageSize + start;

                    if (start <= addr && end - 1 >= addr)
                    {
                        return node;
                    }

                    node = node.Next;
                }
            }

            return null;
        }
    }
}