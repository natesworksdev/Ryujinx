using Ryujinx.Memory.Tracking;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    struct RegionHandleSegment
    {
        public readonly ulong BaseOffset;
        public readonly ulong Size;
        public readonly IEnumerable<IRegionHandle> Handles;

        public RegionHandleSegment(ulong baseOffset, ulong size, IEnumerable<IRegionHandle> handles)
        {
            BaseOffset = baseOffset;
            Size = size;
            Handles = handles;
        }
    }
}