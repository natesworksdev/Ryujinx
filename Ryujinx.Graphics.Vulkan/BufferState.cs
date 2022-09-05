using Silk.NET.Vulkan;
using System;

using BufferHandle = Ryujinx.Graphics.GAL.BufferHandle;

namespace Ryujinx.Graphics.Vulkan
{
    struct BufferState : IDisposable
    {
        public static BufferState Null => new BufferState(null, 0, 0);

        private readonly int _offset;
        private readonly int _size;
        private readonly int _stride;
        private readonly IndexType _type;

        private readonly BufferHandle _handle;
        private readonly Auto<DisposableBuffer> _buffer;

        public BufferState(Auto<DisposableBuffer> buffer, int offset, int size, IndexType type)
        {
            _buffer = buffer;
            _handle = BufferHandle.Null;

            _offset = offset;
            _size = size;
            _stride = 0;
            _type = type;
            buffer?.IncrementReferenceCount();
        }

        public BufferState(Auto<DisposableBuffer> buffer, int offset, int size, int stride = 0)
        {
            _buffer = buffer;
            _handle = BufferHandle.Null;

            _offset = offset;
            _size = size;
            _stride = stride;
            _type = IndexType.Uint16;
            buffer?.IncrementReferenceCount();
        }

        public BufferState(BufferHandle handle, int offset, int size, int stride = 0)
        {
            // This buffer state may be rewritten at bind time, so it must be retrieved on bind.

            _buffer = null;
            _handle = handle;

            _offset = offset;
            _size = size;
            _stride = stride;
            _type = IndexType.Uint16;
        }

        public void BindIndexBuffer(Vk api, CommandBufferScoped cbs)
        {
            if (_buffer != null)
            {
                api.CmdBindIndexBuffer(cbs.CommandBuffer, _buffer.Get(cbs, _offset, _size).Value, (ulong)_offset, _type);
            }
        }

        public void BindTransformFeedbackBuffer(VulkanRenderer gd, CommandBufferScoped cbs, uint binding)
        {
            if (_buffer != null)
            {
                var buffer = _buffer.Get(cbs, _offset, _size).Value;

                gd.TransformFeedbackApi.CmdBindTransformFeedbackBuffers(cbs.CommandBuffer, binding, 1, buffer, (ulong)_offset, (ulong)_size);
            }
        }

        public void BindVertexBuffer(VulkanRenderer gd, CommandBufferScoped cbs, uint binding, int attrScalarAlignment, out int stride)
        {
            var autoBuffer = _buffer;

            if (autoBuffer == null && _handle != BufferHandle.Null)
            {
                // May need to restride the vertex buffer.

                if (gd.NeedsVertexBufferAlignment(attrScalarAlignment, out int alignment) && (_stride % alignment) != 0)
                {
                    autoBuffer = gd.BufferManager.GetAlignedVertexBuffer(cbs, _handle, _offset, _size, _stride, alignment);
                    stride = (_stride + (alignment - 1)) & -alignment;

                    var buffer = autoBuffer.Get(cbs, _offset, _size).Value;

                    if (gd.Capabilities.SupportsExtendedDynamicState)
                    {
                        gd.ExtendedDynamicStateApi.CmdBindVertexBuffers2(
                            cbs.CommandBuffer,
                            binding,
                            1,
                            buffer,
                            0,
                            (ulong)(_size / _stride) * (ulong)stride,
                            (ulong)stride);
                    }
                    else
                    {
                        gd.Api.CmdBindVertexBuffers(cbs.CommandBuffer, binding, 1, buffer, 0);
                    }

                    return;
                }
                else
                {
                    autoBuffer = gd.BufferManager.GetBuffer(cbs.CommandBuffer, _handle, false, out int _);
                }
            }

            stride = _stride;

            if (autoBuffer != null)
            {
                var buffer = autoBuffer.Get(cbs, _offset, _size).Value;

                if (gd.Capabilities.SupportsExtendedDynamicState)
                {
                    gd.ExtendedDynamicStateApi.CmdBindVertexBuffers2(
                        cbs.CommandBuffer,
                        binding,
                        1,
                        buffer,
                        (ulong)_offset,
                        (ulong)_size,
                        (ulong)_stride);
                }
                else
                {
                    gd.Api.CmdBindVertexBuffers(cbs.CommandBuffer, binding, 1, buffer, (ulong)_offset);
                }
            }
        }

        public void Dispose()
        {
            _buffer?.DecrementReferenceCount();
        }
    }
}
