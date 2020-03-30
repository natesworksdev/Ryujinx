using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ryujinx.Memory
{
    /// <summary>
    /// Represents a block of contiguous physical guest memory.
    /// </summary>
    public sealed class MemoryBlock : IDisposable
    {
        private IntPtr _pointer;

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
        }

        /// <summary>
        /// Reads bytes from the memory block.
        /// </summary>
        /// <param name="offset">Starting offset of the range being read</param>
        /// <param name="data">Span where the bytes being read will be copied to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read(ulong offset, Span<byte> data)
        {
            GetSpan(offset, data.Length).CopyTo(data);
        }

        /// <summary>
        /// Reads data from the memory block.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="offset">Offset where the data is located</param>
        /// <returns>Data at the specified address</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(ulong offset) where T : unmanaged
        {
            return GetRef<T>(offset);
        }

        /// <summary>
        /// Writes bytes to the memory block.
        /// </summary>
        /// <param name="offset">Starting offset of the range being written</param>
        /// <param name="data">Span where the bytes being written will be copied from</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong offset, ReadOnlySpan<byte> data)
        {
            data.CopyTo(GetSpan(offset, data.Length));
        }

        /// <summary>
        /// Writes data to the memory block.
        /// </summary>
        /// <typeparam name="T">Type of the data being written</typeparam>
        /// <param name="offset">Offset to write the data into</param>
        /// <param name="data">Data to be written</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(ulong offset, T data) where T : unmanaged
        {
            GetRef<T>(offset) = data;
        }

        /// <summary>
        /// Copies data from one memory location to another.
        /// </summary>
        /// <param name="srcOffset">Source offset to read the data from</param>
        /// <param name="dstOffset">Destination offset to write the data into</param>
        /// <param name="size">Size of the copy in bytes</param>
        public void Copy(ulong srcOffset, ulong dstOffset, ulong size)
        {
            const int MaxChunckSize = 1 << 30;
            
            for (ulong offset = 0; offset < size; offset += MaxChunckSize)
            {
                int copySize = (int)Math.Min(MaxChunckSize, size - offset);

                Write(dstOffset + offset, GetSpan(srcOffset + offset, copySize));
            }   
        }

        /// <summary>
        /// Fills a region of memory with zeros.
        /// </summary>
        /// <param name="offset">Offset of the region to fill with zeros</param>
        /// <param name="size">Size in bytes of the region to fill</param>
        public void ZeroFill(ulong offset, ulong size)
        {
            const int MaxChunckSize = 1 << 30;

            for (ulong subOffset = 0; subOffset < size; subOffset += MaxChunckSize)
            {
                int copySize = (int)Math.Min(MaxChunckSize, size - subOffset);

                GetSpan(offset + subOffset, copySize).Fill(0);
            }
        }

        /// <summary>
        /// Gets a reference of the data at a given memory block region.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Offset of the memory region</param>
        /// <returns>A reference to the given memory region data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetRef<T>(ulong offset) where T : unmanaged
        {
            IntPtr ptr = _pointer;

            if (ptr == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(MemoryBlock));
            }

            int size = Unsafe.SizeOf<T>();

            ulong endAddress = offset + (ulong)size;

            if (endAddress > Size || endAddress < offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return ref Unsafe.AsRef<T>((void*)PtrAddr(ptr, offset));
        }

        /// <summary>
        /// Gets the pointer of a given memory block region.
        /// </summary>
        /// <param name="offset">Start offset of the memory region</param>
        /// <param name="size">Size in bytes of the region</param>
        /// <returns>The pointer to the memory region</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetPointer(ulong offset, int size)
        {
            IntPtr ptr = _pointer;

            if (ptr == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(MemoryBlock));
            }

            ulong endAddress = offset + (ulong)size;

            if (endAddress > Size || endAddress < offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return PtrAddr(ptr, offset);
        }

        /// <summary>
        /// Gets the span of a given memory block region.
        /// </summary>
        /// <param name="offset">Start offset of the memory region</param>
        /// <param name="size">Size in bytes of the region</param>
        /// <returns>Span of the memory region</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> GetSpan(ulong offset, int size)
        {
            return new Span<byte>((void*)GetPointer(offset, size), size);
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
