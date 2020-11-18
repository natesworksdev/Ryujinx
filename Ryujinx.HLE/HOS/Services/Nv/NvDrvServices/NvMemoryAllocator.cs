using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices
{
    class NvMemoryAllocator
    {

        public const ulong AddressSpaceSize = 1UL << 40;

        public const ulong BadAddress = ulong.MaxValue;

        private const int PtLvl0Bits = 14;
        private const int PtLvl1Bits = 14;
        public const int PtPageBits = 12;

        private const ulong PtLvl0Size = 1UL << PtLvl0Bits;
        private const ulong PtLvl1Size = 1UL << PtLvl1Bits;
        public const ulong PageSize = 1UL << PtPageBits;

        private const ulong PtLvl0Mask = PtLvl0Size - 1;
        private const ulong PtLvl1Mask = PtLvl1Size - 1;
        public const ulong PageMask = PageSize - 1;

        private const int PtLvl0Bit = PtPageBits + PtLvl1Bits;
        private const int PtLvl1Bit = PtPageBits;

        public const ulong PteUnmapped = 0xffffffff_ffffffff;
        public const ulong PteReserved = 0xffffffff_fffffffe;

        private TreeDictionary<ulong, MemoryBlock> _tree = new TreeDictionary<ulong, MemoryBlock>();

        public NvMemoryAllocator()
        {
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

    }
}
