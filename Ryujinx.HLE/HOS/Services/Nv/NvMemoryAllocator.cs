using Ryujinx.Common.Collections;
using System.Collections.Generic;
using Ryujinx.Common;
using System;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices
{
    class NvMemoryAllocator
    {
        private const ulong AddressSpaceSize = 1UL << 40;

        private const ulong DefaultStart = 1UL << 32;
        private const ulong InvalidAddress = 0;

        private const ulong PageSize = MemoryManager.PageSize;
        private const ulong PageMask = MemoryManager.PageMask;

        public const ulong PteUnmapped = MemoryManager.PteUnmapped;

        // Key   --> Start Address of Region
        // Value --> End Address of Region
        private readonly TreeDictionary<ulong, ulong> _tree = new TreeDictionary<ulong, ulong>();

        private readonly Dictionary<ulong, LinkedListNode<ulong>> _dictionary = new Dictionary<ulong, LinkedListNode<ulong>>();
        private readonly LinkedList<ulong> _list = new LinkedList<ulong>();

        public NvMemoryAllocator()
        {
            _tree.Add(PageSize, PageSize + AddressSpaceSize);
            LinkedListNode<ulong> node = _list.AddFirst(PageSize);
            _dictionary[PageSize] = node;
        }

        /// <summary>
        /// Marks a range of memory as consumed by removing it from the tree.
        /// This function will split memory regions if there is available space.
        /// </summary>
        /// <param name="va">Virtual address at which to allocate</param>
        /// <param name="size">Size of the allocation in bytes</param>
        /// <param name="referenceAddress">Reference to the address of memory where the allocation can take place</param>
        #region Memory Allocation
        public void AllocateRange(ulong va, ulong size, ulong referenceAddress = InvalidAddress)
        {
            lock (_tree)
            {
                Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Allocating range @ {va} to {va + size}");
                if (referenceAddress != InvalidAddress)
                {
                    ulong endAddress = va + size;
                    ulong referenceEndAddress = _tree.Get(referenceAddress);
                    if (va >= referenceAddress)
                    {
                        // Need Left Node
                        if (va > referenceAddress)
                        {
                            ulong leftEndAddress = va;

                            // Overwrite existing block with its new smaller range.
                            _tree.Add(referenceAddress, leftEndAddress);
                            Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Created smaller range range @ {referenceAddress} to {leftEndAddress}");
                        }
                        else
                        {
                            // We need to get rid of the large chunk.
                            _tree.Remove(referenceAddress);
                        }

                        ulong rightSize = referenceEndAddress - endAddress;
                        // If leftover space, create a right node.
                        if (rightSize > 0)
                        {
                            Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Created smaller range range @ {endAddress} to {referenceEndAddress}");
                            _tree.Add(endAddress, referenceEndAddress);

                            LinkedListNode<ulong> node = _list.AddAfter(_dictionary[referenceAddress], endAddress);
                            _dictionary[endAddress] = node;
                        }

                        if (va == referenceAddress)
                        {
                            _list.Remove(_dictionary[referenceAddress]);
                            _dictionary.Remove(referenceAddress);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Marks a range of memory as free by adding it to the tree.
        /// This function will automatically compact the tree when it determines there are multiple ranges of free memory adjacent to each other.
        /// </summary>
        /// <param name="va">Virtual address at which to deallocate</param>
        /// <param name="size">Size of the allocation in bytes</param>
        public void DeallocateRange(ulong va, ulong size)
        {
            lock (_tree)
            {
                Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Deallocating range @ {va} to {va + size}");

                ulong freeAddressStartPosition = _tree.Floor(va);
                if (freeAddressStartPosition != InvalidAddress)
                {
                    LinkedListNode<ulong> node = _dictionary[freeAddressStartPosition];
                    ulong targetPrevAddress = _dictionary[freeAddressStartPosition].Previous != null ? _dictionary[_dictionary[freeAddressStartPosition].Previous.Value].Value : InvalidAddress;
                    ulong targetNextAddress = _dictionary[freeAddressStartPosition].Next != null ? _dictionary[_dictionary[freeAddressStartPosition].Next.Value].Value : InvalidAddress;
                    ulong expandedStart = va;
                    ulong expandedEnd = va + size;

                    while (targetPrevAddress != InvalidAddress)
                    {
                        ulong prevAddress = targetPrevAddress;
                        ulong prevEndAddress = _tree.Get(targetPrevAddress);
                        if (prevEndAddress >= expandedStart)
                        {
                            expandedStart = targetPrevAddress;
                            LinkedListNode<ulong> prevPtr = _dictionary[prevAddress];
                            if (prevPtr.Previous != null)
                            {
                                targetPrevAddress = prevPtr.Previous.Value;
                            }
                            else
                            {
                                targetPrevAddress = InvalidAddress;
                            }
                            node = node.Previous;
                            _tree.Remove(prevAddress);
                            _list.Remove(_dictionary[prevAddress]);
                            _dictionary.Remove(prevAddress);
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (targetNextAddress != InvalidAddress)
                    {
                        ulong nextAddress = targetNextAddress;
                        ulong nextEndAddress = _tree.Get(targetNextAddress);
                        if (nextAddress <= expandedEnd)
                        {
                            expandedEnd = Math.Max(expandedEnd, nextEndAddress);
                            LinkedListNode<ulong> nextPtr = _dictionary[nextAddress];
                            if (nextPtr.Next != null)
                            {
                                targetNextAddress = nextPtr.Next.Value;
                            }
                            else
                            {
                                targetNextAddress = InvalidAddress;
                            }
                            _tree.Remove(nextAddress);
                            _list.Remove(_dictionary[nextAddress]);
                            _dictionary.Remove(nextAddress);
                        }
                        else
                        {
                            break;
                        }
                    }

                    Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Freed range @ {expandedStart} to {expandedEnd}");

                    _tree.Add(expandedStart, expandedEnd);
                    LinkedListNode<ulong> nodePtr = _list.AddAfter(node, expandedStart);
                    _dictionary[expandedStart] = nodePtr;
                }
            }
        }

        /// <summary>
        /// Gets the address of an unused (free) region of the specified size.
        /// </summary>
        /// <param name="size">Size of the region in bytes</param>
        /// <param name="freeAddressStartPosition">Position at which memory can be allocated</param>
        /// <param name="alignment">Required alignment of the region address in bytes</param>
        /// <param name="start">Start address of the search on the address space</param>
        /// <returns>GPU virtual address of the allocation, or an all ones mask in case of failure</returns>
        public ulong GetFreeAddress(ulong size, out ulong freeAddressStartPosition, ulong alignment = 1, ulong start = DefaultStart)
        {
            // Note: Address 0 is not considered valid by the driver,
            // when 0 is returned it's considered a mapping error.
            lock (_tree)
            {
                Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Getting Free Address Search @ {start} w/ Size {size}");
                ulong address = start;

                if (alignment == 0)
                {
                    alignment = 1;
                }

                alignment = (alignment + PageMask) & ~PageMask;
                if (address < AddressSpaceSize)
                {
                    bool completedFirstPass = false;
                    ulong targetAddress;
                    if(start == DefaultStart)
                    {
                        Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Using Last Value in List: {_list.Last.Value}");
                        targetAddress = _list.Last.Value;
                    }
                    else
                    {
                        targetAddress = _tree.Floor(address);
                        Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Using Floor of Tree: {targetAddress}");
                        if (targetAddress == InvalidAddress)
                        {
                            Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Using Ceiling of Address: {address}");
                            targetAddress = _tree.Ceiling(address);
                            Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Ceiling Found: {address}");
                        }
                    }
                    while (address < AddressSpaceSize)
                    {
                        if (targetAddress != InvalidAddress)
                        {
                            if (address >= targetAddress)
                            {
                                if (address + size <= _tree.Get(targetAddress))
                                {
                                    Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Found Free Address: {targetAddress} for {address}");
                                    freeAddressStartPosition = targetAddress;
                                    return address;
                                }
                                else
                                {
                                    Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Need Successor");
                                    LinkedListNode<ulong> nextPtr = _dictionary[targetAddress];
                                    if (nextPtr.Next != null)
                                    {
                                        targetAddress = nextPtr.Next.Value;
                                        Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Using Successor: {targetAddress}");
                                    }
                                    else
                                    {
                                        if (completedFirstPass)
                                        {
                                            Logger.Debug?.PrintMsg(LogClass.ServiceNv, "Exiting Loop ( Completed First Pass )");
                                            break;
                                        }
                                        else
                                        {
                                            completedFirstPass = true;
                                            address = start;
                                            targetAddress = _tree.Floor(address);
                                            Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Completed First Pass, Starting Loop @ {targetAddress} for {address}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                address += PageSize * (targetAddress / PageSize - (address / PageSize));

                                ulong remainder = address % alignment;

                                if (remainder != 0)
                                {
                                    address = (address - remainder) + alignment;
                                }
                                Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Incrementing & Aligned Address: {address}");

                                if(address + size > AddressSpaceSize && !completedFirstPass)
                                {
                                    completedFirstPass = true;
                                    address = start;
                                    targetAddress = _tree.Floor(address);
                                    Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"Completed First Pass, (Address greater than maximum allowed); Starting Loop @ {targetAddress} for {address}");
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                Logger.Debug?.PrintMsg(LogClass.ServiceNv, $"No Suitable Address Found, Returning: {InvalidAddress}");
                freeAddressStartPosition = InvalidAddress;
            }

            return PteUnmapped;
        }

        /// <summary>
        /// Checks if a given memory region is mapped or reserved.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the page</param>
        /// <param name="size">Size of the allocation in bytes</param>
        /// <param name="freeAddressStartPosition">Nearest lower address that memory can be allocated</param>
        /// <returns>True if the page is mapped or reserved, false otherwise</returns>
        public bool IsRegionInUse(ulong gpuVa, ulong size, out ulong freeAddressStartPosition)
        {
            lock (_tree)
            {
                ulong floorAddress = _tree.Floor(gpuVa);
                freeAddressStartPosition = floorAddress;
                if (floorAddress != InvalidAddress)
                {
                    return !(gpuVa >= floorAddress && ((gpuVa + size) < _tree.Get(floorAddress)));
                }
            }
            return true;
        }
        #endregion
    }
}
