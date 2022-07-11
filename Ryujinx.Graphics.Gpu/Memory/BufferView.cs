using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer, used to store vertex and index data, uniform and storage buffers, and others.
    /// </summary>
    struct BufferView : IRange, IEquatable<BufferView>
    {
        /// <summary>
        /// GPU virtual address of the buffer in guest memory.
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// End address of the buffer in guest memory.
        /// </summary>
        public ulong EndAddress => Address + Size;

        /// <summary>
        /// Base offset of this view on the buffer.
        /// </summary>
        public int BaseOffset { get; }

        /// <summary>
        /// Indicates if the buffer is owned by the virtual or physical buffer cache.
        /// </summary>
        public bool IsVirtual { get; }

        /// <summary>
        /// Backing buffer.
        /// </summary>
        public Buffer Buffer { get; }

        /// <summary>
        /// Creates a new buffer view.
        /// </summary>
        /// <param name="address">Address of the buffer view</param>
        /// <param name="size">Size in bytes of the view</param>
        /// <param name="offset">Offset inside the buffer where <paramref name="address"/> starts</param>
        /// <param name="isVirtual">True if the view is owned by the virtual buffer cache, false if owned by the physical buffer cache</param>
        /// <param name="buffer">Buffer accessible by this view</param>
        public BufferView(ulong address, ulong size, int offset, bool isVirtual, Buffer buffer)
        {
            Address = address;
            Size = size;
            BaseOffset = offset;
            IsVirtual = isVirtual;
            Buffer = buffer;
        }

        /// <summary>
        /// Gets memory tracking handles for the buffer accessible by this view.
        /// </summary>
        /// <remarks>
        /// After calling this, the view becomes unusable because the tracking handles are no longer owned by the buffer.
        /// It's an error to call this method more than once.
        /// </remarks>
        /// <param name="newAddress">Offset where the inherited handles will be placed on the new buffer</param>
        /// <returns>Tracking handles used by this view</returns>
        public IEnumerable<RegionHandleSegment> GetTrackingHandles(ulong newAddress)
        {
            ulong offsetWithinNew = Address - newAddress;

            if (BaseOffset == 0 && Size == Buffer.Size)
            {
                return Buffer.GetTrackingHandles(offsetWithinNew);
            }

            IEnumerable<RegionHandleSegment> slice = Buffer.GetTrackingHandlesSlice(offsetWithinNew, (ulong)BaseOffset, Size);

            // Dispose tracking handles we are not going to inherit.
            ulong viewEndOffset = (ulong)BaseOffset + Size;

            Buffer.DisposeTrackingHandles(0, (ulong)BaseOffset);
            Buffer.DisposeTrackingHandles(viewEndOffset, Buffer.Size - viewEndOffset);

            return slice;
        }

        /// <summary>
        /// Checks if a given range overlaps with the buffer.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes of the range</param>
        /// <returns>True if the range overlaps, false otherwise</returns>
        public bool OverlapsWith(ulong address, ulong size)
        {
            return Address < address + size && address < EndAddress;
        }

        public override bool Equals(object other)
        {
            return other is BufferView view && Equals(view);
        }

        public bool Equals(BufferView other)
        {
            return Address == other.Address && Size == other.Size && BaseOffset == other.BaseOffset && Buffer == other.Buffer;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Size, BaseOffset, Buffer);
        }
    }
}