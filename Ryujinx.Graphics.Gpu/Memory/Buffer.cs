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
    class Buffer : IMultiRangeItem, IDisposable
    {
        private const ulong GranularBufferThreshold = 4096;

        private readonly GpuContext _context;
        private readonly PhysicalMemory _physicalMemory;

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

        /// <summary>
        /// Ranges of the buffer that have been modified on the GPU.
        /// Ranges defined here cannot be updated from CPU until a CPU waiting sync point is reached.
        /// Then, write tracking will signal, wait for GPU sync (generated at the syncpoint) and flush these regions.
        /// </summary>
        /// <remarks>
        /// This is null until at least one modification occurs.
        /// </remarks>
        private BufferModifiedRangeList _modifiedRanges = null;

        private readonly GpuMultiRegionHandle _memoryTrackingGranular;
        private readonly GpuRegionHandle _memoryTracking;

        private readonly RegionSignal _externalFlushDelegate;
        private readonly Action<ulong, ulong> _loadDelegate;
        private readonly Action<ulong, ulong> _modifiedDelegate;

        private int _sequenceNumber;

        private bool _useGranular;
        private bool _syncActionRegistered;

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
            _context        = context;
            _physicalMemory = physicalMemory;
            Range           = range;
            Size            = range.GetSize();

            Handle = context.Renderer.CreateBuffer((int)Size);

            _useGranular = Size > GranularBufferThreshold || range.Count > 1;

            if (_useGranular)
            {
                _memoryTrackingGranular = physicalMemory.BeginGranularTracking(range, baseHandles);

                _memoryTrackingGranular.RegisterPreciseAction(range, PreciseAction);
            }
            else
            {
                _memoryTracking = physicalMemory.BeginTracking(range);

                if (baseHandles != null && baseHandles.Any())
                {
                    _memoryTracking.Reprotect(false);

                    foreach (IRegionHandle handle in baseHandles)
                    {
                        if (handle.Dirty)
                        {
                            _memoryTracking.Reprotect(true);
                        }

                        handle.Dispose();
                    }
                }

                _memoryTracking.RegisterPreciseAction(PreciseAction);
            }

            _externalFlushDelegate = new RegionSignal(ExternalFlush);
            _loadDelegate = new Action<ulong, ulong>(LoadRegion);
            _modifiedDelegate = new Action<ulong, ulong>(RegionModified);

            _views = new List<(RangeList<BufferView>, BufferView)>();
        }

        public IEnumerable<IRegionHandle> GetTrackingHandles()
        {
            if (_useGranular)
            {
                return _memoryTrackingGranular.GetHandles();
            }
            else
            {
                return Enumerable.Repeat(_memoryTracking.GetHandle(), 1);
            }
        }

        public IEnumerable<IRegionHandle> GetTrackingHandlesSlice(MultiRange range)
        {
            int offset = Range.FindOffset(range);
            if (offset == -1)
            {
                return Enumerable.Empty<IRegionHandle>();
            }

            ulong sliceSize = range.GetSize();
            ulong endOffset = (ulong)offset + sliceSize;
            ulong currentOffset = 0;
            ulong skipSize = 0;
            ulong takeSize = 0;

            for (int i = 0; i < Range.Count && currentOffset < endOffset; i++)
            {
                MemoryRange subRange = Range.GetSubRange(i);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    if (currentOffset < (ulong)offset)
                    {
                        skipSize += subRange.Size;
                    }
                    else
                    {
                        takeSize += subRange.Size;
                    }
                }

                currentOffset += subRange.Size;
            }

            int skipCount = (int)(skipSize / GranularBufferThreshold);
            int takeCount = (int)(takeSize / GranularBufferThreshold);

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
        /// Gets offset within the buffer for a given memory region.
        /// </summary>
        /// <remarks>
        /// The range is assumed to be contained inside the buffer. If not, the offset returned will be invalid.
        /// </remarks>
        /// <param name="range">Range of memory that is fully contained inside the buffer, to get the offset from</param>
        /// <returns>The offset within the buffer</returns>
        public int GetOffset(MultiRange range)
        {
            return Range.FindOffset(range);
        }

        /// <summary>
        /// Gets a sub-range from the buffer.
        /// </summary>
        /// <remarks>
        /// This can be used to bind and use sub-ranges of the buffer on the host API.
        /// </remarks>
        /// <param name="range">Range of memory representing the sub-range of the buffer to slice</param>
        /// <returns>The buffer sub-range</returns>
        public BufferRange GetRange(MultiRange range)
        {
            return new BufferRange(Handle, Range.FindOffset(range), (int)range.GetSize());
        }

        /// <summary>
        /// Gets a sub-range from the buffer, from a start address till the end of the buffer.
        /// </summary>
        /// <remarks>
        /// This can be used to bind and use sub-ranges of the buffer on the host API.
        /// </remarks>
        /// <param name="range">Range of memory representing the sub-range of the buffer to slice</param>
        /// <returns>The buffer sub-range</returns>
        public BufferRange GetRangeTillEnd(MultiRange range)
        {
            int offset = Range.FindOffset(range);

            return new BufferRange(Handle, offset, (int)(Size - (ulong)offset));
        }

        /// <summary>
        /// Performs guest to host memory synchronization of the buffer data.
        /// </summary>
        /// <remarks>
        /// This causes the buffer data to be overwritten if a write was detected from the CPU,
        /// since the last call to this method.
        /// </remarks>
        /// <param name="range">Memory range of the modified region</param>
        public void SynchronizeMemory(MultiRange range)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                SynchronizeMemory(subRange.Address, subRange.Size);
            }
        }

        /// <summary>
        /// Performs guest to host memory synchronization of the buffer data.
        /// </summary>
        /// <remarks>
        /// This causes the buffer data to be overwritten if a write was detected from the CPU,
        /// since the last call to this method.
        /// </remarks>
        /// <param name="address">Start address of the range to synchronize</param>
        /// <param name="size">Size in bytes of the range to synchronize</param>
        private void SynchronizeMemory(ulong address, ulong size)
        {
            if (_useGranular)
            {
                _memoryTrackingGranular.QueryModified(address, size, _modifiedDelegate, _context.SequenceNumber);
            }
            else
            {
                if (_context.SequenceNumber != _sequenceNumber && _memoryTracking.DirtyOrVolatile())
                {
                    Debug.Assert(Range.Count == 1);
                    MemoryRange subRange = Range.GetSubRange(0);

                    _memoryTracking.Reprotect();

                    if (_modifiedRanges != null)
                    {
                        _modifiedRanges.ExcludeModifiedRegions(subRange.Address, subRange.Size, _loadDelegate);
                    }
                    else
                    {
                        _context.Renderer.SetBufferData(Handle, 0, _physicalMemory.GetSpan(subRange.Address, (int)subRange.Size));
                    }

                    _sequenceNumber = _context.SequenceNumber;
                }
            }
        }

        /// <summary>
        /// Ensure that the modified range list exists.
        /// </summary>
        private void EnsureRangeList()
        {
            if (_modifiedRanges == null)
            {
                _modifiedRanges = new BufferModifiedRangeList(_context);
            }
        }

        /// <summary>
        /// Signal that the given region of the buffer has been modified.
        /// </summary>
        /// <param name="range">Memory range of the modified region</param>
        public void SignalModified(MultiRange range)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                SignalModified(subRange.Address, subRange.Size);
            }
        }

        /// <summary>
        /// Signal that the given region of the buffer has been modified.
        /// </summary>
        /// <param name="address">The start address of the modified region</param>
        /// <param name="size">The size of the modified region</param>
        private void SignalModified(ulong address, ulong size)
        {
            EnsureRangeList();

            _modifiedRanges.SignalModified(address, size);

            if (!_syncActionRegistered)
            {
                _context.RegisterSyncAction(SyncAction);
                _syncActionRegistered = true;
            }
        }

        /// <summary>
        /// Indicate that mofifications in a given region of this buffer have been overwritten.
        /// </summary>
        /// <param name="range">Memory range of the region</param>
        public void ClearModified(MultiRange range)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                _modifiedRanges?.Clear(subRange.Address, subRange.Size);
            }
        }

        /// <summary>
        /// Action to be performed when a syncpoint is reached after modification.
        /// This will register read/write tracking to flush the buffer from GPU when its memory is used.
        /// </summary>
        private void SyncAction()
        {
            _syncActionRegistered = false;

            if (_useGranular)
            {
                for (int index = 0; index < Range.Count; index++)
                {
                    MemoryRange subRange = Range.GetSubRange(index);

                    _modifiedRanges?.GetRanges(subRange.Address, subRange.Size, (address, size) =>
                    {
                        _memoryTrackingGranular.RegisterAction(address, size, _externalFlushDelegate);
                        SynchronizeMemory(address, size);
                    });
                }
            }
            else
            {
                Debug.Assert(Range.Count == 1);
                MemoryRange subRange = Range.GetSubRange(0);

                _memoryTracking.RegisterAction(_externalFlushDelegate);
                SynchronizeMemory(subRange.Address, subRange.Size);
            }
        }

        /// <summary>
        /// Inherit modified ranges from another buffer.
        /// </summary>
        /// <param name="from">The buffer to inherit from</param>
        /// <param name="bounded">Indicates that only the regions contained inside this buffer should be inherited</param>
        public void InheritModifiedRanges(Buffer from, bool bounded = false)
        {
            if (from._modifiedRanges != null)
            {
                if (from._syncActionRegistered && !_syncActionRegistered)
                {
                    _context.RegisterSyncAction(SyncAction);
                    _syncActionRegistered = true;
                }

                Action<ulong, ulong> registerRangeAction = (ulong address, ulong size) =>
                {
                    if (_useGranular)
                    {
                        _memoryTrackingGranular.RegisterAction(address, size, _externalFlushDelegate);
                    }
                    else
                    {
                        _memoryTracking.RegisterAction(_externalFlushDelegate);
                    }
                };

                if (bounded)
                {
                    EnsureRangeList();

                    _modifiedRanges.InheritRanges(from._modifiedRanges, registerRangeAction, Range);
                }
                else if (_modifiedRanges == null)
                {
                    _modifiedRanges = from._modifiedRanges;
                    _modifiedRanges.ReregisterRanges(registerRangeAction);

                    from._modifiedRanges = null;
                }
                else
                {
                    _modifiedRanges.InheritRanges(from._modifiedRanges, registerRangeAction);
                }
            }
        }

        /// <summary>
        /// Determine if a given region of the buffer has been modified, and must be flushed.
        /// </summary>
        /// <param name="range">Memory range of the region</param>
        /// <returns>True if the region has been modified, false otherwise</returns>
        public bool IsModified(MultiRange range)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                if (_modifiedRanges != null && _modifiedRanges.HasRange(subRange.Address, subRange.Size))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Indicate that a region of the buffer was modified, and must be loaded from memory.
        /// </summary>
        /// <param name="mAddress">Start address of the modified region</param>
        /// <param name="mSize">Size of the modified region</param>
        private void RegionModified(ulong mAddress, ulong mSize)
        {
            (mAddress, mSize) = ClampRange(mAddress, mSize);

            if (_modifiedRanges != null)
            {
                _modifiedRanges.ExcludeModifiedRegions(mAddress, mSize, _loadDelegate);
            }
            else
            {
                LoadRegion(mAddress, mSize);
            }
        }

        /// <summary>
        /// Load a region of the buffer from memory.
        /// </summary>
        /// <param name="mAddress">Start address of the modified region</param>
        /// <param name="mSize">Size of the modified region</param>
        private void LoadRegion(ulong mAddress, ulong mSize)
        {
            int offset = Range.FindOffset(new MultiRange(mAddress, mSize));

            _context.Renderer.SetBufferData(Handle, offset, _physicalMemory.GetSpan(mAddress, (int)mSize));
        }

        /// <summary>
        /// Force a region of the buffer to be dirty. Avoids reprotection and nullifies sequence number check.
        /// </summary>
        /// <param name="mAddress">Start address of the modified region</param>
        /// <param name="mSize">Size of the region to force dirty</param>
        public void ForceDirty(ulong mAddress, ulong mSize)
        {
            _modifiedRanges?.Clear(mAddress, mSize);

            if (_useGranular)
            {
                _memoryTrackingGranular.ForceDirty(mAddress, mSize);
            }
            else
            {
                _memoryTracking.ForceDirty();
                _sequenceNumber--;
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
        /// Flushes a range of the buffer.
        /// This writes the range data back into guest memory.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes of the range</param>
        private void Flush(ulong address, ulong size)
        {
            int offset = Range.FindOffset(new MultiRange(address, size));

            ReadOnlySpan<byte> data = _context.Renderer.GetBufferData(Handle, offset, (int)size);

            // TODO: When write tracking shaders, they will need to be aware of changes in overlapping buffers.
            _physicalMemory.WriteUntracked(address, data);
        }

        /// <summary>
        /// Align a given address and size region to page boundaries.
        /// </summary>
        /// <param name="address">The start address of the region</param>
        /// <param name="size">The size of the region</param>
        /// <returns>The page aligned address and size</returns>
        private static (ulong address, ulong size) PageAlign(ulong address, ulong size)
        {
            ulong pageMask = MemoryManager.PageMask;
            ulong rA = address & ~pageMask;
            ulong rS = ((address + size + pageMask) & ~pageMask) - rA;
            return (rA, rS);
        }

        /// <summary>
        /// Flush modified ranges of the buffer from another thread.
        /// This will flush all modifications made before the active SyncNumber was set, and may block to wait for GPU sync.
        /// </summary>
        /// <param name="address">Address of the memory action</param>
        /// <param name="size">Size in bytes</param>
        private void ExternalFlush(ulong address, ulong size)
        {
            _context.Renderer.BackgroundContextAction(() =>
            {
                var ranges = _modifiedRanges;

                if (ranges != null)
                {
                    (address, size) = PageAlign(address, size);
                    ranges.WaitForAndGetRanges(address, size, Flush);
                }
            }, true);
        }

        /// <summary>
        /// An action to be performed when a precise memory access occurs to this resource.
        /// For buffers, this skips flush-on-write by punching holes directly into the modified range list.
        /// </summary>
        /// <param name="address">Address of the memory action</param>
        /// <param name="size">Size in bytes</param>
        /// <param name="write">True if the access was a write, false otherwise</param>
        private bool PreciseAction(ulong address, ulong size, bool write)
        {
            if (!write)
            {
                // We only want to skip flush-on-write.
                return false;
            }

            (address, size) = ClampRange(address, size);

            ForceDirty(address, size);

            return true;
        }

        private (ulong, ulong) ClampRange(ulong address, ulong size)
        {
            if (Range.Count == 1)
            {
                MemoryRange subRange = Range.GetSubRange(0);

                if (address < subRange.Address)
                {
                    address = subRange.Address;
                }

                ulong maxSize = subRange.Address + subRange.Size - address;

                if (size > maxSize)
                {
                    size = maxSize;
                }

                return (address, size);
            }

            ulong endAddress = address + size;

            for (int index = 0; index < Range.Count; index++)
            {
                MemoryRange subRange = Range.GetSubRange(index);

                if (subRange.OverlapsWith(new MemoryRange(address, size)))
                {
                    if (address < subRange.Address)
                    {
                        address = subRange.Address;
                    }

                    if (endAddress > subRange.EndAddress)
                    {
                        endAddress = subRange.EndAddress;
                    }

                    break;
                }
            }

            return (address, endAddress - address);
        }

        /// <summary>
        /// Called when part of the memory for this buffer has been unmapped.
        /// Calls are from non-GPU threads.
        /// </summary>
        /// <param name="address">Start address of the unmapped region</param>
        /// <param name="size">Size of the unmapped region</param>
        public void Unmapped(ulong address, ulong size)
        {
            BufferModifiedRangeList modifiedRanges = _modifiedRanges;

            modifiedRanges?.Clear(address, size);

            UnmappedSequence++;
        }

        /// <summary>
        /// Disposes the host buffer's data, not its tracking handles.
        /// </summary>
        public void DisposeData()
        {
            _modifiedRanges?.Clear();

            _context.Renderer.DeleteBuffer(Handle);

            UnmappedSequence++;
        }

        /// <summary>
        /// Disposes the host buffer.
        /// </summary>
        public void Dispose()
        {
            _memoryTrackingGranular?.Dispose();
            _memoryTracking?.Dispose();

            DisposeData();
        }
    }
}