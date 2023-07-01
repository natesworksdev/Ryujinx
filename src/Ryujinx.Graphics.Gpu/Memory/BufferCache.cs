using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer cache.
    /// </summary>
    class BufferCache : IDisposable
    {
        /// <summary>
        /// Initial size for the array holding overlaps.
        /// </summary>
        public const int OverlapsBufferInitialCapacity = 10;

        /// <summary>
        /// Maximum size that an array holding overlaps may have after trimming.
        /// </summary>
        public const int OverlapsBufferMaxCapacity = 10000;

        private const ulong BufferAlignmentSize = 0x1000;
        private const ulong BufferAlignmentMask = BufferAlignmentSize - 1;

        /// <summary>
        /// Alignment required for sparse buffer mappings.
        /// </summary>
        public const ulong SparseBufferAlignmentSize = 0x10000;

        private const ulong MaxDynamicGrowthSize = 0x100000;

        private readonly GpuContext _context;
        private readonly PhysicalMemory _physicalMemory;

        /// <remarks>
        /// Only modified from the GPU thread. Must lock for add/remove.
        /// Must lock for any access from other threads.
        /// </remarks>
        private readonly RangeList<Buffer> _buffers;
        private readonly MultiRangeList<MultiRangeBuffer> _multiRangeBuffers;

        private Buffer[] _bufferOverlaps;

        private readonly Dictionary<ulong, BufferCacheEntry> _dirtyCache;
        private readonly Dictionary<ulong, BufferCacheEntry> _modifiedCache;
        private bool _pruneCaches;

        public event Action NotifyBuffersModified;

        /// <summary>
        /// Creates a new instance of the buffer manager.
        /// </summary>
        /// <param name="context">The GPU context that the buffer manager belongs to</param>
        /// <param name="physicalMemory">Physical memory where the cached buffers are mapped</param>
        public BufferCache(GpuContext context, PhysicalMemory physicalMemory)
        {
            _context = context;
            _physicalMemory = physicalMemory;

            _buffers = new RangeList<Buffer>();
            _multiRangeBuffers = new MultiRangeList<MultiRangeBuffer>();

            _bufferOverlaps = new Buffer[OverlapsBufferInitialCapacity];

            _dirtyCache = new Dictionary<ulong, BufferCacheEntry>();

            // There are a lot more entries on the modified cache, so it is separate from the one for ForceDirty.
            _modifiedCache = new Dictionary<ulong, BufferCacheEntry>();
        }

        /// <summary>
        /// Handles removal of buffers written to a memory region being unmapped.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void MemoryUnmappedHandler(object sender, UnmapEventArgs e)
        {
            Buffer[] overlaps = new Buffer[10];
            int overlapCount;

            ulong address = ((MemoryManager)sender).Translate(e.Address);
            ulong size = e.Size;

            lock (_buffers)
            {
                overlapCount = _buffers.FindOverlaps(address, size, ref overlaps);
            }

            for (int i = 0; i < overlapCount; i++)
            {
                overlaps[i].Unmapped(address, size);
            }
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and creates a
        /// new buffer, if needed, for the specified range.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <returns>CPU virtual address of the buffer, after address translation</returns>
        public ulong TranslateAndCreateBuffer(MemoryManager memoryManager, ulong gpuVa, ulong size)
        {
            if (gpuVa == 0)
            {
                return 0;
            }

            ulong address = memoryManager.Translate(gpuVa);

            if (address == MemoryManager.PteUnmapped)
            {
                return 0;
            }

            CreateBuffer(address, size);

            return address;
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and creates
        /// new buffers, if needed, for the specified range.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <returns>CPU virtual addresses of the buffer, after address translation</returns>
        public MultiRange TranslateAndCreateBuffers(MemoryManager memoryManager, ulong gpuVa, ulong size)
        {
            if (gpuVa == 0)
            {
                return new MultiRange(MemoryManager.PteUnmapped, size);
            }

            bool supportsSparse = _context.Capabilities.SupportsSparseBuffer;

            if (memoryManager.VirtualBufferCache.TryGetOrAddRange(gpuVa, size, supportsSparse, out MultiRange range))
            {
                return range;
            }

            if (range.Count > 1)
            {
                for (int i = 0; i < range.Count; i++)
                {
                    MemoryRange subRange = range.GetSubRange(i);

                    if (subRange.Address != MemoryManager.PteUnmapped)
                    {
                        CreateBuffer(subRange.Address, subRange.Size, SparseBufferAlignmentSize);
                    }
                }

                CreateMultiRangeBuffer(range);
            }
            else
            {
                MemoryRange subRange = range.GetSubRange(0);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    CreateBuffer(subRange.Address, subRange.Size);
                }
            }

            return range;
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if it does not yet exist.
        /// This can be used to ensure the existance of a buffer.
        /// </summary>
        /// <param name="address">Address of the buffer in memory</param>
        /// <param name="size">Size of the buffer in bytes</param>
        public void CreateBuffer(ulong address, ulong size)
        {
            ulong endAddress = address + size;

            ulong alignedAddress = address & ~BufferAlignmentMask;
            ulong alignedEndAddress = (endAddress + BufferAlignmentMask) & ~BufferAlignmentMask;

            // The buffer must have the size of at least one page.
            if (alignedEndAddress == alignedAddress)
            {
                alignedEndAddress += BufferAlignmentSize;
            }

            CreateBufferAligned(alignedAddress, alignedEndAddress - alignedAddress);
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if it does not yet exist.
        /// This can be used to ensure the existance of a buffer.
        /// </summary>
        /// <param name="address">Address of the buffer in memory</param>
        /// <param name="size">Size of the buffer in bytes</param>
        /// <param name="alignment">Alignment of the buffer in bytes</param>
        public void CreateBuffer(ulong address, ulong size, ulong alignment)
        {
            ulong alignmentMask = alignment - 1;
            ulong endAddress = address + size;

            ulong alignedAddress = address & ~alignmentMask;
            ulong alignedEndAddress = (endAddress + alignmentMask) & ~alignmentMask;

            // The buffer must have the size of at least one page.
            if (alignedEndAddress == alignedAddress)
            {
                alignedEndAddress += alignment;
            }

            CreateBufferAligned(alignedAddress, alignedEndAddress - alignedAddress, alignment);
        }

        /// <summary>
        /// Creates a buffer for a memory region composed of multiple physical ranges,
        /// if it does not exist yet.
        /// </summary>
        /// <param name="range">Physical ranges of memory</param>
        private void CreateMultiRangeBuffer(MultiRange range)
        {
            MultiRangeBuffer[] overlaps = new MultiRangeBuffer[10];

            int overlapCount = _multiRangeBuffers.FindOverlaps(range, ref overlaps);

            for (int index = 0; index < overlapCount; index++)
            {
                if (overlaps[index].Range.Contains(range))
                {
                    return;
                }
            }

            for (int index = 0; index < overlapCount; index++)
            {
                if (range.Contains(overlaps[index].Range))
                {
                    _multiRangeBuffers.Remove(overlaps[index]);
                }
            }

            BufferRange[] storages = new BufferRange[range.Count];

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                ulong alignmentMask = SparseBufferAlignmentSize - 1;
                ulong endAddress = subRange.Address + subRange.Size;

                ulong alignedAddress = subRange.Address & ~alignmentMask;
                ulong alignedEndAddress = (endAddress + alignmentMask) & ~alignmentMask;
                ulong alignedSize = alignedEndAddress - alignedAddress;

                Buffer buffer = _buffers.FindFirstOverlap(alignedAddress, alignedSize);
                BufferRange bufferRange = buffer.GetRange(alignedAddress, alignedSize, false);

                storages[i] = bufferRange;
            }

            MultiRangeBuffer multiRangeBuffer = new(_context, range, storages);

            _multiRangeBuffers.Add(multiRangeBuffer);
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and attempts to force
        /// the buffer in the region as dirty.
        /// The buffer lookup for this function is cached in a dictionary for quick access, which
        /// accelerates common UBO updates.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        public void ForceDirty(MemoryManager memoryManager, ulong gpuVa, ulong size)
        {
            if (_pruneCaches)
            {
                Prune();
            }

            if (!_dirtyCache.TryGetValue(gpuVa, out BufferCacheEntry result) ||
                result.EndGpuAddress < gpuVa + size ||
                result.UnmappedSequence != result.Buffer.UnmappedSequence)
            {
                ulong address = TranslateAndCreateBuffer(memoryManager, gpuVa, size);
                result = new BufferCacheEntry(address, gpuVa, GetBuffer(address, size));

                _dirtyCache[gpuVa] = result;
            }

            result.Buffer.ForceDirty(result.Address, size);
        }

        /// <summary>
        /// Checks if the given buffer range has been GPU modifed.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <returns>True if modified, false otherwise</returns>
        public bool CheckModified(MemoryManager memoryManager, ulong gpuVa, ulong size, out ulong outAddr)
        {
            if (_pruneCaches)
            {
                Prune();
            }

            // Align the address to avoid creating too many entries on the quick lookup dictionary.
            ulong mask = BufferAlignmentMask;
            ulong alignedGpuVa = gpuVa & (~mask);
            ulong alignedEndGpuVa = (gpuVa + size + mask) & (~mask);

            size = alignedEndGpuVa - alignedGpuVa;

            if (!_modifiedCache.TryGetValue(alignedGpuVa, out BufferCacheEntry result) ||
                result.EndGpuAddress < alignedEndGpuVa ||
                result.UnmappedSequence != result.Buffer.UnmappedSequence)
            {
                ulong address = TranslateAndCreateBuffer(memoryManager, alignedGpuVa, size);
                result = new BufferCacheEntry(address, alignedGpuVa, GetBuffer(address, size));

                _modifiedCache[alignedGpuVa] = result;
            }

            outAddr = result.Address | (gpuVa & mask);

            return result.Buffer.IsModified(result.Address, size);
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="address">Address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        private void CreateBufferAligned(ulong address, ulong size)
        {
            Buffer[] overlaps = _bufferOverlaps;
            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);

            if (overlapsCount != 0)
            {
                // The buffer already exists. We can just return the existing buffer
                // if the buffer we need is fully contained inside the overlapping buffer.
                // Otherwise, we must delete the overlapping buffers and create a bigger buffer
                // that fits all the data we need. We also need to copy the contents from the
                // old buffer(s) to the new buffer.

                ulong endAddress = address + size;
                Buffer overlap0 = overlaps[0];

                if (overlap0.Address > address || overlap0.EndAddress < endAddress)
                {
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
                        address >= overlaps[0].Address &&
                        endAddress - overlaps[0].EndAddress <= BufferAlignmentSize * 2)
                    {
                        // Try to grow the buffer by 1.5x of its current size.
                        // This improves performance in the cases where the buffer is resized often by small amounts.
                        ulong existingSize = overlaps[0].Size;
                        ulong growthSize = (existingSize + Math.Min(existingSize >> 1, MaxDynamicGrowthSize)) & ~BufferAlignmentMask;

                        size = Math.Max(size, growthSize);
                        endAddress = address + size;

                        overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);
                    }

                    for (int index = 0; index < overlapsCount; index++)
                    {
                        Buffer buffer = overlaps[index];

                        address = Math.Min(address, buffer.Address);
                        endAddress = Math.Max(endAddress, buffer.EndAddress);

                        lock (_buffers)
                        {
                            _buffers.Remove(buffer);
                        }
                    }

                    ulong newSize = endAddress - address;

                    CreateBufferAligned(address, newSize, overlaps, overlapsCount);
                }
            }
            else
            {
                // No overlap, just create a new buffer.
                Buffer buffer = new(_context, _physicalMemory, address, size);

                lock (_buffers)
                {
                    _buffers.Add(buffer);
                }
            }

            ShrinkOverlapsBufferIfNeeded();
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="address">Address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="alignment">Buffer range alignment</param>
        private void CreateBufferAligned(ulong address, ulong size, ulong alignment)
        {
            Buffer[] overlaps = _bufferOverlaps;
            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);

            if (overlapsCount != 0)
            {
                // If the buffer already exists, make sure if covers the entire range,
                // and make sure it is properly aligned, otherwise sparse mapping may fail.

                ulong endAddress = address + size;
                Buffer overlap0 = overlaps[0];

                if (overlap0.Address > address ||
                    overlap0.EndAddress < endAddress ||
                    (overlap0.Address & (alignment - 1)) != 0 ||
                    (overlap0.EndAddress & (alignment - 1)) != 0)
                {
                    // We need to make sure the new buffer is properly aligned.
                    // However, after the range is aligned, it is possible that it
                    // overlaps more buffers, so try again after each extension
                    // and ensure we cover all overlaps.

                    int oldOverlapsCount;

                    do
                    {
                        for (int index = 0; index < overlapsCount; index++)
                        {
                            Buffer buffer = overlaps[index];

                            address    = Math.Min(address,    buffer.Address);
                            endAddress = Math.Max(endAddress, buffer.EndAddress);
                        }

                        address &= ~(alignment - 1);
                        endAddress = (endAddress + alignment - 1) & ~(alignment - 1);

                        oldOverlapsCount = overlapsCount;
                        overlapsCount = _buffers.FindOverlapsNonOverlapping(address, endAddress - address, ref overlaps);
                    }
                    while (oldOverlapsCount != overlapsCount);

                    lock (_buffers)
                    {
                        for (int index = 0; index < overlapsCount; index++)
                        {
                            _buffers.Remove(overlaps[index]);
                        }
                    }

                    ulong newSize = endAddress - address;

                    CreateBufferAligned(address, newSize, overlaps, overlapsCount);
                }
            }
            else
            {
                // No overlap, just create a new buffer.
                Buffer buffer = new(_context, _physicalMemory, address, size);

                lock (_buffers)
                {
                    _buffers.Add(buffer);
                }
            }

            ShrinkOverlapsBufferIfNeeded();
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="address">Address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="overlaps">Buffers overlapping the range</param>
        /// <param name="overlapsCount">Total of overlaps</param>
        private void CreateBufferAligned(ulong address, ulong size, Buffer[] overlaps, int overlapsCount)
        {
            Buffer newBuffer = new Buffer(_context, _physicalMemory, address, size, overlaps.Take(overlapsCount));

            lock (_buffers)
            {
                _buffers.Add(newBuffer);
            }

            for (int index = 0; index < overlapsCount; index++)
            {
                Buffer buffer = overlaps[index];

                int dstOffset = (int)(buffer.Address - newBuffer.Address);

                buffer.CopyTo(newBuffer, dstOffset);
                newBuffer.InheritModifiedRanges(buffer);

                buffer.DecrementReferenceCount();
            }

            newBuffer.SynchronizeMemory(address, size);

            // Existing buffers were modified, we need to rebind everything.
            NotifyBuffersModified?.Invoke();

            RecreateMultiRangeBuffers(address, size);
        }

        /// <summary>
        /// Recreates all the multi-range buffers that overlaps a given physical memory range.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range in bytes</param>
        private void RecreateMultiRangeBuffers(ulong address, ulong size)
        {
            if ((address & (SparseBufferAlignmentSize - 1)) != 0 || (size & (SparseBufferAlignmentSize - 1)) != 0)
            {
                return;
            }

            MultiRangeBuffer[] overlaps = new MultiRangeBuffer[10];

            int overlapCount = _multiRangeBuffers.FindOverlaps(address, size, ref overlaps);

            for (int index = 0; index < overlapCount; index++)
            {
                _multiRangeBuffers.Remove(overlaps[index]);
            }

            for (int index = 0; index < overlapCount; index++)
            {
                CreateMultiRangeBuffer(overlaps[index].Range);
            }
        }

        /// <summary>
        /// Resizes the temporary buffer used for range list intersection results, if it has grown too much.
        /// </summary>
        private void ShrinkOverlapsBufferIfNeeded()
        {
            if (_bufferOverlaps.Length > OverlapsBufferMaxCapacity)
            {
                Array.Resize(ref _bufferOverlaps, OverlapsBufferMaxCapacity);
            }
        }

        /// <summary>
        /// Copy a buffer data from a given address to another.
        /// </summary>
        /// <remarks>
        /// This does a GPU side copy.
        /// </remarks>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="srcVa">GPU virtual address of the copy source</param>
        /// <param name="dstVa">GPU virtual address of the copy destination</param>
        /// <param name="size">Size in bytes of the copy</param>
        public void CopyBuffer(MemoryManager memoryManager, ulong srcVa, ulong dstVa, ulong size)
        {
            ulong srcAddress = TranslateAndCreateBuffer(memoryManager, srcVa, size);
            ulong dstAddress = TranslateAndCreateBuffer(memoryManager, dstVa, size);

            Buffer srcBuffer = GetBuffer(srcAddress, size);
            Buffer dstBuffer = GetBuffer(dstAddress, size);

            int srcOffset = (int)(srcAddress - srcBuffer.Address);
            int dstOffset = (int)(dstAddress - dstBuffer.Address);

            _context.Renderer.Pipeline.CopyBuffer(
                srcBuffer.Handle,
                dstBuffer.Handle,
                srcOffset,
                dstOffset,
                (int)size);

            if (srcBuffer.IsModified(srcAddress, size))
            {
                dstBuffer.SignalModified(dstAddress, size);
            }
            else
            {
                // Optimization: If the data being copied is already in memory, then copy it directly instead of flushing from GPU.

                dstBuffer.ClearModified(dstAddress, size);
                memoryManager.Physical.WriteTrackedResource(dstAddress, memoryManager.Physical.GetSpan(srcAddress, (int)size), ResourceKind.Buffer);
            }
        }

        /// <summary>
        /// Clears a buffer at a given address with the specified value.
        /// </summary>
        /// <remarks>
        /// Both the address and size must be aligned to 4 bytes.
        /// </remarks>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">GPU virtual address of the region to clear</param>
        /// <param name="size">Number of bytes to clear</param>
        /// <param name="value">Value to be written into the buffer</param>
        public void ClearBuffer(MemoryManager memoryManager, ulong gpuVa, ulong size, uint value)
        {
            ulong address = TranslateAndCreateBuffer(memoryManager, gpuVa, size);

            Buffer buffer = GetBuffer(address, size);

            int offset = (int)(address - buffer.Address);

            _context.Renderer.Pipeline.ClearBuffer(buffer.Handle, offset, (int)size, value);

            memoryManager.Physical.FillTrackedResource(address, size, value, ResourceKind.Buffer);
        }

        /// <summary>
        /// Gets a buffer sub-range starting at a given memory address, aligned to the next page boundary.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range starting at the given memory address</returns>
        public BufferRange GetBufferRangeAligned(MultiRange range, bool write = false)
        {
            if (range.Count > 1)
            {
                return GetBuffer(range, write).GetRange(range);
            }
            else
            {
                MemoryRange subRange = range.GetSubRange(0);
                return GetBuffer(subRange.Address, subRange.Size, write).GetRangeAligned(subRange.Address, subRange.Size, write);
            }
        }

        /// <summary>
        /// Gets a buffer sub-range for a given memory range.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range for the given range</returns>
        public BufferRange GetBufferRange(MultiRange range, bool write = false)
        {
            if (range.Count > 1)
            {
                return GetBuffer(range, write).GetRange(range);
            }
            else
            {
                MemoryRange subRange = range.GetSubRange(0);
                return GetBuffer(subRange.Address, subRange.Size, write).GetRange(subRange.Address, subRange.Size, write);
            }
        }

        /// <summary>
        /// Gets a buffer sub-range starting at a given memory address.
        /// </summary>
        /// <param name="address">Start address of the memory range</param>
        /// <param name="size">Size in bytes of the memory range</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range starting at the given memory address</returns>
        public BufferRange GetBufferRangeAligned(ulong address, ulong size, bool write = false)
        {
            return GetBuffer(address, size, write).GetRangeAligned(address, size, write);
        }

        /// <summary>
        /// Gets a buffer sub-range for a given memory range.
        /// </summary>
        /// <param name="address">Start address of the memory range</param>
        /// <param name="size">Size in bytes of the memory range</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range for the given range</returns>
        public BufferRange GetBufferRange(ulong address, ulong size, bool write = false)
        {
            return GetBuffer(address, size, write).GetRange(address, size, write);
        }

        /// <summary>
        /// Gets a buffer for a given memory range.
        /// A buffer overlapping with the specified range is assumed to already exist on the cache.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer where the range is fully contained</returns>
        private MultiRangeBuffer GetBuffer(MultiRange range, bool write = false)
        {
            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                Buffer subBuffer = _buffers.FindFirstOverlap(subRange.Address, subRange.Size);

                subBuffer.SynchronizeMemory(subRange.Address, subRange.Size);

                if (write)
                {
                    subBuffer.SignalModified(subRange.Address, subRange.Size);
                }
            }

            MultiRangeBuffer[] overlaps = new MultiRangeBuffer[10];

            int overlapCount = _multiRangeBuffers.FindOverlaps(range, ref overlaps);

            MultiRangeBuffer buffer = null;

            for (int i = 0; i < overlapCount; i++)
            {
                if (overlaps[i].Range.Contains(range))
                {
                    buffer = overlaps[i];
                    break;
                }
            }

            return buffer;
        }

        /// <summary>
        /// Gets a buffer for a given memory range.
        /// A buffer overlapping with the specified range is assumed to already exist on the cache.
        /// </summary>
        /// <param name="address">Start address of the memory range</param>
        /// <param name="size">Size in bytes of the memory range</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer where the range is fully contained</returns>
        private Buffer GetBuffer(ulong address, ulong size, bool write = false)
        {
            Buffer buffer;

            if (size != 0)
            {
                buffer = _buffers.FindFirstOverlap(address, size);

                buffer.SynchronizeMemory(address, size);

                if (write)
                {
                    buffer.SignalModified(address, size);
                }
            }
            else
            {
                buffer = _buffers.FindFirstOverlap(address, 1);
            }

            return buffer;
        }

        /// <summary>
        /// Performs guest to host memory synchronization of a given memory range.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        public void SynchronizeBufferRange(MultiRange range)
        {
            SynchronizeBufferRange(range.GetSubRange(0).Address, range.GetSubRange(0).Size);
        }

        /// <summary>
        /// Performs guest to host memory synchronization of a given memory range.
        /// </summary>
        /// <param name="address">Start address of the memory range</param>
        /// <param name="size">Size in bytes of the memory range</param>
        public void SynchronizeBufferRange(ulong address, ulong size)
        {
            if (size != 0)
            {
                Buffer buffer = _buffers.FindFirstOverlap(address, size);

                buffer.SynchronizeMemory(address, size);
            }
        }

        /// <summary>
        /// Prune any invalid entries from a quick access dictionary.
        /// </summary>
        /// <param name="dictionary">Dictionary to prune</param>
        /// <param name="toDelete">List used to track entries to delete</param>
        private static void Prune(Dictionary<ulong, BufferCacheEntry> dictionary, ref List<ulong> toDelete)
        {
            foreach (var entry in dictionary)
            {
                if (entry.Value.UnmappedSequence != entry.Value.Buffer.UnmappedSequence)
                {
                    (toDelete ??= new()).Add(entry.Key);
                }
            }

            if (toDelete != null)
            {
                foreach (ulong entry in toDelete)
                {
                    dictionary.Remove(entry);
                }
            }
        }

        /// <summary>
        /// Prune any invalid entries from the quick access dictionaries.
        /// </summary>
        private void Prune()
        {
            List<ulong> toDelete = null;

            Prune(_dirtyCache, ref toDelete);

            toDelete?.Clear();

            Prune(_modifiedCache, ref toDelete);

            _pruneCaches = false;
        }

        /// <summary>
        /// Queues a prune of invalid entries the next time a dictionary cache is accessed.
        /// </summary>
        public void QueuePrune()
        {
            _pruneCaches = true;
        }

        /// <summary>
        /// Disposes all buffers in the cache.
        /// It's an error to use the buffer cache after disposal.
        /// </summary>
        public void Dispose()
        {
            lock (_buffers)
            {
                foreach (Buffer buffer in _buffers)
                {
                    buffer.Dispose();
                }
            }
        }
    }
}
