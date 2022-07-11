using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Memory range used for buffers.
    /// </summary>
    struct BufferBounds
    {
        /// <summary>
        /// GPU virtual address of the buffer binding.
        /// </summary>
        public ulong GpuVa { get; }

        /// <summary>
        /// Size of the buffer binding in bytes.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Buffer usage flags.
        /// </summary>
        public BufferUsageFlags Flags { get; }

        /// <summary>
        /// Creates a new buffer region.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the buffer binding</param>
        /// <param name="size">Size of the buffer binding in bytes</param>
        /// <param name="flags">Buffer usage flags</param>
        public BufferBounds(ulong gpuVa, ulong size, BufferUsageFlags flags = BufferUsageFlags.None)
        {
            GpuVa = gpuVa;
            Size = size;
            Flags = flags;
        }
    }
}