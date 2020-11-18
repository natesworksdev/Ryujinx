using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu.Types;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using System;
using System.Collections.Concurrent;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu
{
    class NvHostAsGpuDeviceFile : NvDeviceFile
    {
        private static ConcurrentDictionary<KProcess, AddressSpaceContext> _addressSpaceContextRegistry = new ConcurrentDictionary<KProcess, AddressSpaceContext>();

        private TreeDictionary<ulong, MemoryBlock> _map = new TreeDictionary<ulong, MemoryBlock>();

        public NvHostAsGpuDeviceFile(ServiceCtx context) : base(context) {
            _map.Add(4096UL, new MemoryBlock(4096UL, MemoryManager.AddressSpaceSize));
        }

        #region Memory Allocation

        private void AllocateMemoryBlock(ulong va, ulong size, TreeNode<ulong, MemoryBlock> reference)
        {
            lock (_map)
            {
                if (reference != null)
                {
                    MemoryBlock referenceBlock = reference.Value;
                    // Fixed Addresses are being mapped. Ignore the reference.
                    if (referenceBlock.address == MemoryManager.PteUnmapped)
                    {
                        TreeNode<ulong, MemoryBlock> entry = _map.PredecessorOf(reference);
                        if (null == entry) return;
                        referenceBlock = entry.Value;

                        if (!(va >= referenceBlock.address && va + size <= referenceBlock.endAddress)) return;
                    }

                    if (va > referenceBlock.address)
                    {
                        // Need to create a left block.
                        MemoryBlock leftBlock = new MemoryBlock(referenceBlock.address, va - referenceBlock.address);
                        _map.Add(referenceBlock.address, leftBlock);
                    }
                    else if (va == referenceBlock.address)
                    {
                        _map.Remove(va);
                    }
                    ulong endAddress = va + size;
                    if (endAddress < referenceBlock.endAddress)
                    {
                        // Need to create a right block.
                        MemoryBlock rightBlock = new MemoryBlock(endAddress, referenceBlock.endAddress - endAddress);
                        _map.Add(endAddress, rightBlock);
                    }
                }
            }
        }

        /// <summary>
        /// Marks a block of memory as free by adding it to the tree.
        /// This function will automatically defragment the tree when it determines there are multiple blocks of free memory adjacent to each other.
        /// </summary>
        /// <param name="va"></param>
        /// <param name="size"></param>
        public void DeallocateMemoryBlock(ulong va, ulong size)
        {
            lock (_map)
            {
                TreeNode<ulong, MemoryBlock> entry = _map.GetNode(va);
                if (null != entry)
                {
                    TreeNode<ulong, MemoryBlock> prev = _map.PredecessorOf(entry);
                    TreeNode<ulong, MemoryBlock> next = _map.SuccessorOf(entry);
                    ulong expandedStart = va;
                    ulong expandedEnd = va + size;
                    while (prev != null)
                    {
                        MemoryBlock prevBlock = prev.Value;
                        ulong prevAddress = prevBlock.address;
                        if (prevBlock.endAddress == expandedStart - 1UL)
                        {
                            expandedStart = prevAddress;
                            prev = _map.PredecessorOf(prev);
                            _map.Remove(prevAddress);
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (next != null)
                    {
                        MemoryBlock nextBlock = next.Value;
                        ulong nextAddress = nextBlock.address;
                        if (nextBlock.address == expandedEnd - 1UL)
                        {
                            expandedEnd = nextBlock.endAddress;
                            next = _map.SuccessorOf(next);
                            _map.Remove(nextAddress);
                        }
                        else
                        {
                            break;
                        }
                    }

                    _map.Add(expandedStart, new MemoryBlock(expandedStart, expandedEnd - expandedStart));
                }
            }
        }

        /// <summary>
        /// Gets the address of an unused (free) region of the specified size.
        /// </summary>
        /// <param name="size">Size of the region in bytes</param>
        /// <param name="alignment">Required alignment of the region address in bytes</param>
        /// <param name="start">Start address of the search on the address space</param>
        /// <returns>GPU virtual address of the allocation, or an all ones mask in case of failure</returns>
        private ulong GetFreePosition(ulong size, out TreeNode<ulong, MemoryBlock> memoryBlock, ulong alignment = 1, ulong start = 1UL << 32)
        {
            // Note: Address 0 is not considered valid by the driver,
            // when 0 is returned it's considered a mapping error.
            ulong address = start;

            if (alignment == 0)
            {
                alignment = 1;
            }

            alignment = (alignment + PageMask) & ~PageMask;
            if (address < AddressSpaceSize)
            {
                TreeNode<ulong, MemoryBlock> blockNode = _map.Count == 1 ? _map.FloorNode(address) : _map.CeilingNode(address);
                while (address < AddressSpaceSize)
                {
                    if (blockNode != null)
                    {
                        MemoryBlock block = blockNode.Value;
                        if (address >= block.address)
                        {
                            if (address + size <= block.endAddress)
                            {
                                memoryBlock = blockNode;
                                return address;
                            }
                            else
                            {
                                blockNode = _map.SuccessorOf(blockNode);
                            }
                        }
                        else
                        {
                            address += PageSize;

                            ulong remainder = address % alignment;

                            if (remainder != 0)
                            {
                                address = (address - remainder) + alignment;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            memoryBlock = null;
            return PteUnmapped;
        }

        #endregion

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvGpuAsMagic)
            {
                switch (command.Number)
                {
                    case 0x01:
                        result = CallIoctlMethod<BindChannelArguments>(BindChannel, arguments);
                        break;
                    case 0x02:
                        result = CallIoctlMethod<AllocSpaceArguments>(AllocSpace, arguments);
                        break;
                    case 0x03:
                        result = CallIoctlMethod<FreeSpaceArguments>(FreeSpace, arguments);
                        break;
                    case 0x05:
                        result = CallIoctlMethod<UnmapBufferArguments>(UnmapBuffer, arguments);
                        break;
                    case 0x06:
                        result = CallIoctlMethod<MapBufferExArguments>(MapBufferEx, arguments);
                        break;
                    case 0x08:
                        result = CallIoctlMethod<GetVaRegionsArguments>(GetVaRegions, arguments);
                        break;
                    case 0x09:
                        result = CallIoctlMethod<InitializeExArguments>(InitializeEx, arguments);
                        break;
                    case 0x14:
                        result = CallIoctlMethod<RemapArguments>(Remap, arguments);
                        break;
                }
            }

            return result;
        }

        public override NvInternalResult Ioctl3(NvIoctl command, Span<byte> arguments, Span<byte> inlineOutBuffer)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvGpuAsMagic)
            {
                switch (command.Number)
                {
                    case 0x08:
                        // This is the same as the one in ioctl as inlineOutBuffer is empty.
                        result = CallIoctlMethod<GetVaRegionsArguments>(GetVaRegions, arguments);
                        break;
                }
            }

            return result;
        }

        private NvInternalResult BindChannel(ref BindChannelArguments arguments)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult AllocSpace(ref AllocSpaceArguments arguments)
        {
            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(Context);

            ulong size = (ulong)arguments.Pages * (ulong)arguments.PageSize;

            NvInternalResult result = NvInternalResult.Success;

            lock (addressSpaceContext)
            {
                // Note: When the fixed offset flag is not set,
                // the Offset field holds the alignment size instead.
                if ((arguments.Flags & AddressSpaceFlags.FixedOffset) != 0)
                {
                    arguments.Offset = (long)addressSpaceContext.Gmm.ReserveFixed((ulong)arguments.Offset, size);
                }
                else
                {
                    arguments.Offset = (long)addressSpaceContext.Gmm.Reserve((ulong)size, (ulong)arguments.Offset);
                }

                if (arguments.Offset < 0)
                {
                    arguments.Offset = 0;

                    Logger.Warning?.Print(LogClass.ServiceNv, $"Failed to allocate size {size:x16}!");

                    result = NvInternalResult.OutOfMemory;
                }
                else
                {
                    addressSpaceContext.AddReservation(arguments.Offset, (long)size);
                }
            }

            return result;
        }

        private NvInternalResult FreeSpace(ref FreeSpaceArguments arguments)
        {
            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(Context);

            NvInternalResult result = NvInternalResult.Success;

            lock (addressSpaceContext)
            {
                ulong size = (ulong)arguments.Pages * (ulong)arguments.PageSize;

                if (addressSpaceContext.RemoveReservation(arguments.Offset))
                {
                    addressSpaceContext.Gmm.Free((ulong)arguments.Offset, size);
                }
                else
                {
                    Logger.Warning?.Print(LogClass.ServiceNv,
                        $"Failed to free offset 0x{arguments.Offset:x16} size 0x{size:x16}!");

                    result = NvInternalResult.InvalidInput;
                }
            }

            return result;
        }

        private NvInternalResult UnmapBuffer(ref UnmapBufferArguments arguments)
        {
            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(Context);

            lock (addressSpaceContext)
            {
                if (addressSpaceContext.RemoveMap(arguments.Offset, out long size))
                {
                    if (size != 0)
                    {
                        addressSpaceContext.Gmm.Free((ulong)arguments.Offset, (ulong)size);
                    }
                }
                else
                {
                    Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid buffer offset {arguments.Offset:x16}!");
                }
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult MapBufferEx(ref MapBufferExArguments arguments)
        {
            const string mapErrorMsg = "Failed to map fixed buffer with offset 0x{0:x16}, size 0x{1:x16} and alignment 0x{2:x16}!";

            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(Context);

            NvMapHandle map = NvMapDeviceFile.GetMapFromHandle(Owner, arguments.NvMapHandle, true);

            if (map == null)
            {
                Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid NvMap handle 0x{arguments.NvMapHandle:x8}!");

                return NvInternalResult.InvalidInput;
            }

            ulong pageSize = (ulong)arguments.PageSize;

            if (pageSize == 0)
            {
                pageSize = (ulong)map.Align;
            }

            long physicalAddress;

            if ((arguments.Flags & AddressSpaceFlags.RemapSubRange) != 0)
            {
                lock (addressSpaceContext)
                {
                    if (addressSpaceContext.TryGetMapPhysicalAddress(arguments.Offset, out physicalAddress))
                    {
                        long virtualAddress = arguments.Offset + arguments.BufferOffset;

                        physicalAddress += arguments.BufferOffset;

                        if ((long)addressSpaceContext.Gmm.Map((ulong)physicalAddress, (ulong)virtualAddress, (ulong)arguments.MappingSize) < 0)
                        {
                            string message = string.Format(mapErrorMsg, virtualAddress, arguments.MappingSize, pageSize);

                            Logger.Warning?.Print(LogClass.ServiceNv, message);

                            return NvInternalResult.InvalidInput;
                        }

                        return NvInternalResult.Success;
                    }
                    else
                    {
                        Logger.Warning?.Print(LogClass.ServiceNv, $"Address 0x{arguments.Offset:x16} not mapped!");

                        return NvInternalResult.InvalidInput;
                    }
                }
            }

            physicalAddress = map.Address + arguments.BufferOffset;

            long size = arguments.MappingSize;

            if (size == 0)
            {
                size = (uint)map.Size;
            }

            NvInternalResult result = NvInternalResult.Success;

            lock (addressSpaceContext)
            {
                // Note: When the fixed offset flag is not set,
                // the Offset field holds the alignment size instead.
                bool virtualAddressAllocated = (arguments.Flags & AddressSpaceFlags.FixedOffset) == 0;

                if (!virtualAddressAllocated)
                {
                    if (addressSpaceContext.ValidateFixedBuffer(arguments.Offset, size, pageSize))
                    {
                        arguments.Offset = (long)addressSpaceContext.Gmm.Map((ulong)physicalAddress, (ulong)arguments.Offset, (ulong)size);
                    }
                    else
                    {
                        string message = string.Format(mapErrorMsg, arguments.Offset, size, pageSize);

                        Logger.Warning?.Print(LogClass.ServiceNv, message);

                        result = NvInternalResult.InvalidInput;
                    }
                }
                else
                {
                    ulong va = GetFreePosition(size, out TreeNode<ulong, MemoryBlock> memoryBlock, (ulong) pageSize);
                    AllocateMemoryBlock(va, size, memoryBlock);
                    arguments.Offset = (long)addressSpaceContext.Gmm.Map((ulong)physicalAddress, va, size);
                }

                if (arguments.Offset < 0)
                {
                    arguments.Offset = 0;

                    Logger.Warning?.Print(LogClass.ServiceNv, $"Failed to map size 0x{size:x16}!");

                    result = NvInternalResult.InvalidInput;
                }
                else
                {
                    addressSpaceContext.AddMap(arguments.Offset, size, physicalAddress, virtualAddressAllocated);
                }
            }

            return result;
        }

        private NvInternalResult GetVaRegions(ref GetVaRegionsArguments arguments)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult InitializeEx(ref InitializeExArguments arguments)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult Remap(Span<RemapArguments> arguments)
        {
            for (int index = 0; index < arguments.Length; index++)
            {
                MemoryManager gmm = GetAddressSpaceContext(Context).Gmm;

                NvMapHandle map = NvMapDeviceFile.GetMapFromHandle(Owner, arguments[index].NvMapHandle, true);

                if (map == null)
                {
                    Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid NvMap handle 0x{arguments[index].NvMapHandle:x8}!");

                    return NvInternalResult.InvalidInput;
                }

                long result = (long)gmm.Map(
                    ((ulong)arguments[index].MapOffset << 16) + (ulong)map.Address,
                     (ulong)arguments[index].GpuOffset << 16,
                     (ulong)arguments[index].Pages     << 16);

                if (result < 0)
                {
                    Logger.Warning?.Print(LogClass.ServiceNv,
                        $"Page 0x{arguments[index].GpuOffset:x16} size 0x{arguments[index].Pages:x16} not allocated!");

                    return NvInternalResult.InvalidInput;
                }
            }

            return NvInternalResult.Success;
        }

        public override void Close() { }

        public static AddressSpaceContext GetAddressSpaceContext(ServiceCtx context)
        {
            return _addressSpaceContextRegistry.GetOrAdd(context.Process, (key) => new AddressSpaceContext(context));
        }
    }
}
