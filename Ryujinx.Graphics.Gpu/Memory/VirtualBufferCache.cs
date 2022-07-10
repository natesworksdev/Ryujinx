using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer cache that manages buffers with GPU virtual memory regions.
    /// </summary>
    class VirtualBufferCache
    {
        private const int OverlapsBufferInitialCapacity = 10;
        private const int OverlapsBufferMaxCapacity = 10000;

        private const ulong BufferAlignmentSize = 0x1000;
        private const ulong BufferAlignmentMask = BufferAlignmentSize - 1;

        private const ulong MaxDynamicGrowthSize = 0x100000;

        public int MappingUpdates { get; private set; }

        private readonly GpuContext _context;
        private readonly MemoryManager _memoryManager;
        private readonly BufferCache _bufferCache;

        private readonly RangeList<BufferView> _buffers;

        private BufferView[] _viewOverlaps;

        private readonly Dictionary<ulong, BufferCacheEntry> _dirtyCache;
        private readonly ConcurrentQueue<UnmapEventArgs> _pendingUnmaps;

        /// <summary>
        /// Creates a new instance of the buffer manager.
        /// </summary>
        /// <param name="context">The GPU context that the buffer manager belongs to</param>
        /// <param name="memoryManager">GPU virtual memory manager</param>
        public VirtualBufferCache(GpuContext context, MemoryManager memoryManager)
        {
            _context = context;
            _memoryManager = memoryManager;
            _bufferCache = memoryManager.Physical.BufferCache;

            _buffers = new RangeList<BufferView>();

            _viewOverlaps = new BufferView[OverlapsBufferInitialCapacity];

            _dirtyCache = new Dictionary<ulong, BufferCacheEntry>();
            _pendingUnmaps = new ConcurrentQueue<UnmapEventArgs>();
        }

        /// <summary>
        /// Handles removal of buffers written to a memory region being unmapped.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void MemoryUnmappedHandler(object sender, UnmapEventArgs e)
        {
            BufferView[] overlaps = new BufferView[10];
            int overlapCount;

            lock (_buffers)
            {
                overlapCount = _buffers.FindOverlapsNonOverlapping(e.Address, e.Size, ref overlaps);
            }

            for (int i = 0; i < overlapCount; i++)
            {
                BufferView view = overlaps[i];
                ulong clampedAddress = Math.Max(view.Address, e.Address);
                ulong clampedEndAddress = Math.Min(view.EndAddress, e.Address + e.Size);
                ulong clampedSize = clampedEndAddress - clampedAddress;

                if (clampedSize != 0)
                {
                    view.Buffer.Unmapped((ulong)view.BaseOffset + (clampedAddress - view.Address), clampedSize);
                }
            }

            _pendingUnmaps.Enqueue(e);
        }

        /// <summary>
        /// Updates the internal mappings of the cache, removing buffer ranges that have been unmapped.
        /// </summary>
        /// <remarks>
        /// This must be called before getting buffers from the cache for a GPU operation.
        /// </remarks>
        public void RefreshMappings()
        {
            bool updated = false;

            while (_pendingUnmaps.TryDequeue(out UnmapEventArgs unmapRegion))
            {
                BufferView[] overlaps = _viewOverlaps;
                int overlapCount;

                lock (_buffers)
                {
                    overlapCount = _buffers.FindOverlapsNonOverlapping(unmapRegion.Address, unmapRegion.Size, ref overlaps);
                }

                for (int i = 0; i < overlapCount; i++)
                {
                    RemoveRange(ref overlaps[i], unmapRegion.Address, unmapRegion.Size);
                }

                updated |= overlapCount != 0;
            }

            if (updated)
            {
                MappingUpdates++;

                _bufferCache.ForceBindingsUpdate();
            }
        }

        /// <summary>
        /// Remove a sub-range of a buffer on the cache.
        /// </summary>
        /// <param name="view">View to have a sub-range removed</param>
        /// <param name="gpuVa">GPU virtual address of the sub-range to remove</param>
        /// <param name="size">Size of the sub-range to remove in bytes</param>
        private void RemoveRange(ref BufferView view, ulong gpuVa, ulong size)
        {
            ulong clampedAddress = Math.Max(view.Address, gpuVa);
            ulong clampedEndAddress = Math.Min(view.EndAddress, gpuVa + size);

            if (clampedAddress >= clampedEndAddress)
            {
                // Nothing to remove.
                return;
            }

            RemoveView(ref view);

            if (clampedAddress > view.Address)
            {
                SplitAndAdd(ref view, view.Address, clampedAddress - view.Address);
            }

            if (clampedEndAddress < view.EndAddress)
            {
                SplitAndAdd(ref view, clampedEndAddress, view.EndAddress - clampedEndAddress);
            }

            // Dispose handles of the unmapped region if the buffer is going to be deleted.
            if (view.IsVirtual)
            {
                Buffer oldBuffer = view.Buffer;
                ulong clampedSize = clampedEndAddress - clampedAddress;
                oldBuffer.DisposeTrackingHandles(clampedAddress - view.Address, clampedSize);
            }

            // If we are doing a partial unmap, the tracking handles will be inherited by the new buffer(s).
            DeleteViewBuffer(ref view, dataOnly: view.IsVirtual);
        }

        /// <summary>
        /// Splits a given sub-range of a buffer view, and adds it to the cache.
        /// </summary>
        /// <param name="viewToSplit">Buffer view to be split</param>
        /// <param name="splitAddress">GPU virtual address of the split sub-range</param>
        /// <param name="splitSize">Size of the split sub-range in bytes</param>
        private void SplitAndAdd(ref BufferView viewToSplit, ulong splitAddress, ulong splitSize)
        {
            ulong splitRangeOffset = (ulong)viewToSplit.BaseOffset + (splitAddress - viewToSplit.Address);
            MultiRange splitRange = viewToSplit.Buffer.Range.Slice(splitRangeOffset, splitSize);

            if (IsFullyUnmapped(splitRange))
            {
                // No need to dispose tracking handles as unmapped ranges don't have any tracking handle.

                return;
            }

            BufferView newView;

            if (viewToSplit.IsVirtual)
            {
                // If this is using a virtual buffer, first we try to use a physical one instead if possible.
                Buffer physicalBuffer = null;
                Buffer oldBuffer = viewToSplit.Buffer;

                var baseHandles = oldBuffer.GetTrackingHandlesSlice(0, splitRangeOffset, splitSize);

                if (splitRange.Count == 1)
                {
                    MemoryRange subRange = splitRange.GetSubRange(0);
                    physicalBuffer = _bufferCache.TryCreateBuffer(subRange.Address, subRange.Size, baseHandles);
                }

                Buffer newBuffer = physicalBuffer ?? new Buffer(_context, _memoryManager.Physical, splitRange, baseHandles);

                newView = new BufferView(splitAddress, splitSize, 0, isVirtual: physicalBuffer == null, newBuffer);
                newBuffer.AddView(_buffers, newView);

                ulong offsetWithinOld = splitAddress - viewToSplit.Address;

                oldBuffer.CopyTo(newBuffer, (int)offsetWithinOld, 0, (int)splitSize);
                newBuffer.InheritModifiedRangesForSplit(oldBuffer, offsetWithinOld);
            }
            else
            {
                // If this is using a physical buffer, let's just keep it and adjust the offset and size.
                int newOffset = viewToSplit.BaseOffset + (int)(splitAddress - viewToSplit.Address);
                newView = new BufferView(splitAddress, splitSize, newOffset, isVirtual: false, viewToSplit.Buffer);
                viewToSplit.Buffer.AddView(_buffers, newView);
            }

            lock (_buffers)
            {
                _buffers.Add(newView);
            }
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and attempts to force
        /// the buffer in the region as dirty.
        /// The buffer lookup for this function is cached in a dictionary for quick access, which
        /// accelerates common UBO updates.
        /// </summary>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        public void ForceDirty(ulong gpuVa, ulong size)
        {
            if (!_dirtyCache.TryGetValue(gpuVa, out BufferCacheEntry result) ||
                result.EndGpuAddress < gpuVa + size ||
                result.UnmappedSequence != result.Buffer.UnmappedSequence)
            {
                CreateBuffer(gpuVa, size);
                Buffer buffer = GetBuffer(gpuVa, size, write: false, out int bufferOffset);
                if (buffer == null)
                {
                    return;
                }

                result = new BufferCacheEntry(bufferOffset, gpuVa, size, buffer);

                _dirtyCache[gpuVa] = result;
            }

            result.Buffer.ForceDirty((ulong)result.BufferOffset, result.Size);
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if it does not yet exist.
        /// This can be used to ensure the existence of a buffer.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the buffer in memory</param>
        /// <param name="size">Size of the buffer in bytes</param>
        public void CreateBuffer(ulong gpuVa, ulong size)
        {
            ulong endAddress = gpuVa + size;
            ulong alignedGpuVa = gpuVa & ~BufferAlignmentMask;
            ulong alignedEndAddress = (endAddress + BufferAlignmentMask) & ~BufferAlignmentMask;

            if (alignedGpuVa == alignedEndAddress)
            {
                // Buffer size is zero.
                return;
            }

            CreateBufferAligned(alignedGpuVa, alignedEndAddress - alignedGpuVa);
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        private void CreateBufferAligned(ulong gpuVa, ulong size)
        {
            BufferView[] overlaps = _viewOverlaps;
            int overlapsCount;

            lock (_buffers)
            {
                overlapsCount = _buffers.FindOverlapsNonOverlapping(gpuVa, size, ref overlaps);
            }

            bool anyVirtualOverlap = false;

            if (overlapsCount != 0)
            {
                // The buffer already exists. We can just return the existing buffer
                // if the buffer we need is fully contained inside the overlapping buffer.
                // Otherwise, we must delete the overlapping buffers and create a bigger buffer
                // that fits all the data we need. We also need to copy the contents from the
                // old buffer(s) to the new buffer.

                ulong endAddress = gpuVa + size;

                if (overlaps[0].Address <= gpuVa && overlaps[0].EndAddress >= endAddress)
                {
                    return;
                }

                // Check if the following conditions are met:
                // - We have a single overlap.
                // - The overlap starts at or before the requested range. That is, the overlap happens at the end.
                // - The size delta between the new, merged buffer and the old one is of at most 2 pages.
                // In this case, we attempt to extend the buffer further than the requested range,
                // this can potentially avoid future resizes if the application keeps using overlapping
                // sequential memory.
                // Allowing for 2 pages (rather than just one) is necessary to catch cases where the
                // range crosses a page, and after alignment, ends having a size of 2 pages.
                if (overlapsCount == 1 &&
                    gpuVa >= overlaps[0].Address &&
                    endAddress - overlaps[0].EndAddress <= BufferAlignmentSize * 2)
                {
                    // Try to grow the buffer by 1.5x of its current size.
                    // This improves performance in the cases where the buffer is resized often by small amounts.
                    ulong existingSize = overlaps[0].Size;
                    ulong growthSize = (existingSize + Math.Min(existingSize >> 1, MaxDynamicGrowthSize)) & ~BufferAlignmentMask;

                    // Make we sure we won't grow into unmapped or non-contiguous regions.
                    if (size < growthSize)
                    {
                        ulong maximumSize = size + _memoryManager.GetContiguousMappedSize(gpuVa + size, growthSize - size);
                        growthSize = Math.Min(growthSize, maximumSize);
                    }

                    if (size < growthSize)
                    {
                        size = growthSize;
                        endAddress = gpuVa + size;

                        lock (_buffers)
                        {
                            overlapsCount = _buffers.FindOverlapsNonOverlapping(gpuVa, size, ref overlaps);
                        }
                    }
                }

                for (int index = 0; index < overlapsCount; index++)
                {
                    ref BufferView view = ref overlaps[index];

                    anyVirtualOverlap |= view.IsVirtual;

                    gpuVa = Math.Min(gpuVa, view.Address);
                    endAddress = Math.Max(endAddress, view.EndAddress);

                    RemoveView(ref view);
                }

                size = endAddress - gpuVa;
            }

            CreateView(gpuVa, size, overlaps, overlapsCount, anyVirtualOverlap);

            ShrinkOverlapsBufferIfNeeded();
        }

        /// <summary>
        /// Creates a buffer view for a new range of virtual memory.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address where the view starts</param>
        /// <param name="size">Size in bytes of the view</param>
        /// <param name="overlaps">Buffer overlaps that are fully contained inside the view</param>
        /// <param name="overlapCount">Number of overlaps in <paramref name="overlaps"/></param>
        /// <param name="forceVirtual">If true, forces the buffer view to be "virtual", which may not support aliasing</param>
        private void CreateView(ulong gpuVa, ulong size, BufferView[] overlaps, int overlapCount, bool forceVirtual)
        {
            MultiRange range = GetRangeWithOldMappings(gpuVa, size, overlaps, overlapCount);

            // We do not want to create the view if the memory is completely unmapped,
            // as such a buffer would not be able to store any data.
            if (IsFullyUnmapped(range))
            {
                return;
            }

            Buffer buffer;
            BufferView view;

            if (range.Count == 1 && !forceVirtual)
            {
                // Physical buffer creation.
                // We will get a shared buffer from the physical cache.
                // This can only be done if the range is contiguous because we can't
                // share non-contiguous buffers, as such cases would create uncoalescible overlaps.

                MemoryRange subRange = range.GetSubRange(0);

                buffer = _bufferCache.FindOrCreateBuffer(subRange.Address, subRange.Size, out int bufferOffset);
                view = new BufferView(gpuVa, size, bufferOffset, isVirtual: false, buffer);
            }
            else
            {
                // Virtual buffer creation.
                // This buffer is owned by the virtual buffer cache and the physical cache does not know about it.
                // This is done if the range is not contiguous, and we avoid the overlaps problem by managing them by
                // GPU virtual memory ranges, as those will always be contiguous and coalescible.

                // We can only inherit handles from buffers that no longer have any users, be it virtual or physical.
                List<RegionHandleSegment> handles = new List<RegionHandleSegment>();

                for (int index = 0; index < overlapCount; index++)
                {
                    ref BufferView overlapView = ref overlaps[index];
                    if (!overlapView.Buffer.HasViews)
                    {
                        handles.AddRange(overlapView.GetTrackingHandles(gpuVa));
                    }
                }

                buffer = new Buffer(_context, _memoryManager.Physical, range, handles);
                view = new BufferView(gpuVa, size, 0, isVirtual: true, buffer);

                for (int index = 0; index < overlapCount; index++)
                {
                    ref BufferView overlapView = ref overlaps[index];
                    Buffer overlap = overlapView.Buffer;

                    ulong offsetWithinOverlap = overlapView.Address - gpuVa;
                    bool inheritable = !overlap.HasViews;

                    if (inheritable)
                    {
                        // We can only inherit modified ranges if we are also inheriting tracking handles.
                        buffer.InheritModifiedRanges(overlap, offsetWithinOverlap);
                    }
                    else
                    {
                        // If we can't inherit the tracking handles,
                        // make sure the data we will copy is up-to-date.
                        // Also call sync on the new buffer to ensure the range we copied will not be immediately
                        // overwritten on a future sync.
                        overlap.SynchronizeMemory((ulong)overlapView.BaseOffset, overlapView.Size);
                        buffer.SynchronizeMemory(offsetWithinOverlap, overlapView.Size);
                    }

                    overlap.CopyTo(buffer, overlapView.BaseOffset, (int)offsetWithinOverlap, (int)overlapView.Size);

                    DeleteViewBuffer(ref overlapView, dataOnly: true);
                }

                if (overlapCount != 0)
                {
                    buffer.SynchronizeMemory(0, size);

                    _bufferCache.ForceBindingsUpdate();
                }
            }

            lock (_buffers)
            {
                _buffers.Add(view);
            }

            buffer.AddView(_buffers, view);
        }

        /// <summary>
        /// Gets the physical ranges for a given virtual memory region,
        /// while replacing new sub-ranges by old ones if they already exist in the cache.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address of the range</param>
        /// <param name="size">Size of the range in bytes</param>
        /// <param name="overlaps">Existing buffer that overlaps the new range</param>
        /// <param name="overlapCount">Number of overlaps in <paramref name="overlaps"/></param>
        /// <returns>Physical regions where the specified virtual memory range is mapped to</returns>
        private MultiRange GetRangeWithOldMappings(ulong gpuVa, ulong size, BufferView[] overlaps, int overlapCount)
        {
            MultiRange newRange = _memoryManager.GetPhysicalRegions(gpuVa, size);

            // If we have overlaps, place the overlap ranges over the new ranges.
            // This is necessary because we inherit tracking handles, and the inheritance
            // assumes that the mappings did not change for the buffers that overlap.
            // Eventually, RefreshMappings should remove the stale mappings.

            for (int index = 0; index < overlapCount; index++)
            {
                ref BufferView overlapView = ref overlaps[index];

                ulong offsetWithinNew = overlapView.Address - gpuVa;

                MultiRange existingSlice = overlapView.Buffer.Range.Slice((ulong)overlapView.BaseOffset, overlapView.Size);
                MultiRange newSlice = newRange.Slice(offsetWithinNew, overlapView.Size);

                if (!existingSlice.Equals(newSlice))
                {
                    ulong rightOffset = offsetWithinNew + overlapView.Size;

                    MultiRange left = newRange.Slice(0, offsetWithinNew);
                    MultiRange right = newRange.Slice(rightOffset, size - rightOffset);

                    newRange = left.Append(existingSlice).Append(right);
                }
            }

            return newRange;
        }

        /// <summary>
        /// Checks if a multi-range is fully unmapped.
        /// </summary>
        /// <param name="range">Multi-range to check</param>
        /// <returns>True if all pages are unmapped, false otherwise</returns>
        private static bool IsFullyUnmapped(MultiRange range)
        {
            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);
                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes a buffer view from the cache.
        /// </summary>
        /// <param name="view">View to remove</param>
        private void RemoveView(ref BufferView view)
        {
            lock (_buffers)
            {
                _buffers.Remove(view);
            }

            view.Buffer.RemoveView(_buffers, view);
        }

        /// <summary>
        /// Deletes the buffer used by a buffer view if no longer accessible by any view.
        /// </summary>
        /// <param name="view">View using the buffer to be deleted</param>
        /// <param name="dataOnly">True to keep the tracking handles, false to delete everything</param>
        private void DeleteViewBuffer(ref BufferView view, bool dataOnly)
        {
            if (!view.Buffer.HasViews)
            {
                if (view.IsVirtual)
                {
                    if (dataOnly)
                    {
                        view.Buffer.DisposeData();
                    }
                    else
                    {
                        view.Buffer.Dispose();
                    }
                }
                else
                {
                    MemoryRange subRange = view.Buffer.Range.GetSubRange(0);
                    _bufferCache.RemoveBuffer(subRange.Address, subRange.Size, dataOnly);
                }
            }
        }

        /// <summary>
        /// Resizes the temporary buffer used for range list intersection results, if it has grown too much.
        /// </summary>
        private void ShrinkOverlapsBufferIfNeeded()
        {
            if (_viewOverlaps.Length > OverlapsBufferMaxCapacity)
            {
                Array.Resize(ref _viewOverlaps, OverlapsBufferMaxCapacity);
            }
        }

        /// <summary>
        /// Copy a buffer's data from a given address to another.
        /// </summary>
        /// <remarks>
        /// This does a GPU side copy.
        /// </remarks>
        /// <param name="srcVa">GPU virtual address of the copy source</param>
        /// <param name="dstVa">GPU virtual address of the copy destination</param>
        /// <param name="size">Size in bytes of the copy</param>
        public void CopyBuffer(ulong srcVa, ulong dstVa, ulong size)
        {
            RefreshMappings();

            CreateBuffer(srcVa, size);
            CreateBuffer(dstVa, size);

            Buffer srcBuffer = GetBuffer(srcVa, size, write: false, out int srcOffset);
            Buffer dstBuffer = GetBuffer(dstVa, size, write: false, out int dstOffset);

            if (srcBuffer == null || dstBuffer == null)
            {
                return;
            }

            _context.Renderer.Pipeline.CopyBuffer(
                srcBuffer.Handle,
                dstBuffer.Handle,
                srcOffset,
                dstOffset,
                (int)size);

            if (srcBuffer.IsModified((ulong)srcOffset, size))
            {
                dstBuffer.SignalModified((ulong)dstOffset, size);
            }
            else
            {
                // Optimization: If the data being copied is already in memory, then copy it directly instead of flushing from GPU.

                dstBuffer.ClearModified((ulong)dstOffset, size);
                _memoryManager.WriteUntracked(dstVa, _memoryManager.GetSpan(srcVa, (int)size));
            }
        }

        /// <summary>
        /// Clears a buffer at a given address with the specified value.
        /// </summary>
        /// <remarks>
        /// Both the address and size must be aligned to 4 bytes.
        /// </remarks>
        /// <param name="gpuVa">GPU virtual address of the region to clear</param>
        /// <param name="size">Number of bytes to clear</param>
        /// <param name="value">Value to be written into the buffer</param>
        public void ClearBuffer(ulong gpuVa, ulong size, uint value)
        {
            RefreshMappings();

            CreateBuffer(gpuVa, size);

            Buffer buffer = GetBuffer(gpuVa, size, write: false, out int bufferOffset);

            if (buffer == null)
            {
                return;
            }

            _context.Renderer.Pipeline.ClearBuffer(buffer.Handle, bufferOffset, (int)size, value);

            buffer.SignalModified((ulong)bufferOffset, size);
        }

        /// <summary>
        /// Gets a buffer sub-range starting at a given memory address.
        /// </summary>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range starting at the given memory address</returns>
        public BufferRange GetBufferRangeTillEnd(ulong gpuVa, ulong size, bool write = false)
        {
            Buffer buffer = GetBuffer(gpuVa, size, write, out int bufferOffset);

            if (buffer == null)
            {
                return BufferRange.Empty;
            }

            return new BufferRange(buffer.Handle, bufferOffset, (int)buffer.Size - bufferOffset);
        }

        /// <summary>
        /// Gets a buffer sub-range for a given memory range.
        /// </summary>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range for the given range</returns>
        public BufferRange GetBufferRange(ulong gpuVa, ulong size, bool write = false)
        {
            Buffer buffer = GetBuffer(gpuVa, size, write, out int bufferOffset);

            if (buffer == null)
            {
                return BufferRange.Empty;
            }

            return new BufferRange(buffer.Handle, bufferOffset, (int)size);
        }

        /// <summary>
        /// Gets a buffer for a given memory range.
        /// A buffer overlapping with the specified range is assumed to already exist on the cache.
        /// </summary>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <param name="bufferOffset">Offset in bytes of the buffer where <paramref name="gpuVa"/> starts</param>
        /// <returns>The buffer where the range is fully contained</returns>
        private Buffer GetBuffer(ulong gpuVa, ulong size, bool write, out int bufferOffset)
        {
            Buffer buffer = null;
            bufferOffset = 0;

            if (size != 0)
            {
                BufferView view;

                lock (_buffers)
                {
                    view = _buffers.FindFirstOverlap(gpuVa, size);
                }

                buffer = view.Buffer;
                bufferOffset = view.BaseOffset + (int)(gpuVa - view.Address);

                if (buffer != null)
                {
                    buffer.SynchronizeMemory((ulong)bufferOffset, size);

                    if (write)
                    {
                        buffer.SignalModified((ulong)bufferOffset, size);
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Performs guest to host memory synchronization of a given memory range.
        /// </summary>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        public void SynchronizeBufferRange(ulong gpuVa, ulong size)
        {
            if (size != 0)
            {
                BufferView view;

                lock (_buffers)
                {
                    view = _buffers.FindFirstOverlap(gpuVa, size);
                }

                if (view.Buffer == null)
                {
                    return;
                }

                view.Buffer.SynchronizeMemory((ulong)view.BaseOffset + (gpuVa - view.Address), size);
            }
        }

        /// <summary>
        /// Disposes all buffers in the cache.
        /// It's an error to use the buffer cache after disposal.
        /// </summary>
        public void Dispose()
        {
            foreach (BufferView view in _buffers)
            {
                if (view.IsVirtual)
                {
                    view.Buffer.Dispose();
                }
            }
        }
    }
}