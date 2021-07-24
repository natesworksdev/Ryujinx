using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Graphics.Vulkan
{
    class StagingBuffer : IDisposable
    {
        private const int BufferSize = 16 * 1024 * 1024;

        private int _freeOffset;
        private int _freeSize;

        private readonly VulkanGraphicsDevice _gd;
        private readonly BufferHolder _buffer;

        private struct PendingCopy
        {
            public FenceHolder Fence { get; }
            public int Size { get; }

            public PendingCopy(FenceHolder fence, int size)
            {
                Fence = fence;
                Size = size;
                fence.Get();
            }
        }

        private readonly Queue<PendingCopy> _pendingCopies;

        public StagingBuffer(VulkanGraphicsDevice gd, BufferManager bufferManager)
        {
            _gd = gd;
            _buffer = bufferManager.Create(gd, BufferSize);
            _pendingCopies = new Queue<PendingCopy>();
            _freeSize = BufferSize;
        }

        public unsafe bool TryPushData(CommandBufferScoped cbs, Action endRenderPass, BufferHolder dst, int dstOffset, ReadOnlySpan<byte> data)
        {
            if (data.Length > BufferSize)
            {
                return false;
            }

            if (_freeSize < data.Length)
            {
                FreeCompleted();

                if (_freeSize < data.Length)
                {
                    return false;
                }
            }

            endRenderPass();

            var srcBuffer = _buffer.GetBuffer();
            var dstBuffer = dst.GetBuffer();

            int offset = _freeOffset;
            int capacity = BufferSize - offset;
            if (capacity < data.Length)
            {
                _buffer.SetDataUnchecked(offset, data.Slice(0, capacity));
                _buffer.SetDataUnchecked(0, data.Slice(capacity));

                BufferHolder.Copy(_gd, cbs, srcBuffer, dstBuffer, offset, dstOffset, capacity);
                BufferHolder.Copy(_gd, cbs, srcBuffer, dstBuffer, 0, dstOffset + capacity, data.Length - capacity);
            }
            else
            {
                _buffer.SetDataUnchecked(offset, data);

                BufferHolder.Copy(_gd, cbs, srcBuffer, dstBuffer, offset, dstOffset, data.Length);
            }

            _freeOffset = (offset + data.Length) & (BufferSize - 1);
            _freeSize -= data.Length;
            Debug.Assert(_freeSize >= 0);

            _pendingCopies.Enqueue(new PendingCopy(cbs.GetFence(), data.Length));

            return true;
        }

        private void FreeCompleted()
        {
            while (_pendingCopies.TryPeek(out var pc) && pc.Fence.IsSignaled())
            {
                var dequeued = _pendingCopies.Dequeue();
                Debug.Assert(dequeued.Fence == pc.Fence);
                _freeSize += pc.Size;
                pc.Fence.Put();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buffer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
