using Ryujinx.Common.Collections;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices
{
    public class NvMemoryAllocator
    {
        public const ulong AddressSpaceSize = 1UL << 40;

        public const ulong BadAddress = ulong.MaxValue;

        public const int PtPageBits = 12;

        public const ulong PageSize = 1UL << PtPageBits;
        public const ulong PageMask = PageSize - 1;

        public const ulong PteUnmapped = 0xffffffff_ffffffff;
        public const ulong PteReserved = 0xffffffff_fffffffe;

        private readonly TreeDictionary<ulong, MemoryBlock> _tree = new TreeDictionary<ulong, MemoryBlock>();

        public NvMemoryAllocator()
        {
            _tree.Add(PageSize, new MemoryBlock(PageSize, AddressSpaceSize));
        }

        /// <summary>
        /// Marks a block of memory as consumed by removing it from the tree.
        /// This function will split memory regions if there is available space
        /// </summary>
        /// <param name="va">Virtual address at which to allocate</param>
        /// <param name="size">Size of the allocation in bytes</param>
        /// <param name="reference">Reference to the block of memory where the allocation can take place</param>
        #region Memory Allocation
        internal void AllocateMemoryBlock(ulong va, ulong size, Node<ulong, MemoryBlock> reference = null)
        {
            lock (_tree)
            {
                if(reference == null)
                {
                    return;
                }

                if (reference != null)
                {
                    MemoryBlock referenceBlock = reference.Value;
                    ulong endAddress = va + size;
                    ulong refEndAddress = referenceBlock.EndAddress;
                    if (va >= referenceBlock.Address)
                    {
                        // Need Left Node
                        if (va > referenceBlock.Address)
                        {
                            ulong leftEndAddress = va;

                            //Overwrite existing block with its new smaller range.
                            _tree.Add(referenceBlock.Address, new MemoryBlock(referenceBlock.Address, leftEndAddress - referenceBlock.Address));
                        }
                        else
                        {
                            // We need to get rid of the large chunk.
                            _tree.Remove(referenceBlock.Address);
                        }

                        ulong rightSize = refEndAddress - endAddress;
                        // If leftover space, create a right node.
                        if (rightSize > 0)
                        {
                            _tree.Add(endAddress,new MemoryBlock(endAddress, rightSize));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Marks a block of memory as free by adding it to the tree.
        /// This function will automatically defragment the tree when it determines there are multiple blocks of free memory adjacent to each other.
        /// </summary>
        /// <param name="va">Virtual address at which to allocate</param>
        /// <param name="size">Size of the allocation in bytes</param>
        public void DeallocateMemoryBlock(ulong va, ulong size)
        {
            lock (_tree)
            {
                Node<ulong, MemoryBlock> entry = _tree.FloorNode(va);
                if (null != entry)
                {
                    Node<ulong, MemoryBlock> prev = _tree.PredecessorOf(entry);
                    Node<ulong, MemoryBlock> next = _tree.SuccessorOf(entry);
                    ulong expandedStart = va;
                    ulong expandedEnd = va + size;
                    while (prev != null)
                    {
                        MemoryBlock prevBlock = prev.Value;
                        ulong prevAddress = prevBlock.Address;
                        if (prevBlock.EndAddress == expandedStart)
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
                        ulong nextAddress = nextBlock.Address;
                        if (nextBlock.Address == expandedEnd)
                        {
                            expandedEnd = nextBlock.EndAddress;
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
        internal ulong GetFreePosition(ulong size, out Node<ulong, MemoryBlock> memoryBlock, ulong alignment = 1, ulong start = 1UL << 32)
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
                    Node<ulong, MemoryBlock> blockNode = _tree.Count == 1 ? _tree.FloorNode(address) : _tree.CeilingNode(address);
                    while (address < AddressSpaceSize)
                    {
                        if (blockNode != null)
                        {
                            MemoryBlock block = blockNode.Value;
                            if (address >= block.Address)
                            {
                                if (address + size <= block.EndAddress)
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
        internal bool IsRegionInUse(ulong gpuVa, ulong size, out Node<ulong, MemoryBlock> memoryNode)
        {
            lock (_tree)
            {
                Node<ulong, MemoryBlock> floorNode = _tree.FloorNode(gpuVa);
                memoryNode = floorNode;
                if (null != floorNode)
                {
                    MemoryBlock memoryBlock = floorNode.Value;
                    return !(gpuVa >= memoryBlock.Address && ((gpuVa + size) < memoryBlock.EndAddress));
                }
            }
            return true;
        }
        #endregion
    }
}
