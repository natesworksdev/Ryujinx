using Ryujinx.Memory.Range;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A cached entry for easily locating a buffer that is used often internally.
    /// </summary>
    class BufferCacheEntry
    {
        /// <summary>
        /// Offset of the data inside the buffer.
        /// </summary>
        public int BufferOffset;

        /// <summary>
        /// The end GPU VA of the associated buffer, used to check if new data can fit.
        /// </summary>
        public ulong EndGpuAddress;

        /// <summary>
        /// Size of the data in bytes.
        /// </summary>
        public ulong Size;

        /// <summary>
        /// The buffer associated with this cache entry.
        /// </summary>
        public Buffer Buffer;

        /// <summary>
        /// The UnmappedSequence of the buffer at the time of creation.
        /// If this differs from the value currently in the buffer, then this cache entry is outdated.
        /// </summary>
        public int UnmappedSequence;

        /// <summary>
        /// Create a new cache entry.
        /// </summary>
        /// <param name="bufferOffset">Offset of the data inside the buffer</param>
        /// <param name="gpuVa">The GPU VA of the buffer destination</param>
        /// <param name="size">Size of the data in bytes</param>
        /// <param name="buffer">The buffer object containing the target buffer</param>
        public BufferCacheEntry(int bufferOffset, ulong gpuVa, ulong size, Buffer buffer)
        {
            BufferOffset = bufferOffset;
            EndGpuAddress = gpuVa + size;
            Size = size;
            Buffer = buffer;
            UnmappedSequence = buffer.UnmappedSequence;
        }
    }
}
