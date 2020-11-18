using Ryujinx.Cpu;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU memory manager.
    /// </summary>
    public class MemoryManager
    {
        private const ulong AddressSpaceSize = 1UL << 40;

        public const ulong BadAddress = ulong.MaxValue;

        private const int PtLvl0Bits = 14;
        private const int PtLvl1Bits = 14;
        public  const int PtPageBits = 12;

        private const ulong PtLvl0Size = 1UL << PtLvl0Bits;
        private const ulong PtLvl1Size = 1UL << PtLvl1Bits;
        public  const ulong PageSize   = 1UL << PtPageBits;

        private const ulong PtLvl0Mask = PtLvl0Size - 1;
        private const ulong PtLvl1Mask = PtLvl1Size - 1;
        public  const ulong PageMask   = PageSize   - 1;

        private const int PtLvl0Bit = PtPageBits + PtLvl1Bits;
        private const int PtLvl1Bit = PtPageBits;

        private const ulong PteUnmapped = 0xffffffff_ffffffff;
        private const ulong PteReserved = 0xffffffff_fffffffe;

        private readonly ulong[][] _pageTable;

        public event EventHandler<UnmapEventArgs> MemoryUnmapped;

        private TreeDictionary<ulong, MemoryBlock> _map = new TreeDictionary<ulong, MemoryBlock>();

        private GpuContext _context;

        /// <summary>
        /// Creates a new instance of the GPU memory manager.
        /// </summary>
        public MemoryManager(GpuContext context)
        {
            _context = context;
            _pageTable = new ulong[PtLvl0Size][];
            _map.Add(4096UL, new MemoryBlock(4096UL, AddressSpaceSize));
        }

        /// <summary>
        /// Reads data from GPU mapped memory.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="gpuVa">GPU virtual address where the data is located</param>
        /// <returns>The data at the specified memory location</returns>
        public T Read<T>(ulong gpuVa) where T : unmanaged
        {
            ulong processVa = Translate(gpuVa);

            return MemoryMarshal.Cast<byte, T>(_context.PhysicalMemory.GetSpan(processVa, Unsafe.SizeOf<T>()))[0];
        }

        /// <summary>
        /// Gets a read-only span of data from GPU mapped memory.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address where the data is located</param>
        /// <param name="size">Size of the data</param>
        /// <returns>The span of the data at the specified memory location</returns>
        public ReadOnlySpan<byte> GetSpan(ulong gpuVa, int size)
        {
            ulong processVa = Translate(gpuVa);

            return _context.PhysicalMemory.GetSpan(processVa, size);
        }

        /// <summary>
        /// Gets a writable region from GPU mapped memory.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes to be range</param>
        /// <returns>A writable region with the data at the specified memory location</returns>
        public WritableRegion GetWritableRegion(ulong gpuVa, int size)
        {
            ulong processVa = Translate(gpuVa);

            return _context.PhysicalMemory.GetWritableRegion(processVa, size);
        }

        /// <summary>
        /// Writes data to GPU mapped memory.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="gpuVa">GPU virtual address to write the value into</param>
        /// <param name="value">The value to be written</param>
        public void Write<T>(ulong gpuVa, T value) where T : unmanaged
        {
            ulong processVa = Translate(gpuVa);

            _context.PhysicalMemory.Write(processVa, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)));
        }

        /// <summary>
        /// Writes data to GPU mapped memory.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void Write(ulong gpuVa, ReadOnlySpan<byte> data)
        {
            ulong processVa = Translate(gpuVa);

            _context.PhysicalMemory.Write(processVa, data);
        }

        /// <summary>
        /// Maps a given range of pages to the specified CPU virtual address.
        /// </summary>
        /// <remarks>
        /// All addresses and sizes must be page aligned.
        /// </remarks>
        /// <param name="pa">CPU virtual address to map into</param>
        /// <param name="va">GPU virtual address to be mapped</param>
        /// <param name="size">Size in bytes of the mapping</param>
        /// <returns>GPU virtual address of the mapping</returns>
        public ulong Map(ulong pa, ulong va, ulong size)
        {
            lock (_pageTable)
            {
                MemoryUnmapped?.Invoke(this, new UnmapEventArgs(va, size));
                AllocateMemoryBlock(va, size, null);
                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, pa + offset);
                }
            }

            return va;
        }

        /// <summary>
        /// Maps a given range of pages to an allocated GPU virtual address.
        /// The memory is automatically allocated by the memory manager.
        /// </summary>
        /// <param name="pa">CPU virtual address to map into</param>
        /// <param name="size">Size in bytes of the mapping</param>
        /// <param name="alignment">Required alignment of the GPU virtual address in bytes</param>
        /// <returns>GPU virtual address where the range was mapped, or an all ones mask in case of failure</returns>
        public ulong MapAllocate(ulong pa, ulong size, ulong alignment)
        {
            lock (_pageTable)
            {
                ulong va = GetFreePosition(size, out TreeNode<ulong, MemoryBlock> referenceBlock, alignment);

                if (va != PteUnmapped)
                {
                    AllocateMemoryBlock(va, size, referenceBlock);
                    for (ulong offset = 0; offset < size; offset += PageSize)
                    {
                        SetPte(va + offset, pa + offset);
                    }
                }

                return va;
            }
        }

        /// <summary>
        /// Maps a given range of pages to an allocated GPU virtual address.
        /// The memory is automatically allocated by the memory manager.
        /// This also ensures that the mapping is always done in the first 4GB of GPU address space.
        /// </summary>
        /// <param name="pa">CPU virtual address to map into</param>
        /// <param name="size">Size in bytes of the mapping</param>
        /// <returns>GPU virtual address where the range was mapped, or an all ones mask in case of failure</returns>
        public ulong MapLow(ulong pa, ulong size)
        {
            lock (_pageTable)
            {
                ulong va = GetFreePosition(size, out TreeNode<ulong, MemoryBlock> referenceBlock, 1, PageSize);

                if (va != PteUnmapped && va <= uint.MaxValue && (va + size) <= uint.MaxValue)
                {
                    AllocateMemoryBlock(va, size, referenceBlock);
                    for (ulong offset = 0; offset < size; offset += PageSize)
                    {
                        SetPte(va + offset, pa + offset);
                    }
                }
                else
                {
                    va = PteUnmapped;
                }

                return va;
            }
        }

        /// <summary>
        /// Reserves memory at a fixed GPU memory location.
        /// This prevents the reserved region from being used for memory allocation for map.
        /// </summary>
        /// <param name="va">GPU virtual address to reserve</param>
        /// <param name="size">Size in bytes of the reservation</param>
        /// <returns>GPU virtual address of the reservation, or an all ones mask in case of failure</returns>
        public ulong ReserveFixed(ulong va, ulong size)
        {
            lock (_pageTable)
            {
                MemoryUnmapped?.Invoke(this, new UnmapEventArgs(va, size));

                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    if (IsPageInUse(va + offset))
                    {
                        return PteUnmapped;
                    }
                }

                AllocateMemoryBlock(va, size, null);
                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, PteReserved);
                }
            }

            return va;
        }

        /// <summary>
        /// Reserves memory at any GPU memory location.
        /// </summary>
        /// <param name="size">Size in bytes of the reservation</param>
        /// <param name="alignment">Reservation address alignment in bytes</param>
        /// <returns>GPU virtual address of the reservation, or an all ones mask in case of failure</returns>
        public ulong Reserve(ulong size, ulong alignment)
        {
            lock (_pageTable)
            {
                ulong address = GetFreePosition(size, out TreeNode<ulong, MemoryBlock> referenceBlock, alignment);

                if (address != PteUnmapped)
                {
                    AllocateMemoryBlock(address, size, referenceBlock);
                    for (ulong offset = 0; offset < size; offset += PageSize)
                    {
                        SetPte(address + offset, PteReserved);
                    }
                }

                return address;
            }
        }

        /// <summary>
        /// Frees memory that was previously allocated by a map or reserved.
        /// </summary>
        /// <param name="va">GPU virtual address to free</param>
        /// <param name="size">Size in bytes of the region being freed</param>
        public void Free(ulong va, ulong size)
        {
            lock (_pageTable)
            {
                // Event handlers are not expected to be thread safe.
                MemoryUnmapped?.Invoke(this, new UnmapEventArgs(va, size));
                DeallocateMemoryBlock(va, size);
                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, PteUnmapped);
                }
            }
        }

        public void AllocateMemoryBlock(ulong va, ulong size, TreeNode<ulong, MemoryBlock> reference)
        {
            lock (_map)
            {
                if (reference != null)
                {
                    MemoryBlock referenceBlock = reference.Value;
                    // Fixed Addresses are being mapped. Ignore the reference.
                    if (referenceBlock.address == PteUnmapped)
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
                    while(prev != null)
                    {
                        MemoryBlock prevBlock = prev.Value;
                        ulong prevAddress = prevBlock.address;
                        if(prevBlock.endAddress == expandedStart - 1UL)
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

                    while(next != null)
                    {
                        MemoryBlock nextBlock = next.Value;
                        ulong nextAddress = nextBlock.address;
                        if(nextBlock.address == expandedEnd - 1UL)
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
            ulong address  = start;

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
                        if(address >= block.address)
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

        /// <summary>
        /// Checks if a given page is mapped.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the page to check</param>
        /// <returns>True if the page is mapped, false otherwise</returns>
        public bool IsMapped(ulong gpuVa)
        {
            return Translate(gpuVa) != PteUnmapped;
        }

        /// <summary>
        /// Translates a GPU virtual address to a CPU virtual address.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address to be translated</param>
        /// <returns>CPU virtual address</returns>
        public ulong Translate(ulong gpuVa)
        {
            ulong baseAddress = GetPte(gpuVa);

            if (baseAddress == PteUnmapped || baseAddress == PteReserved)
            {
                return PteUnmapped;
            }

            return baseAddress + (gpuVa & PageMask);
        }

        /// <summary>
        /// Checks if a given memory page is mapped or reserved.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the page</param>
        /// <returns>True if the page is mapped or reserved, false otherwise</returns>
        private bool IsPageInUse(ulong gpuVa)
        {
            if (gpuVa >> PtLvl0Bits + PtLvl1Bits + PtPageBits != 0)
            {
                return false;
            }

            ulong l0 = (gpuVa >> PtLvl0Bit) & PtLvl0Mask;
            ulong l1 = (gpuVa >> PtLvl1Bit) & PtLvl1Mask;

            if (_pageTable[l0] == null)
            {
                return false;
            }

            return _pageTable[l0][l1] != PteUnmapped;
        }

        /// <summary>
        /// Gets the Page Table entry for a given GPU virtual address.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address</param>
        /// <returns>Page table entry (CPU virtual address)</returns>
        private ulong GetPte(ulong gpuVa)
        {
            ulong l0 = (gpuVa >> PtLvl0Bit) & PtLvl0Mask;
            ulong l1 = (gpuVa >> PtLvl1Bit) & PtLvl1Mask;

            if (_pageTable[l0] == null)
            {
                return PteUnmapped;
            }

            return _pageTable[l0][l1];
        }

        /// <summary>
        /// Sets a Page Table entry at a given GPU virtual address.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address</param>
        /// <param name="pte">Page table entry (CPU virtual address)</param>
        private void SetPte(ulong gpuVa, ulong pte)
        {
            ulong l0 = (gpuVa >> PtLvl0Bit) & PtLvl0Mask;
            ulong l1 = (gpuVa >> PtLvl1Bit) & PtLvl1Mask;

            if (_pageTable[l0] == null)
            {
                _pageTable[l0] = new ulong[PtLvl1Size];

                for (ulong index = 0; index < PtLvl1Size; index++)
                {
                    _pageTable[l0][index] = PteUnmapped;
                }
            }

            _pageTable[l0][l1] = pte;
        }
    }
}