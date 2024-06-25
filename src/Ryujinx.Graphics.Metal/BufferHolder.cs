using Ryujinx.Graphics.GAL;
using SharpMetal.Metal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    class BufferHolder : IDisposable, IMirrorable<DisposableBuffer>
    {
        private CacheByRange<BufferHolder> _cachedConvertedBuffers;

        public int Size { get; }

        private readonly IntPtr _map;
        private readonly MetalRenderer _renderer;
        private readonly Pipeline _pipeline;

        private readonly MultiFenceHolder _waitable;
        private readonly Auto<DisposableBuffer> _buffer;

        private readonly ReaderWriterLockSlim _flushLock;
        private FenceHolder _flushFence;
        private int _flushWaiting;

        private byte[] _pendingData;
        private BufferMirrorRangeList _pendingDataRanges;
        private Dictionary<ulong, StagingBufferReserved> _mirrors;

        public BufferHolder(MetalRenderer renderer, Pipeline pipeline, MTLBuffer buffer, int size)
        {
            _renderer = renderer;
            _pipeline = pipeline;
            _map = buffer.Contents;
            _waitable = new MultiFenceHolder(size);
            _buffer = new Auto<DisposableBuffer>(new(buffer), this, _waitable);

            _flushLock = new ReaderWriterLockSlim();

            Size = size;
        }

        private static ulong ToMirrorKey(int offset, int size)
        {
            return ((ulong)offset << 32) | (uint)size;
        }

        private static (int offset, int size) FromMirrorKey(ulong key)
        {
            return ((int)(key >> 32), (int)key);
        }

        private unsafe bool TryGetMirror(CommandBufferScoped cbs, ref int offset, int size, out Auto<DisposableBuffer> buffer)
        {
            size = Math.Min(size, Size - offset);

            // Does this binding need to be mirrored?

            if (!_pendingDataRanges.OverlapsWith(offset, size))
            {
                buffer = null;
                return false;
            }

            var key = ToMirrorKey(offset, size);

            if (_mirrors.TryGetValue(key, out StagingBufferReserved reserved))
            {
                buffer = reserved.Buffer.GetBuffer();
                offset = reserved.Offset;

                return true;
            }

            // Is this mirror allowed to exist? Can't be used for write in any in-flight write.
            if (_waitable.IsBufferRangeInUse(offset, size, true))
            {
                // Some of the data is not mirrorable, so upload the whole range.
                ClearMirrors(cbs, offset, size);

                buffer = null;
                return false;
            }

            // Build data for the new mirror.

            var baseData = new Span<byte>((void*)(_map + offset), size);
            var modData = _pendingData.AsSpan(offset, size);

            StagingBufferReserved? newMirror = _renderer.BufferManager.StagingBuffer.TryReserveData(cbs, size);

            if (newMirror != null)
            {
                var mirror = newMirror.Value;
                _pendingDataRanges.FillData(baseData, modData, offset, new Span<byte>((void*)(mirror.Buffer._map + mirror.Offset), size));

                if (_mirrors.Count == 0)
                {
                    _pipeline.RegisterActiveMirror(this);
                }

                _mirrors.Add(key, mirror);

                buffer = mirror.Buffer.GetBuffer();
                offset = mirror.Offset;

                return true;
            }
            else
            {
                // Data could not be placed on the mirror, likely out of space. Force the data to flush.
                ClearMirrors(cbs, offset, size);

                buffer = null;
                return false;
            }
        }

        public Auto<DisposableBuffer> GetBuffer()
        {
            return _buffer;
        }

        public Auto<DisposableBuffer> GetBuffer(bool isWrite)
        {
            if (isWrite)
            {
                SignalWrite(0, Size);
            }

            return _buffer;
        }

        public Auto<DisposableBuffer> GetBuffer(int offset, int size, bool isWrite)
        {
            if (isWrite)
            {
                SignalWrite(offset, size);
            }

            return _buffer;
        }

        public Auto<DisposableBuffer> GetMirrorable(CommandBufferScoped cbs, ref int offset, int size, out bool mirrored)
        {
            if (_pendingData != null && TryGetMirror(cbs, ref offset, size, out Auto<DisposableBuffer> result))
            {
                mirrored = true;
                return result;
            }

            mirrored = false;
            return _buffer;
        }

        public void ClearMirrors()
        {
            // Clear mirrors without forcing a flush. This happens when the command buffer is switched,
            // as all reserved areas on the staging buffer are released.

            if (_pendingData != null)
            {
                _mirrors.Clear();
            }
        }

        public void ClearMirrors(CommandBufferScoped cbs, int offset, int size)
        {
            // Clear mirrors in the given range, and submit overlapping pending data.

            if (_pendingData != null)
            {
                bool hadMirrors = _mirrors.Count > 0 && RemoveOverlappingMirrors(offset, size);

                if (_pendingDataRanges.Count() != 0)
                {
                    UploadPendingData(cbs, offset, size);
                }

                if (hadMirrors)
                {
                    _pipeline.Rebind(_buffer, offset, size);
                }
            }
        }

        private void UploadPendingData(CommandBufferScoped cbs, int offset, int size)
        {
            var ranges = _pendingDataRanges.FindOverlaps(offset, size);

            if (ranges != null)
            {
                _pendingDataRanges.Remove(offset, size);

                foreach (var range in ranges)
                {
                    int rangeOffset = Math.Max(offset, range.Offset);
                    int rangeSize = Math.Min(offset + size, range.End) - rangeOffset;

                    if (_pipeline.Cbs.CommandBuffer == cbs.CommandBuffer)
                    {
                        SetData(rangeOffset, _pendingData.AsSpan(rangeOffset, rangeSize), cbs, _pipeline.EndRenderPassDelegate, false);
                    }
                    else
                    {
                        SetData(rangeOffset, _pendingData.AsSpan(rangeOffset, rangeSize), cbs, null, false);
                    }
                }
            }
        }

        public void SignalWrite(int offset, int size)
        {
            if (offset == 0 && size == Size)
            {
                _cachedConvertedBuffers.Clear();
            }
            else
            {
                _cachedConvertedBuffers.ClearRange(offset, size);
            }
        }

        private void ClearFlushFence()
        {
            // Assumes _flushLock is held as writer.

            if (_flushFence != null)
            {
                if (_flushWaiting == 0)
                {
                    _flushFence.Put();
                }

                _flushFence = null;
            }
        }

        private void WaitForFlushFence()
        {
            if (_flushFence == null)
            {
                return;
            }

            // If storage has changed, make sure the fence has been reached so that the data is in place.
            _flushLock.ExitReadLock();
            _flushLock.EnterWriteLock();

            if (_flushFence != null)
            {
                var fence = _flushFence;
                Interlocked.Increment(ref _flushWaiting);

                // Don't wait in the lock.

                _flushLock.ExitWriteLock();

                fence.Wait();

                _flushLock.EnterWriteLock();

                if (Interlocked.Decrement(ref _flushWaiting) == 0)
                {
                    fence.Put();
                }

                _flushFence = null;
            }

            // Assumes the _flushLock is held as reader, returns in same state.
            _flushLock.ExitWriteLock();
            _flushLock.EnterReadLock();
        }

        public PinnedSpan<byte> GetData(int offset, int size)
        {
            _flushLock.EnterReadLock();

            WaitForFlushFence();

            Span<byte> result;

            if (_map != IntPtr.Zero)
            {
                result = GetDataStorage(offset, size);

                // Need to be careful here, the buffer can't be unmapped while the data is being used.
                _buffer.IncrementReferenceCount();

                _flushLock.ExitReadLock();

                return PinnedSpan<byte>.UnsafeFromSpan(result, _buffer.DecrementReferenceCount);
            }

            throw new InvalidOperationException("The buffer is not mapped");
        }

        public unsafe Span<byte> GetDataStorage(int offset, int size)
        {
            int mappingSize = Math.Min(size, Size - offset);

            if (_map != IntPtr.Zero)
            {
                return new Span<byte>((void*)(_map + offset), mappingSize);
            }

            throw new InvalidOperationException("The buffer is not mapped.");
        }

        public bool RemoveOverlappingMirrors(int offset, int size)
        {
            List<ulong> toRemove = null;
            foreach (var key in _mirrors.Keys)
            {
                (int keyOffset, int keySize) = FromMirrorKey(key);
                if (!(offset + size <= keyOffset || offset >= keyOffset + keySize))
                {
                    toRemove ??= new List<ulong>();

                    toRemove.Add(key);
                }
            }

            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    _mirrors.Remove(key);
                }

                return true;
            }

            return false;
        }

        public unsafe void SetData(int offset, ReadOnlySpan<byte> data, CommandBufferScoped? cbs = null, Action endRenderPass = null, bool allowCbsWait = true)
        {
            int dataSize = Math.Min(data.Length, Size - offset);
            if (dataSize == 0)
            {
                return;
            }

            bool allowMirror = allowCbsWait && cbs != null;

            if (_map != IntPtr.Zero)
            {
                // If persistently mapped, set the data directly if the buffer is not currently in use.
                bool isRented = _buffer.HasRentedCommandBufferDependency(_renderer.CommandBufferPool);

                // If the buffer is rented, take a little more time and check if the use overlaps this handle.
                bool needsFlush = isRented && _waitable.IsBufferRangeInUse(offset, dataSize, false);

                if (!needsFlush)
                {
                    WaitForFences(offset, dataSize);

                    data[..dataSize].CopyTo(new Span<byte>((void*)(_map + offset), dataSize));

                    SignalWrite(offset, dataSize);

                    return;
                }
            }

            // If the buffer does not have an in-flight write (including an inline update), then upload data to a pendingCopy.
            if (allowMirror && !_waitable.IsBufferRangeInUse(offset, dataSize, true))
            {
                if (_pendingData == null)
                {
                    _pendingData = new byte[Size];
                    _mirrors = new Dictionary<ulong, StagingBufferReserved>();
                }

                data[..dataSize].CopyTo(_pendingData.AsSpan(offset, dataSize));
                _pendingDataRanges.Add(offset, dataSize);

                // Remove any overlapping mirrors.
                RemoveOverlappingMirrors(offset, dataSize);

                // Tell the graphics device to rebind any constant buffer that overlaps the newly modified range, as it should access a mirror.
                _pipeline.Rebind(_buffer, offset, dataSize);

                return;
            }

            if (_pendingData != null)
            {
                _pendingDataRanges.Remove(offset, dataSize);
            }

            if (cbs != null &&
                _pipeline.RenderPassActive &&
                !(_buffer.HasCommandBufferDependency(cbs.Value) &&
                  _waitable.IsBufferRangeInUse(cbs.Value.CommandBufferIndex, offset, dataSize)))
            {
                // If the buffer hasn't been used on the command buffer yet, try to preload the data.
                // This avoids ending and beginning render passes on each buffer data upload.

                cbs = _pipeline.PreloadCbs;
                endRenderPass = null;
            }

            if (allowCbsWait)
            {
                _renderer.BufferManager.StagingBuffer.PushData(_renderer.CommandBufferPool, cbs, endRenderPass, this, offset, data);
            }
            else
            {
                bool rentCbs = cbs == null;
                if (rentCbs)
                {
                    cbs = _renderer.CommandBufferPool.Rent();
                }

                if (!_renderer.BufferManager.StagingBuffer.TryPushData(cbs.Value, endRenderPass, this, offset, data))
                {
                    // Need to do a slow upload.
                    BufferHolder srcHolder = _renderer.BufferManager.Create(dataSize);
                    srcHolder.SetDataUnchecked(0, data);

                    var srcBuffer = srcHolder.GetBuffer();
                    var dstBuffer = this.GetBuffer(true);

                    Copy(_pipeline, cbs.Value, srcBuffer, dstBuffer, 0, offset, dataSize);

                    srcHolder.Dispose();
                }

                if (rentCbs)
                {
                    cbs.Value.Dispose();
                }
            }
        }

        public unsafe void SetDataUnchecked(int offset, ReadOnlySpan<byte> data)
        {
            int dataSize = Math.Min(data.Length, Size - offset);
            if (dataSize == 0)
            {
                return;
            }

            if (_map != IntPtr.Zero)
            {
                data[..dataSize].CopyTo(new Span<byte>((void*)(_map + offset), dataSize));
            }
        }

        public void SetDataUnchecked<T>(int offset, ReadOnlySpan<T> data) where T : unmanaged
        {
            SetDataUnchecked(offset, MemoryMarshal.AsBytes(data));
        }

        public static void Copy(
            Pipeline pipeline,
            CommandBufferScoped cbs,
            Auto<DisposableBuffer> src,
            Auto<DisposableBuffer> dst,
            int srcOffset,
            int dstOffset,
            int size,
            bool registerSrcUsage = true)
        {
            var srcBuffer = registerSrcUsage ? src.Get(cbs, srcOffset, size).Value : src.GetUnsafe().Value;
            var dstbuffer = dst.Get(cbs, dstOffset, size, true).Value;

            pipeline.GetOrCreateBlitEncoder().CopyFromBuffer(
                srcBuffer,
                (ulong)srcOffset,
                dstbuffer,
                (ulong)dstOffset,
                (ulong)size);
        }

        public void WaitForFences()
        {
            _waitable.WaitForFences();
        }

        public void WaitForFences(int offset, int size)
        {
            _waitable.WaitForFences(offset, size);
        }

        private bool BoundToRange(int offset, ref int size)
        {
            if (offset >= Size)
            {
                return false;
            }

            size = Math.Min(Size - offset, size);

            return true;
        }

        public Auto<DisposableBuffer> GetBufferI8ToI16(CommandBufferScoped cbs, int offset, int size)
        {
            if (!BoundToRange(offset, ref size))
            {
                return null;
            }

            var key = new I8ToI16CacheKey(_renderer);

            if (!_cachedConvertedBuffers.TryGetValue(offset, size, key, out var holder))
            {
                holder = _renderer.BufferManager.Create((size * 2 + 3) & ~3);

                _renderer.HelperShader.ConvertI8ToI16(cbs, this, holder, offset, size);

                key.SetBuffer(holder.GetBuffer());

                _cachedConvertedBuffers.Add(offset, size, key, holder);
            }

            return holder.GetBuffer();
        }

        public bool TryGetCachedConvertedBuffer(int offset, int size, ICacheKey key, out BufferHolder holder)
        {
            return _cachedConvertedBuffers.TryGetValue(offset, size, key, out holder);
        }

        public void AddCachedConvertedBuffer(int offset, int size, ICacheKey key, BufferHolder holder)
        {
            _cachedConvertedBuffers.Add(offset, size, key, holder);
        }

        public void AddCachedConvertedBufferDependency(int offset, int size, ICacheKey key, Dependency dependency)
        {
            _cachedConvertedBuffers.AddDependency(offset, size, key, dependency);
        }

        public void RemoveCachedConvertedBuffer(int offset, int size, ICacheKey key)
        {
            _cachedConvertedBuffers.Remove(offset, size, key);
        }


        public void Dispose()
        {
            _pipeline.FlushCommandsIfWeightExceeding(_buffer, (ulong)Size);

            _buffer.Dispose();
            _cachedConvertedBuffers.Dispose();

            _flushLock.EnterWriteLock();

            ClearFlushFence();

            _flushLock.ExitWriteLock();
        }
    }
}
