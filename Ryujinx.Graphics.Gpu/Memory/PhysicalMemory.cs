using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    using CpuMemoryManager = ARMeilleure.Memory.MemoryManager;

    /// <summary>
    /// Represents physical memory, accessible from the GPU.
    /// This is actually working CPU virtual addresses, of memory mapped on the application process.
    /// </summary>
    class PhysicalMemory
    {
        public const int PageSize = CpuMemoryManager.PageSize;

        private readonly CpuMemoryManager _cpuMemory;

        /// <summary>
        /// Creates a new instance of the physical memory.
        /// </summary>
        /// <param name="cpuMemory">CPU memory manager of the application process</param>
        public PhysicalMemory(CpuMemoryManager cpuMemory, MemoryBlock backingMemory)
        {
            _cpuMemory = cpuMemory;
        }

        /// <summary>
        /// Gets a span of data from the application process.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes to be range</param>
        /// <returns>A read only span of the data at the specified memory location</returns>
        public ReadOnlySpan<byte> GetSpan(ulong address, ulong size)
        {
            return _cpuMemory.GetSpan(address, (int)size);
        }

        /// <summary>
        /// Writes data to the application process.
        /// </summary>
        /// <param name="address">Address to write into</param>
        /// <param name="data">Data to be written</param>
        public void Write(ulong address, ReadOnlySpan<byte> data)
        {
            _cpuMemory.Write(address, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int QueryModified(ulong address, ulong size, ResourceName name, (ulong, ulong)[] modifiedRanges = null)
        {
            return _cpuMemory.QueryModified(address, size, (int)name, modifiedRanges);
        }
    }
}