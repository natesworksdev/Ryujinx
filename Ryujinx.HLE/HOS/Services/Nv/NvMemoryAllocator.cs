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

        private TreeDictionary<ulong, MemoryBlock> _tree = new TreeDictionary<ulong, MemoryBlock>();

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
                    ulong refEndAddress = referenceBlock.endAddress;
                    if (va >= referenceBlock.address)
                    {
                        // Need Left Node
                        if (va > referenceBlock.address)
                        {
                            ulong leftEndAddress = va;

                            //Overwrite existing block with its new smaller range.
                            _tree.Add(referenceBlock.address, new MemoryBlock(referenceBlock.address, leftEndAddress - referenceBlock.address));
                        }
                        else
                        {
                            // We need to get rid of the large chunk.
                            _tree.Remove(referenceBlock.address);
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
                // FIXME Figure out how to make this work.
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
        internal bool IsRegionInUse(ulong gpuVa, ulong size, out Node<ulong, MemoryBlock> memoryNode)
        {
            lock (_tree)
            {
                Node<ulong, MemoryBlock> floorNode = _tree.FloorNode(gpuVa);
                memoryNode = floorNode;
                if (null != floorNode)
                {
                    MemoryBlock memoryBlock = floorNode.Value;
                    return !(gpuVa >= memoryBlock.address && ((gpuVa + size) < memoryBlock.endAddress));
                }
            }
            return true;
        }
        #endregion
    }
}
