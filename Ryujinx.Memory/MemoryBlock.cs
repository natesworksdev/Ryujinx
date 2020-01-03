using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using static Ryujinx.Memory.MemoryConstants;

namespace Ryujinx.Memory
{
    /// <summary>
    /// Represents a block of contiguous physical guest memory.
    /// </summary>
    public sealed class MemoryBlock : IDisposable
    {
        private IntPtr _pointer;

        private readonly HashSet<MemoryRange>[] _pages;

        /// <summary>
        /// Pointer to the memory block data.
        /// </summary>
        public IntPtr Pointer => _pointer;

        /// <summary>
        /// Size of the memory block.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Initializes a new instance of the memory block class.
        /// </summary>
        /// <param name="size">Size of the memory block</param>
        public MemoryBlock(ulong size)
        {
            _pointer = MemoryManagement.Allocate(size);

            Size = size;

            _pages = new HashSet<MemoryRange>[size >> PageBits];
        }

        /// <summary>
        /// Reads bytes from the memory block.
        /// </summary>
        /// <param name="address">Starting address of the range being read</param>
        /// <param name="data">Span where the bytes being read will be copied to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read(ulong address, Span<byte> data)
        {
            GetSpan(address, data.Length).CopyTo(data);
        }

        /// <summary>
        /// Reads data from the memory block.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="address">Address where the data is located</param>
        /// <returns>Data at the specified address</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(ulong address) where T : unmanaged
        {
            return GetRef<T>(address);
        }

        /// <summary>
        /// Writes bytes to the memory block.
        /// </summary>
        /// <param name="address">Starting address of the range being written</param>
        /// <param name="data">Span where the bytes being written will be copied from</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong address, ReadOnlySpan<byte> data)
        {
            data.CopyTo(GetSpan(address, data.Length));
        }

        /// <summary>
        /// Writes data to the memory block.
        /// </summary>
        /// <typeparam name="T">Type of the data being written</typeparam>
        /// <param name="address">Address to write the data into</param>
        /// <param name="data">Data to be written</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(ulong address, T data) where T : unmanaged
        {
            GetRef<T>(address) = data;
        }

        /// <summary>
        /// Copies data from one memory location to another.
        /// </summary>
        /// <param name="srcAddress">Source address to read the data from</param>
        /// <param name="dstAddress">Destination address to write the data into</param>
        /// <param name="size">Size of the copy in bytes</param>
        public void Copy(ulong srcAddress, ulong dstAddress, ulong size)
        {
            Write(dstAddress, GetSpan(srcAddress, (int)size));
        }

        /// <summary>
        /// Fills a region of memory with zeros.
        /// </summary>
        /// <param name="address">Address of the region to fill with zeros</param>
        /// <param name="size">Size in bytes of the region to fill</param>
        public void ZeroFill(ulong address, ulong size)
        {
            GetSpan(address, (int)size).Fill(0);
        }

        /// <summary>
        /// Creates a range of memory that is tracked for memory modification.
        /// The range can be checked for modification of any sub-range inside the range.
        /// </summary>
        /// <param name="address">Starting address of the range being tracked</param>
        /// <param name="size">Size in bytes of the range being tracked</param>
        /// <returns>The new write tracked memory range</returns>
        public MemoryRange CreateMemoryRange(ulong address, ulong size)
        {
            return new MemoryRange(this, address, size);
        }

        /// <summary>
        /// Gets a reference of the data at a given memory block region.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="address">Address of the memory region</param>
        /// <returns>A reference to the given memory region data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetRef<T>(ulong address) where T : unmanaged
        {
            IntPtr ptr = _pointer;

            if (ptr == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(MemoryBlock));
            }

            int size = Unsafe.SizeOf<T>();

            ulong endAddress = address + (ulong)size;

            if (endAddress > Size || endAddress < address)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return ref Unsafe.AsRef<T>((void*)PtrAddr(ptr, address));
        }

