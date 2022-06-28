using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;
using Ryujinx.Memory.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer, used to store vertex and index data, uniform and storage buffers, and others.
    /// </summary>
    class Buffer : IDisposable
    {
        private readonly GpuContext _context;
        private readonly PhysicalMemory _physicalMemory;
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

        public bool HasViews => _views.Count != 0;

        private readonly List<(RangeList<BufferView>, BufferView)> _views;

        /// <summary>
        /// Creates a new instance of the buffer.
        /// </summary>
        /// <param name="context">GPU context that the buffer belongs to</param>
        /// <param name="physicalMemory">Physical memory where the buffer is mapped</param>
        /// <param name="range">Range of memory where the buffer data is located</param>
        /// <param name="baseHandles">Tracking handles to be inherited by this buffer</param>
        public Buffer(GpuContext context, PhysicalMemory physicalMemory, MultiRange range, IEnumerable<IRegionHandle> baseHandles = null)
        {
            _context = context;
            _physicalMemory = physicalMemory;
            Range = range;
            Size = range.GetSize();

            Handle = context.Renderer.CreateBuffer((int)Size);

            BufferRegion[] regions = new BufferRegion[range.Count];
            int regionsCount = 0;
            ulong baseOffset = 0;

            IRegionHandle[] handlesArray = null;
            int currentIndex = 0;

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    IEnumerable<IRegionHandle> handlesForRange = baseHandles;

                    if (baseHandles != null && range.Count > 1)
                    {
                        if (handlesArray == null)
                        {
                            handlesArray = baseHandles.ToArray();
                        }

                        int previousIndex = currentIndex;
                        ulong currentAddress = subRange.Address;

                        while (currentIndex < handlesArray.Length &&
                            (handlesArray[currentIndex].Address > currentAddress ||
                            (handlesArray[currentIndex].Address == currentAddress && previousIndex == currentIndex)) &&
                            handlesArray[currentIndex].EndAddress <= subRange.EndAddress)
                        {
                            currentAddress = handlesArray[currentIndex].Address;
                            currentIndex++;
                        }

                        int count = currentIndex - previousIndex;
                        if (count != 0)
                        {
                            handlesForRange = new ArraySegment<IRegionHandle>(handlesArray, previousIndex, count);
                        }
                        else
                        {
                            handlesForRange = null;
                        }
                    }

                    regions[regionsCount++] = new BufferRegion(
                        context,
                        physicalMemory,
                        subRange.Address,
                        subRange.Size,
                        Handle,
                        baseOffset,
                        handlesForRange);
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

        public IEnumerable<IRegionHandle> GetTrackingHandles()
        {
            if (_regions.Length == 1)
            {
                return _regions[0].GetTrackingHandles();
            }

            return _regions.SelectMany(x => x.GetTrackingHandles());
        }

        public IEnumerable<IRegionHandle> GetTrackingHandlesSlice(ulong bufferOffset, ulong size)
        {
            ulong bufferEndOffset = bufferOffset + size;
            int index = FindStartIndex(bufferOffset);

            ulong skipSize = 0;

            for (int i = 0; i < index; i++)
            {
                skipSize += _regions[i].Size;
            }

            ulong takeSize = 0;

            for (; index < _regions.Length; index++)
            {
                BufferRegion region = _regions[index];

                if (region.BaseOffset >= bufferEndOffset)
                {
                    break;
                }

                if (bufferOffset > region.BaseOffset)
                {
                    skipSize += bufferOffset - region.BaseOffset;
                }

                ulong clampedOffset = Math.Max(bufferOffset, region.BaseOffset);
                ulong clampedEndOffset = Math.Min(bufferEndOffset, region.BaseOffset + region.Size);

                takeSize += clampedEndOffset - clampedOffset;
            }

            int skipCount = (int)(skipSize / BufferRegion.GranularBufferThreshold);
            int takeCount = (int)(takeSize / BufferRegion.GranularBufferThreshold);

            return GetTrackingHandles().Skip(skipCount).Take(takeCount);
        }

        public void AddView(RangeList<BufferView> list, BufferView view)
        {
            _views.Add((list, view));
        }

        public void RemoveView(RangeList<BufferView> list, BufferView view)
        {
            _views.Remove((list, view));
        }

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
                if (_regions[i].OverlapsWith(from._regions[j].Address, from._regions[j].Size))
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
                if (_regions[i].OverlapsWith(from._regions[j].Address, from._regions[j].Size))
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
        /// Performs copy of all the buffer data from one buffer to another.
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

        private static (ulong, ulong) GetRegionAddressAndSize(BufferRegion region, ulong bufferOffset, ulong bufferEndOffset)
        {
            ulong clampedOffset = Math.Max(region.BaseOffset, bufferOffset);
            ulong clampedEndOffset = Math.Min(region.BaseOffset + region.Size, bufferEndOffset);
            ulong clampedSize = clampedEndOffset - clampedOffset;

            return (region.Address + (clampedOffset - region.BaseOffset), clampedSize);
        }

        /// <summary>
        /// Disposes the host buffer's data, not its tracking handles.
        /// </summary>
        public void DisposeData()
        {
            foreach (BufferRegion region in _regions)
            {
                region.DisposeData();
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