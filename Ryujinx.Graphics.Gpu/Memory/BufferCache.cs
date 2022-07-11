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
        private const int OverlapsBufferInitialCapacity = 10;
        private const int OverlapsBufferMaxCapacity = 10000;

        private readonly GpuContext _context;
        private readonly PhysicalMemory _physicalMemory;

        private readonly RangeList<BufferView> _buffers;

        private BufferView[] _bufferOverlaps;

        /// <summary>
        /// Event that notifies when any buffer is deleted on the cache, and bindings needs to be updated due to that.
        /// </summary>
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

            _buffers = new RangeList<BufferView>();

            _bufferOverlaps = new BufferView[OverlapsBufferInitialCapacity];
        }

        /// <summary>
        /// Tries to find an existing buffer on the cache, and if none is found, a new one is created.
        /// </summary>
        /// <param name="address">Address of the buffer to find or create</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="offset">If the buffer already exists, this is the offset where <paramref name="address"/> starts inside the buffer</param>
        /// <returns>The buffer that fully contains the specified memory range</returns>
        public Buffer FindOrCreateBuffer(ulong address, ulong size, out int offset)
        {
            ulong requestedAddress = address;
            ulong endAddress = address + size;

            BufferView[] overlaps = _bufferOverlaps;

            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);

            if (overlapsCount == 1 && overlaps[0].Address <= address && overlaps[0].EndAddress >= endAddress)
            {
                offset = (int)(address - overlaps[0].Address);
                return overlaps[0].Buffer;
            }

            for (int index = 0; index < overlapsCount; index++)
            {
                ref BufferView view = ref overlaps[index];

                address = Math.Min(address, view.Address);
                endAddress = Math.Max(endAddress, view.EndAddress);
            }

            size = endAddress - address;

            MultiRange range = new MultiRange(address, size);
            var handles = overlaps.Take(overlapsCount).SelectMany(x => x.Buffer.GetTrackingHandles(x.Address - address));
            Buffer buffer = new Buffer(_context, _physicalMemory, range, handles);

            _buffers.Add(new BufferView(address, size, 0, isVirtual: false, buffer));

            for (int index = 0; index < overlapsCount; index++)
            {
                ref BufferView view = ref overlaps[index];
                Buffer overlap = view.Buffer;

                ulong offsetWithinOverlap = view.Address - address;

                overlap.CopyTo(buffer, (int)offsetWithinOverlap);
                _buffers.Remove(view);
                buffer.InheritModifiedRanges(overlap, offsetWithinOverlap);

                overlap.DisposeData();
                overlap.UpdateViews(buffer, (int)offsetWithinOverlap);
            }

            if (overlapsCount != 0)
            {
                buffer.SynchronizeMemory(0, size);

                // Existing buffers were modified, we need to rebind everything.
                NotifyBuffersModified?.Invoke();
            }

            ShrinkOverlapsBufferIfNeeded();
            offset = (int)(requestedAddress - address);
            return buffer;
        }

        /// <summary>
        /// Tries to create a new buffer for the specified memory range, if none exists.
        /// </summary>
        /// <param name="address">Address of the buffer to create</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="baseHandles">Tracking handles to be inherited by the new buffer</param>
        /// <returns>The new buffer, or null if none was created because it already exists</returns>
        public Buffer TryCreateBuffer(ulong address, ulong size, IEnumerable<RegionHandleSegment> baseHandles)
        {
            BufferView[] overlaps = _bufferOverlaps;

            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);

            Buffer buffer = null;

            if (overlapsCount == 0)
            {
                MultiRange range = new MultiRange(address, size);
                buffer = new Buffer(_context, _physicalMemory, range, baseHandles);

                _buffers.Add(new BufferView(address, size, 0, isVirtual: false, buffer));
            }

            ShrinkOverlapsBufferIfNeeded();
            return buffer;
        }

        /// <summary>
        /// Removes a buffer from the cache.
        /// </summary>
        /// <remarks>
        /// This will remove all buffers that overlaps the specified range.
        /// </remarks>
        /// <param name="address">Address of the buffer to remove</param>
        /// <param name="size">Size of the buffer to remove in bytes</param>
        /// <param name="dataOnly">True to only delete the storage, false to delete everything</param>
        public void RemoveBuffer(ulong address, ulong size, bool dataOnly)
        {
            BufferView[] overlaps = _bufferOverlaps;

            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);

            for (int index = 0; index < overlapsCount; index++)
            {
                ref BufferView view = ref overlaps[index];

                _buffers.Remove(view);

                if (dataOnly)
                {
                    view.Buffer.DisposeData();
                }
                else
                {
                    view.Buffer.Dispose();
                }
            }

            if (overlapsCount != 0)
            {
                NotifyBuffersModified?.Invoke();
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
        /// Forces buffer bindings to be updated. Should be called after any operation that deletes buffers.
        /// </summary>
        public void ForceBindingsUpdate()
        {
            NotifyBuffersModified?.Invoke();
        }

        /// <summary>
        /// Disposes all buffers in the cache.
        /// It's an error to use the buffer cache after disposal.
        /// </summary>
        public void Dispose()
        {
            foreach (BufferView view in _buffers)
            {
                view.Buffer.Dispose();
            }
        }
    }
}