        /// <summary>
        /// Gets the span of a given memory block region.
        /// </summary>
        /// <param name="address">Start address of the memory region</param>
        /// <param name="size">Size in bytes of the region</param>
        /// <returns>Span of the memory region</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> GetSpan(ulong address, int size)
        {
            IntPtr ptr = _pointer;

            if (ptr == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(MemoryBlock));
            }

            ulong endAddress = address + (ulong)size;

            if (endAddress > Size || endAddress < address)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return new Span<byte>((void*)PtrAddr(ptr, address), size);
        }

        /// <summary>
        /// Checks if a range of pages was modified, since the last call to this method.
        /// </summary>
        /// <param name="page">Page number of the first page to be checked</param>
        /// <param name="count">Number of pages to check</param>
        /// <param name="range">Memory range being checked</param>
        /// <returns>True if any of the pages were modified, false otherwise</returns>
        internal bool QueryModified(int page, int count, MemoryRange range)
        {
            return QueryModifiedImpl(page, count, range, null);
        }

        /// <summary>
        /// Checks if a range of pages was modified, since the last call to this method.
        /// </summary>
        /// <param name="page">Page number of the first page to be checked</param>
        /// <param name="count">Number of pages to check</param>
        /// <param name="range">Memory range being checked</param>
        /// <param name="outBuffer">Optional output buffer to write the new data</param>
        /// <returns>True if any of the pages were modified, false otherwise</returns>
        internal bool QueryModified(int page, int count, MemoryRange range, Span<byte> outBuffer)
        {
            return QueryModifiedImpl(page, count, range, outBuffer);
        }

        /// <summary>
        /// Checks if a range of pages was modified, since the last call to this method.
        /// </summary>
        /// <param name="page">Page number of the first page to be checked</param>
        /// <param name="count">Number of pages to check</param>
        /// <param name="range">Memory range being checked</param>
        /// <param name="outBuffer">Optional output buffer to write the new data</param>
        /// <returns>True if any of the pages were modified, false otherwise</returns>
        private bool QueryModifiedImpl(int page, int count, MemoryRange range, Span<byte> outBuffer)
        {
            if (count <= 0)
            {
                return false;
            }

            Span<IntPtr> addresses = stackalloc IntPtr[count];

            IntPtr pagePtr = PtrAddr(_pointer, (ulong)page << PageBits);

            MemoryManagement.QueryModifiedPages(pagePtr, (IntPtr)((ulong)count << PageBits), addresses, out ulong pc);

            for (int i = 0; i < (int)pc; i++)
            {
                int modifiedPage = (int)((ulong)(addresses[i].ToInt64() - _pointer.ToInt64()) >> PageBits);

                if (outBuffer != null)
                {
                    Read((ulong)modifiedPage << PageBits, outBuffer.Slice((modifiedPage - page) << PageBits, PageSize));
                }

                _pages[modifiedPage]?.Clear();
            }

            bool modified = false;

            for (int i = 0; i < count; i++)
            {
                if (_pages[page + i] == null)
                {
                    _pages[page + i] = new HashSet<MemoryRange>();
                }

                modified |= _pages[page + i].Add(range);
            }

            return modified;
        }

        /// <summary>
        /// Adds a 64-bits offset to a native pointer.
        /// </summary>
        /// <param name="pointer">Native pointer</param>
        /// <param name="offset">Offset to add</param>
        /// <returns>Native pointer with the added offset</returns>
        private IntPtr PtrAddr(IntPtr pointer, ulong offset)
        {
            return (IntPtr)(pointer.ToInt64() + (long)offset);
        }

        /// <summary>
        /// Frees the memory allocated for this memory block.
        /// </summary>
        /// <remarks>
        /// It's an error to use the memory block after disposal.
        /// </remarks>
        public void Dispose()
        {
            IntPtr ptr = Interlocked.Exchange(ref _pointer, IntPtr.Zero);

            if (ptr != IntPtr.Zero)
            {
                MemoryManagement.Free(ptr);
            }
        }
    }
}
