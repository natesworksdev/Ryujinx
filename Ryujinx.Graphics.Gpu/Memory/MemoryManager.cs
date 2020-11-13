using Ryujinx.Cpu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU memory manager.
    /// </summary>
    public class MemoryManager
    {
        private Stopwatch stopwatch = new Stopwatch();
        private const ulong AddressSpaceSize = 1UL << 40;
        private const ulong MinAddress = 1UL;
        private const ulong MaxAddress = AddressSpaceSize;
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
        private LinkedList<MemoryRange> _memory = new LinkedList<MemoryRange>();
        public event EventHandler<UnmapEventArgs> MemoryUnmapped;

        private GpuContext _context;

        /// <summary>
        /// Creates a new instance of the GPU memory manager.
        /// </summary>
        public MemoryManager(GpuContext context)
        {
            _context = context;
            _pageTable = new ulong[PtLvl0Size][];
            _memory.AddFirst(new MemoryRange(MinAddress, MaxAddress));
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
        /// Marks a range of memory as allocated.
        /// </summary>
        /// <param name="address">The address at which to mark</param>
        /// <param name="size">Size in bytes of the range</param>
        private void Alloc(ulong address, ulong size)
        {
            LinkedListNode<MemoryRange> node = _memory.First;
            ulong endAddress = address + size;
            while (node != null)
            {
                MemoryRange range = node.Value;
                if (address >= range.startAddress && endAddress < range.endAddress)
                {
                    ulong rightStart = endAddress + 1UL, rightEnd = range.endAddress;

                    if (address >= range.startAddress)
                    {
                        _memory.AddBefore(node, new MemoryRange(range.startAddress, address - 1UL));
                    }
                    if (rightStart <= rightEnd)
                    {
                        _memory.AddAfter(node, new MemoryRange(rightStart, rightEnd));
                    }

                    _memory.Remove(node);
                   // Console.WriteLine(string.Join(',', _memory));
                    return;
                }
                else
                {
                    node = node.Next;
                }
            }
        }

        /// <summary>
        /// Marks a range of memory as free.
        /// </summary>
        /// <param name="address">The address at which to mark</param>
        /// <param name="size">Size in bytes of the range</param>
        private void Dealloc(ulong address, ulong size)
        {
            LinkedListNode<MemoryRange> node = _memory.First;
            while (node != null)
            {
                MemoryRange range = node.Value;
                if (address < range.startAddress)
                {
                    ulong start = address, end = range.endAddress;

                    LinkedListNode<MemoryRange> prev = node.Previous;

                    while (prev != null)
                    {
                        if (prev.Value.endAddress == start - 1UL)
                        {
                            start = prev.Value.startAddress;
                            _memory.Remove(prev);
                            prev = prev.Previous;
                        }
                        else
                        {
                            break;
                        }
                    }

                    LinkedListNode<MemoryRange> next = node.Next;

                    while(next != null)
                    {
                        if(next.Value.startAddress == end + 1UL)
                        {
                            end = next.Value.endAddress;
                            _memory.Remove(next);
                            next = next.Next;
                        }
                        else {
                            break;
                        }
                    }

                    _memory.AddBefore(node, new MemoryRange(start, end));
                    _memory.Remove(node);
                    return;
                }
                else
                {
                    node = node.Next;
                }
            }
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

                ulong remainder = size > PageSize ? size % PageSize > 0 ? PageSize : 0 : 0;
                Alloc(va, Math.Max(PageSize * (size / PageSize) + remainder, PageSize));
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
                ulong va = GetFreePosition(size, alignment);

                if (va != PteUnmapped)
                {
                    ulong remainder = size > PageSize ? size % PageSize > 0 ? PageSize : 0 : 0;
                    Alloc(va, Math.Max(PageSize * (size / PageSize) + remainder, PageSize));
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
                ulong va = GetFreePosition(size, 1, PageSize);

                if (va != PteUnmapped && va <= uint.MaxValue && (va + size) <= uint.MaxValue)
                {
                    ulong remainder = size > PageSize ? size % PageSize > 0 ? PageSize : 0 : 0;
                    Alloc(va, Math.Max(PageSize * (size / PageSize) + remainder, PageSize));
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
                foreach(MemoryRange range in _memory)
                {
                    if (va >= range.startAddress && range.endAddress - range.startAddress >= size)
                    {
                        ulong remainder = size > PageSize ? size % PageSize > 0 ? PageSize : 0 : 0;
                        Alloc(va, Math.Max(PageSize * (size / PageSize) + remainder, PageSize));
                        for (ulong offset = 0; offset < size; offset += PageSize)
                        {
                            SetPte(va + offset, PteReserved);
                        }
                        return va;
                    }
                }

                return PteUnmapped;
            }
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
                ulong address = GetFreePosition(size, alignment);

                if (address != PteUnmapped)
                {
                    ulong remainder = size > PageSize ? size % PageSize > 0 ? PageSize : 0 : 0;
                    Alloc(address, Math.Max(PageSize * (size / PageSize) + remainder, PageSize));
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

                ulong remainder = size > PageSize ? size % PageSize > 0 ? PageSize : 0 : 0;
                Dealloc(va, Math.Max(PageSize * (size / PageSize) + remainder, PageSize));
                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, PteUnmapped);
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
        private ulong GetFreePosition(ulong size, ulong alignment = 1, ulong start = 1UL << 32)
        {
            stopwatch.Restart();

            // Note: Address 0 is not considered valid by the driver,
            // when 0 is returned it's considered a mapping error.
            ulong address = start;

            if (alignment == 0)
            {
                alignment = 1;
            }

            alignment = (alignment + PageMask) & ~PageMask;

            while (address <= AddressSpaceSize)
            {
                LinkedListNode<MemoryRange> node = _memory.First;
                while(null != node)
                {
                    MemoryRange memoryRange = node.Value;
                    if (memoryRange.size >= PageSize && address >= memoryRange.startAddress && address + PageSize - 1UL <= memoryRange.endAddress)
                    {
                        Console.WriteLine($"Position Took: {stopwatch.ElapsedMilliseconds}ms");
                        Console.WriteLine($"# Ranges: {_memory.Count}");
                        return address;
                    }
                    node = node.Next;
                }
                address += PageSize;

                ulong remainder = address % alignment;

                if (remainder != 0)
                {
                    address = (address - remainder) + alignment;
                }
            }

            Console.WriteLine($"Position Took: {stopwatch.ElapsedMilliseconds}ms");
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