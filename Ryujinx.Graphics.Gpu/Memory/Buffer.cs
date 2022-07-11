using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;
using Ryujinx.Memory.Tracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer, used to store vertex and index data, uniform and storage buffers, and others.
    /// </summary>
    class Buffer : IDisposable
    {
        private readonly GpuContext _context;
        private readonly BufferRegion[] _regions;

        /// <summary>
        /// Host buffer handle.
        /// </summary>
        public BufferHandle Handle { get; }

        /// <summary>
        /// Ranges of memory where the buffer data resides.
        /// </summary>
        public MultiRange Range { get; }

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Increments when the buffer is (partially) unmapped or disposed.
        /// </summary>
        public int UnmappedSequence { get; private set; }

        /// <summary>
        /// Indicates if this buffer is accessible by any buffer view.
        /// </summary>
        public bool HasViews => _views.Count != 0;

        /// <summary>
        /// Buffer views that can access this buffer.
        /// </summary>
        private readonly List<(RangeList<BufferView>, BufferView)> _views;

        /// <summary>
        /// Creates a new instance of the buffer.
        /// </summary>
        /// <param name="context">GPU context that the buffer belongs to</param>
        /// <param name="physicalMemory">Physical memory where the buffer is mapped</param>
        /// <param name="range">Range of memory where the buffer data is located</param>
        /// <param name="baseHandles">Tracking handles to be inherited by this buffer</param>
        public Buffer(GpuContext context, PhysicalMemory physicalMemory, MultiRange range, IEnumerable<RegionHandleSegment> baseHandles = null)
        {
            _context = context;
            Range = range;
            Size = range.GetSize();

            Handle = context.Renderer.CreateBuffer((int)Size);

            BufferRegion[] regions = new BufferRegion[range.Count];
            int regionsCount = 0;
            ulong baseOffset = 0;

            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    IEnumerable<IRegionHandle> handles = null;

                    if (baseHandles != null)
                    {
                        handles = baseHandles
                            .Where(x => x.BaseOffset >= baseOffset && x.BaseOffset + x.Size <= baseOffset + subRange.Size)
                            .SelectMany(x => x.Handles);
                    }

                    regions[regionsCount++] = new BufferRegion(
                        context,
                        physicalMemory,
                        subRange.Address,
                        subRange.Size,
                        Handle,
                        baseOffset,
                        handles);
                }

                baseOffset += subRange.Size;
            }

            if (range.Count != regionsCount)
            {
                Array.Resize(ref regions, regionsCount);
            }

            _regions = regions;

            _views = new List<(RangeList<BufferView>, BufferView)>();
        }

        /// <summary>
        /// Adds a view to the list of views that can access this buffer.
        /// </summary>
        /// <param name="list">List of views from the buffer cache where the view was added</param>
        /// <param name="view">Buffer view that can access this buffer</param>
        public void AddView(RangeList<BufferView> list, BufferView view)
        {
            _views.Add((list, view));
        }

        /// <summary>
        /// Removes a view from the list of views that can access this buffer.
        /// </summary>
        /// <param name="list">List of views from the buffer cache where the view was added</param>
        /// <param name="view">Buffer view that can access this buffer</param>
        public void RemoveView(RangeList<BufferView> list, BufferView view)
        {
            _views.Remove((list, view));
        }

        /// <summary>
        /// Migrates the views that can access this buffer to a new buffer.
        /// </summary>
        /// <param name="newBuffer">Buffer where the views should be migrated to</param>
        /// <param name="offsetDelta">Delta between the start address of this buffer and the new one</param>
        public void UpdateViews(Buffer newBuffer, int offsetDelta)
        {
            foreach ((RangeList<BufferView> list, BufferView view) in _views)
            {
                BufferView newView = new BufferView(view.Address, view.Size, view.BaseOffset + offsetDelta, view.IsVirtual, newBuffer);

                lock (list)
                {
                    list.Remove(view);
                    list.Add(newView);
                }

                newBuffer.AddView(list, newView);
            }

            _views.Clear();
        }

        /// <summary>
        /// Performs guest to host memory synchronization of the buffer data.
        /// </summary>
        /// <remarks>
        /// This causes the buffer data to be overwritten if a write was detected from the CPU,
        /// since the last call to this method.
        /// </remarks>
        /// <param name="bufferOffset">Offset of the region inside the buffer</param>
        /// <param name="size">Size of the region in bytes</param>
        public void SynchronizeMemory(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                (ulong currentAddress, ulong currentSize) = GetRegionAddressAndSize(region, bufferOffset, bufferEndOffset);

                region.SynchronizeMemory(currentAddress, currentSize);
            }
        }

        /// <summary>
        /// Signal that the given region of the buffer has been modified.
        /// </summary>
        /// <param name="bufferOffset">Offset of the region inside the buffer</param>
        /// <param name="size">Size of the region in bytes</param>
        public void SignalModified(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                (ulong currentAddress, ulong currentSize) = GetRegionAddressAndSize(region, bufferOffset, bufferEndOffset);

                region.SignalModified(currentAddress, currentSize);
            }
        }

        /// <summary>
        /// Indicate that mofifications in a given region of this buffer have been overwritten.
        /// </summary>
        /// <param name="bufferOffset">Offset of the region inside the buffer</param>
        /// <param name="size">Size of the region in bytes</param>
        public void ClearModified(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                (ulong currentAddress, ulong currentSize) = GetRegionAddressAndSize(region, bufferOffset, bufferEndOffset);

                region.ClearModified(currentAddress, currentSize);
            }
        }

        /// <summary>
        /// Inherit modified ranges from another buffer.
        /// </summary>
        /// <param name="from">The buffer to inherit from</param>
        /// <param name="offsetWithinFrom">Offset of the from buffer inside this buffer</param>
        public void InheritModifiedRanges(Buffer from, ulong offsetWithinFrom)
        {
            int startIndex = FindStartIndex(offsetWithinFrom);

            for (int i = startIndex, j = 0; i < _regions.Length && j < from._regions.Length; i++)
            {
                ulong thisOffset = _regions[i].BaseOffset;
                ulong thisEndOffset = thisOffset + _regions[i].Size;
                ulong fromOffset = offsetWithinFrom + from._regions[j].BaseOffset;
                ulong fromEndOffset = fromOffset + from._regions[j].Size;

                if (thisOffset < fromEndOffset && fromOffset < thisEndOffset)
                {
                    _regions[i].InheritModifiedRanges(from._regions[j++], bounded: false);
                }
            }
        }

        /// <summary>
        /// Inherit modified ranges from another buffer.
        /// </summary>
        /// <param name="from">The buffer to inherit from</param>
        /// <param name="offsetWithinFrom">Offset of this buffer inside the from buffer</param>
        public void InheritModifiedRangesForSplit(Buffer from, ulong offsetWithinFrom)
        {
            int startIndex = from.FindStartIndex(offsetWithinFrom);

            for (int i = 0, j = startIndex; i < _regions.Length && j < from._regions.Length; i++)
            {
                ulong thisOffset = offsetWithinFrom + _regions[i].BaseOffset;
                ulong thisEndOffset = thisOffset + _regions[i].Size;
                ulong fromOffset = from._regions[j].BaseOffset;
                ulong fromEndOffset = fromOffset + from._regions[j].Size;

                if (thisOffset < fromEndOffset && fromOffset < thisEndOffset)
                {
                    _regions[i].InheritModifiedRanges(from._regions[j++], bounded: true);
                }
            }
        }

        /// <summary>
        /// Determine if a given region of the buffer has been modified, and must be flushed.
        /// </summary>
        /// <param name="bufferOffset">Offset of the region inside the buffer</param>
        /// <param name="size">Size of the region in bytes</param>
        /// <returns>True if the region has been modified, false otherwise</returns>
        public bool IsModified(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                (ulong currentAddress, ulong currentSize) = GetRegionAddressAndSize(region, bufferOffset, bufferEndOffset);

                if (region.IsModified(currentAddress, currentSize))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Force a region of the buffer to be dirty. Avoids reprotection and nullifies sequence number check.
        /// </summary>
        /// <param name="bufferOffset">Offset of the region inside the buffer</param>
        /// <param name="size">Size of the region in bytes</param>
        public void ForceDirty(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                (ulong currentAddress, ulong currentSize) = GetRegionAddressAndSize(region, bufferOffset, bufferEndOffset);

                region.ForceDirty(currentAddress, currentSize);
            }
        }

        /// <summary>
        /// Performs copy of all the buffer data from one buffer to another.
        /// </summary>
        /// <param name="destination">The destination buffer to copy the data into</param>
        /// <param name="dstOffset">The offset of the destination buffer to copy into</param>
        public void CopyTo(Buffer destination, int dstOffset)
        {
            CopyTo(destination, 0, dstOffset, (int)Size);
        }

        /// <summary>
        /// Performs copy of buffer data on the specified region from one buffer to another.
        /// </summary>
        /// <param name="destination">The destination buffer to copy the data into</param>
        /// <param name="srcOffset">The offset of the source buffer to copy from</param>
        /// <param name="dstOffset">The offset of the destination buffer to copy into</param>
        /// <param name="size">Number of bytes to copy</param>
        public void CopyTo(Buffer destination, int srcOffset, int dstOffset, int size)
        {
            _context.Renderer.Pipeline.CopyBuffer(Handle, destination.Handle, srcOffset, dstOffset, size);
        }

        /// <summary>
        /// Called when part of the memory for this buffer has been unmapped.
        /// Calls are from non-GPU threads.
        /// </summary>
        /// <param name="bufferOffset">Offset of the region inside the buffer</param>
        /// <param name="size">Size of the region in bytes</param>
        public void Unmapped(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                (ulong currentAddress, ulong currentSize) = GetRegionAddressAndSize(region, bufferOffset, bufferEndOffset);

                region.Unmapped(currentAddress, currentSize);
            }

            UnmappedSequence++;
        }

        /// <summary>
        /// Finds the index of the first buffer that ends after <paramref name="bufferOffset"/> in the regions array.
        /// </summary>
        /// <param name="bufferOffset">Offset to find a overlap for</param>
        /// <returns>Index of the first overlap, or the array length if none is found</returns>
        private int FindStartIndex(ulong bufferOffset)
        {
            int index;

            for (index = 0; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset + region.Size > bufferOffset)
                {
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Gets the clamped buffer CPU virtual address and size from a buffer offset and size.
        /// </summary>
        /// <param name="region">Buffer that contains the specified range</param>
        /// <param name="bufferOffset">Start offset into the buffer in bytes</param>
        /// <param name="bufferEndOffset">End offset into the buffer in bytes</param>
        /// <returns>The clamped CPU virtual address and size that is fully contained inside <paramref name="region"/></returns>
        private static (ulong, ulong) GetRegionAddressAndSize(BufferRegion region, ulong bufferOffset, ulong bufferEndOffset)
        {
            ulong clampedOffset = Math.Max(region.BaseOffset, bufferOffset);
            ulong clampedEndOffset = Math.Min(region.BaseOffset + region.Size, bufferEndOffset);
            ulong clampedSize = clampedEndOffset - clampedOffset;

            return (region.Address + (clampedOffset - region.BaseOffset), clampedSize);
        }

        /// <summary>
        /// Gets all the tracking handles used by this buffer.
        /// </summary>
        /// <param name="baseOffset">Offset of this buffer inside the new buffer that will inherit the tracking handles</param>
        /// <returns>Tracking handle segments</returns>
        public IEnumerable<RegionHandleSegment> GetTrackingHandles(ulong baseOffset)
        {
            if (_regions.Length == 1)
            {
                return Enumerable.Repeat(new RegionHandleSegment(baseOffset, _regions[0].Size, _regions[0].GetTrackingHandles()), 1);
            }

            RegionHandleSegment[] handles = new RegionHandleSegment[_regions.Length];

            for (int index = 0; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                handles[index] = new RegionHandleSegment(baseOffset + region.BaseOffset, region.Size, region.GetTrackingHandles());
            }

            return handles;
        }

        /// <summary>
        /// Gets the tracking handles on a given sub-range of this buffer.
        /// </summary>
        /// <param name="baseOffset">Offset where the handles will be placed on the new buffer that will inherit them</param>
        /// <param name="bufferOffset">Start offset of the range to get the tracking handles from</param>
        /// <param name="size">Size in bytes of the range</param>
        /// <returns>The tracking handles at the specified range</returns>
        public IEnumerable<RegionHandleSegment> GetTrackingHandlesSlice(ulong baseOffset, ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            List<RegionHandleSegment> handles = new List<RegionHandleSegment>();

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                ulong regionEndOffset = region.BaseOffset + region.Size;

                ulong clampedOffset = Math.Max(bufferOffset, region.BaseOffset);
                ulong clampedEndOffset = Math.Min(bufferEndOffset, regionEndOffset);
                ulong clampedSize = clampedEndOffset - clampedOffset;

                IEnumerable<IRegionHandle> regionHandles = region.GetTrackingHandles();

                if (clampedOffset > region.BaseOffset || clampedEndOffset < regionEndOffset)
                {
                    ulong skipSize = clampedOffset - region.BaseOffset;

                    int skipCount = (int)(skipSize / BufferRegion.GranularBufferThreshold);
                    int takeCount = (int)(clampedSize / BufferRegion.GranularBufferThreshold);

                    regionHandles = regionHandles.Skip(skipCount).Take(takeCount);
                }

                handles.Add(new RegionHandleSegment(baseOffset + (clampedOffset - bufferOffset), clampedSize, regionHandles));
            }

            return handles;
        }

        /// <summary>
        /// Disposes the tracking handles at a specified buffer sub-range.
        /// </summary>
        /// <remarks>
        /// This buffer should no longer be used after disposing tracking handles.
        /// It also can't be disposed using the regular Dispose method, instead one must use <see cref="DisposeData"/>
        /// to dispose of the buffer storage, and this method to dispose of any other remaining tracking handles.
        /// </remarks>
        /// <param name="bufferOffset">Start offset of the range to have its tracking handles disposed</param>
        /// <param name="size">Size in bytes of the range</param>
        public void DisposeTrackingHandles(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                ulong regionEndOffset = region.BaseOffset + region.Size;

                ulong clampedOffset = Math.Max(bufferOffset, region.BaseOffset);
                ulong clampedEndOffset = Math.Min(bufferEndOffset, regionEndOffset);
                ulong clampedSize = clampedEndOffset - clampedOffset;

                IEnumerable<IRegionHandle> regionHandles = region.GetTrackingHandles();

                if (clampedOffset > region.BaseOffset || clampedEndOffset < regionEndOffset)
                {
                    ulong skipSize = clampedOffset - region.BaseOffset;

                    int skipCount = (int)(skipSize / BufferRegion.GranularBufferThreshold);
                    int takeCount = (int)(clampedSize / BufferRegion.GranularBufferThreshold);

                    foreach (IRegionHandle handle in regionHandles)
                    {
                        if (skipCount > 0)
                        {
                            skipCount--;
                        }
                        else if (takeCount > 0)
                        {
                            takeCount--;
                            handle.Dispose();
                        }
                        else
                        {
                            break;
                        }
                    }

                    Debug.Assert(skipCount == 0);
                    Debug.Assert(takeCount == 0);
                }
                else
                {
                    foreach (IRegionHandle handle in regionHandles)
                    {
                        handle.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the host buffer's data, not its tracking handles.
        /// </summary>
        public void DisposeData()
        {
            foreach (BufferRegion region in _regions)
            {
                region.ClearModified();
            }

            _context.Renderer.DeleteBuffer(Handle);

            UnmappedSequence++;
        }

        /// <summary>
        /// Disposes the host buffer.
        /// </summary>
        public void Dispose()
        {
            foreach (BufferRegion region in _regions)
            {
                region.Dispose();
            }

            _context.Renderer.DeleteBuffer(Handle);

            UnmappedSequence++;
        }
    }
}