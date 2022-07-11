using Ryujinx.Memory.Tracking;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Represents a segment of buffer handles that are contiguous in memory and used by a buffer.
    /// </summary>
    struct RegionHandleSegment
    {
        /// <summary>
        /// Base offset of the segment inside the new owner.
        /// </summary>
        public readonly ulong BaseOffset;

        /// <summary>
        /// Size in bytes of the segment.
        /// </summary>
        public readonly ulong Size;

        /// <summary>
        /// Handles contained inside the segment.
        /// </summary>
        public readonly IEnumerable<IRegionHandle> Handles;

        /// <summary>
        /// Creates a new buffer handle segment.
        /// </summary>
        /// <param name="baseOffset">Base offset of the segment inside the new owner</param>
        /// <param name="size">Size in bytes of the segment</param>
        /// <param name="handles">Handles contained inside the segment</param>
        public RegionHandleSegment(ulong baseOffset, ulong size, IEnumerable<IRegionHandle> handles)
        {
            BaseOffset = baseOffset;
            Size = size;
            Handles = handles;
        }
    }
}