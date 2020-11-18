using Ryujinx.Common.Collections;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices
{

    public class NvMemoryAllocator
    {
        private static NvMemoryAllocator nvMemoryAllocator = new NvMemoryAllocator();

        public const ulong AddressSpaceSize = 1UL << 40;

        public const ulong BadAddress = ulong.MaxValue;

        public const int PtPageBits = 12;

        public const ulong PageSize = 1UL << PtPageBits;
        public const ulong PageMask = PageSize - 1;

        public const ulong PteUnmapped = 0xffffffff_ffffffff;
        public const ulong PteReserved = 0xffffffff_fffffffe;

        private TreeDictionary<ulong, MemoryBlock> _tree = new TreeDictionary<ulong, MemoryBlock>();

        private NvMemoryAllocator()
        {
            _tree.Add(4096UL, new MemoryBlock(4096UL, AddressSpaceSize));
        }

        public static NvMemoryAllocator GetInstance()
        {
            return nvMemoryAllocator;
        }

        /// <summary>
        /// Marks a block of memory as consumed by removing it from the tree.
        /// This function will split memory regions if there is available space
        /// </summary>
        /// <param name="va"></param>
        /// <param name="size"></param>
        /// <param name="reference"></param>
        #region Memory Allocation
        public void AllocateMemoryBlock(ulong va, ulong size, TreeNode<ulong, MemoryBlock> reference)
        {
            lock (_tree)
            {
                if (reference != null)
                {
                    MemoryBlock referenceBlock = reference.Value;
                    // Fixed Addresses are being mapped. Ignore the reference.
                    if (referenceBlock.address == PteUnmapped)
                    {
                        TreeNode<ulong, MemoryBlock> entry = _tree.PredecessorOf(reference);
                        if (null == entry) return;
                        referenceBlock = entry.Value;

                        if (!(va >= referenceBlock.address && va + size <= referenceBlock.endAddress)) return;
                    }

                    if (va > referenceBlock.address)
                    {
                        // Need to create a left block.
                        MemoryBlock leftBlock = new MemoryBlock(referenceBlock.address, va - referenceBlock.address);
                        _tree.Add(referenceBlock.address, leftBlock);
                    }
                    else if (va == referenceBlock.address)
                    {
                        _tree.Remove(va);
                    }
                    ulong endAddress = va + size;
                    if (endAddress < referenceBlock.endAddress)
                    {
                        // Need to create a right block.
                        MemoryBlock rightBlock = new MemoryBlock(endAddress, referenceBlock.endAddress - endAddress);
                        _tree.Add(endAddress, rightBlock);
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
            lock (_tree)
            {
                TreeNode<ulong, MemoryBlock> entry = _tree.GetNode(va);
                if (null != entry)
                {
                    TreeNode<ulong, MemoryBlock> prev = _tree.PredecessorOf(entry);
                    TreeNode<ulong, MemoryBlock> next = _tree.SuccessorOf(entry);
                    ulong expandedStart = va;
                    ulong expandedEnd = va + size;
                    while (prev != null)
                    {
                        MemoryBlock prevBlock = prev.Value;
                        ulong prevAddress = prevBlock.address;
                        if (prevBlock.endAddress == expandedStart - 1UL)
                        {
                            expandedStart = prevAddress;
                            prev = _tree.PredecessorOf(prev);
                            _tree.Remove(prevAddress);
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
                            next = _tree.SuccessorOf(next);
                            _tree.Remove(nextAddress);
                        }
                        else
                        {
                            break;
                        }
                    }

                    _tree.Add(expandedStart, new MemoryBlock(expandedStart, expandedEnd - expandedStart));
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
        public ulong GetFreePosition(ulong size, out TreeNode<ulong, MemoryBlock> memoryBlock, ulong alignment = 1, ulong start = 1UL << 32)
        {
            // Note: Address 0 is not considered valid by the driver,
            // when 0 is returned it's considered a mapping error.
            lock (_tree)
            {
                ulong address = start;

                if (alignment == 0)
                {
                    alignment = 1;
                }

                alignment = (alignment + PageMask) & ~PageMask;
                if (address < AddressSpaceSize)
                {
                    TreeNode<ulong, MemoryBlock> blockNode = _tree.Count == 1 ? _tree.FloorNode(address) : _tree.CeilingNode(address);
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
                                    blockNode = _tree.SuccessorOf(blockNode);
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
            }
            return PteUnmapped;
        }

        /// <summary>
        /// Checks if a given memory page is mapped or reserved.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the page</param>
        /// <param name="size">Size of the allocation in bytes</param>
        /// <returns>True if the page is mapped or reserved, false otherwise</returns>
        public bool IsRegionInUse(ulong gpuVa, ulong size, out TreeNode<ulong, MemoryBlock> memoryNode)
        {
            lock (_tree)
            {
                TreeNode<ulong, MemoryBlock> floorNode = _tree.FloorNode(gpuVa);
                memoryNode = floorNode;
                if (null != floorNode)
                {
                    MemoryBlock memoryBlock = floorNode.Value;
                    return (gpuVa >= memoryBlock.address && gpuVa + size < memoryBlock.endAddress);
                }
            }
            return false;
        }
        #endregion
    }
}
