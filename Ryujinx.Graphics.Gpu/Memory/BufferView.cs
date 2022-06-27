using Ryujinx.Memory.Range;
using System;

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

        public BufferView(ulong address, ulong size, int offset, bool isVirtual, Buffer buffer)
        {
            Address = address;
            Size = size;
            BaseOffset = offset;
            IsVirtual = isVirtual;
            Buffer = buffer;
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