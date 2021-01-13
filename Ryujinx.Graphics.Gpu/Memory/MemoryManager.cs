using Ryujinx.Memory;
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

        public const ulong PteUnmapped = 0xffffffff_ffffffff;

        private readonly ulong[][] _pageTable;

        public event EventHandler<UnmapEventArgs> MemoryUnmapped;

        private GpuContext _context;

        /// <summary>
        /// Creates a new instance of the GPU memory manager.
        /// </summary>
        public MemoryManager(GpuContext context)
        {
            _context = context;
            _pageTable = new ulong[PtLvl0Size][];
        }

        /// <summary>
        /// Reads data from GPU mapped memory.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="gpuVa">GPU virtual address where the data is located</param>
        /// <returns>The data at the specified memory location</returns>
        public T Read<T>(ulong gpuVa) where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(GetSpan(gpuVa, Unsafe.SizeOf<T>()))[0];
        }

        /// <summary>
        /// Gets a read-only span of data from GPU mapped memory.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address where the data is located</param>
        /// <param name="size">Size of the data</param>
        /// <returns>The span of the data at the specified memory location</returns>
        public ReadOnlySpan<byte> GetSpan(ulong gpuVa, int size, bool tracked = false)
        {
            ulong processVa = Translate(gpuVa);

            if (IsContiguous(gpuVa, size))
            {
                return _context.PhysicalMemory.GetSpan(processVa, size, tracked);
            }
            else
            {
                Span<byte> data = new byte[size];

                ReadImpl(gpuVa, data, tracked);

                return data;
            }
        }

        /// <summary>
        /// Reads data from a possibly non-contiguous region of GPU mapped memory.
        /// </summary>
        /// <param name="va">GPU virtual address of the data</param>
        /// <param name="data">Span to write the read data into</param>
        /// <param name="tracked">True to enable write tracking on read, false otherwise</param>
        private void ReadImpl(ulong va, Span<byte> data, bool tracked)
        {
            if (data.Length == 0)
            {
                return;
            }

            int offset = 0, size;

            if ((va & PageMask) != 0)
            {
                ulong pa = Translate(va);

                if (pa == PteUnmapped)
                {
                    return;
                }

                size = Math.Min(data.Length, (int)PageSize - (int)(va & PageMask));

                _context.PhysicalMemory.GetSpan(pa, size, tracked).CopyTo(data.Slice(0, size));

                offset += size;
            }

            for (; offset < data.Length; offset += size)
            {
                ulong pa = Translate(va + (ulong)offset);

                if (pa == PteUnmapped)
                {
                    break;
                }

                size = Math.Min(data.Length - offset, (int)PageSize);

                _context.PhysicalMemory.GetSpan(pa, size, tracked).CopyTo(data.Slice(offset, size));
            }
        }

        /// <summary>
        /// Gets a writable region from GPU mapped memory.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes to be range</param>
        /// <returns>A writable region with the data at the specified memory location</returns>
        public WritableRegion GetWritableRegion(ulong gpuVa, int size)
        {
            if (IsContiguous(gpuVa, size))
            {
                ulong processVa = Translate(gpuVa);

                return _context.PhysicalMemory.GetWritableRegion(processVa, size);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Writes data to GPU mapped memory.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="gpuVa">GPU virtual address to write the value into</param>
        /// <param name="value">The value to be written</param>
        public void Write<T>(ulong gpuVa, T value) where T : unmanaged
        {
            Write(gpuVa, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)));
        }

        /// <summary>
        /// Writes data to GPU mapped memory.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void Write(ulong gpuVa, ReadOnlySpan<byte> data)
        {
            WriteImpl(gpuVa, data, _context.PhysicalMemory.Write);
        }

        /// <summary>
        /// Writes data to GPU mapped memory without write tracking.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void WriteUntracked(ulong gpuVa, ReadOnlySpan<byte> data)
        {
            WriteImpl(gpuVa, data, _context.PhysicalMemory.WriteUntracked);
        }

        private delegate void WriteCallback(ulong address, ReadOnlySpan<byte> data);

        /// <summary>
        /// Writes data to possibly non-contiguous GPU mapped memory.
        /// </summary>
        /// <param name="va">GPU virtual address of the region to write into</param>
        /// <param name="data">Data to be written</param>
        /// <param name="writeCallback">Write callback</param>
        private void WriteImpl(ulong va, ReadOnlySpan<byte> data, WriteCallback writeCallback)
        {
            if (IsContiguous(va, data.Length))
            {
                ulong processVa = Translate(va);

                writeCallback(processVa, data);
            }
            else
            {
                int offset = 0, size;

                if ((va & PageMask) != 0)
                {
                    ulong pa = Translate(va);

                    if (pa == PteUnmapped)
                    {
                        return;
                    }

                    size = Math.Min(data.Length, (int)PageSize - (int)(va & PageMask));

                    writeCallback(pa, data.Slice(0, size));

                    offset += size;
                }

                for (; offset < data.Length; offset += size)
                {
                    ulong pa = Translate(va + (ulong)offset);

                    if (pa == PteUnmapped)
                    {
                        break;
                    }

                    size = Math.Min(data.Length - offset, (int)PageSize);

                    writeCallback(pa, data.Slice(offset, size));
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
        public void Map(ulong pa, ulong va, ulong size)
        {
            lock (_pageTable)
            {
                MemoryUnmapped?.Invoke(this, new UnmapEventArgs(va, size));

                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, pa + offset);
                }
            }
        }

        /// <summary>
        /// Unmaps a given range of pages at the specified GPU virtual memory region.
        /// </summary>
        /// <param name="va">GPU virtual address to unmap</param>
        /// <param name="size">Size in bytes of the region being unmapped</param>
        public void Unmap(ulong va, ulong size)
        {
            lock (_pageTable)
            {
                // Event handlers are not expected to be thread safe.
                MemoryUnmapped?.Invoke(this, new UnmapEventArgs(va, size));

                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, PteUnmapped);
                }
            }
        }

        /// <summary>
        /// Checks if a region of GPU mapped memory is contiguous.
        /// </summary>
        /// <param name="va">GPU virtual address of the region</param>
        /// <param name="size">Size of the region</param>
        /// <returns>True if the region is contiguous, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsContiguous(ulong va, int size)
        {
            if (!ValidateAddress(va) || GetPte(va) == PteUnmapped)
            {
                return false;
            }

            ulong endVa = (va + (ulong)size + PageMask) & ~(ulong)PageMask;

            va &= ~(ulong)PageMask;

            int pages = (int)((endVa - va) / PageSize);

            for (int page = 0; page < pages - 1; page++)
            {
                if (!ValidateAddress(va + PageSize)|| GetPte(va + PageSize) == PteUnmapped)
                {
                    return false;
                }

                if (Translate(va) + PageSize != Translate(va + PageSize))
                {
                    return false;
                }

                va += PageSize;
            }

            return true;
        }

        /// <summary>
        /// Validates a GPU virtual address.
        /// </summary>
        /// <param name="va">Address to validate</param>
        /// <returns>True if the address is valid, false otherwise</returns>
        private static bool ValidateAddress(ulong va)
        {
            return va < (1UL << 40);
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

            if (baseAddress == PteUnmapped)
            {
                return PteUnmapped;
            }

            return baseAddress + (gpuVa & PageMask);
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