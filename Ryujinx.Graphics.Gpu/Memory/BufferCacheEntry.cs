using Ryujinx.Memory.Range;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A cached entry for easily locating a buffer that is used often internally.
    /// </summary>
    class BufferCacheEntry
    {
        /// <summary>
        /// The memory region of the buffer destination.
        /// </summary>
        public MultiRange Range;

        /// <summary>
        /// The end GPU VA of the associated buffer, used to check if new data can fit.
        /// </summary>
        public ulong EndGpuAddress;

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
        /// <param name="range">Memory region of the buffer destination</param>
        /// <param name="gpuVa">The GPU VA of the buffer destination</param>
        /// <param name="buffer">The buffer object containing the target buffer</param>
        public BufferCacheEntry(MultiRange range, ulong gpuVa, Buffer buffer)
        {
            Range = range;
            EndGpuAddress = gpuVa + range.GetSize();
            Buffer = buffer;
            UnmappedSequence = buffer.UnmappedSequence;
        }
    }
}
