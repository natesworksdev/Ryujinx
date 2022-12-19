using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Memory range used for buffers.
    /// </summary>
    readonly struct BufferBounds
    {
        /// <summary>
        /// Physical memory backing the buffer.
        /// </summary>
        public PhysicalMemory Physical { get; }

        /// <summary>
        /// Buffer cache that owns the buffer.
        /// </summary>
        public BufferCache BufferCache => Physical.BufferCache;

        /// <summary>
        /// Region virtual address.
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// Region size in bytes.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Buffer usage flags.
        /// </summary>
        public BufferUsageFlags Flags { get; }

        /// <summary>
        /// Creates a new buffer region.
        /// </summary>
        /// <param name="physical">Physical memory backing the buffer</param>
        /// <param name="address">Region address</param>
        /// <param name="size">Region size</param>
        /// <param name="flags">Buffer usage flags</param>
        public BufferBounds(PhysicalMemory physical, ulong address, ulong size, BufferUsageFlags flags = BufferUsageFlags.None)
        {
            Physical = physical;
            Address = address;
            Size = size;
            Flags = flags;
        }
    }
}
