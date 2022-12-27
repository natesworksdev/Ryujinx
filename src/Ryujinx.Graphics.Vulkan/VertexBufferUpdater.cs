using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    internal class VertexBufferUpdater
    {
        private int MaxVertexBuffers = 16;
        private VulkanRenderer _gd;

        private uint _baseBinding;
        private uint _count;

        private NativeArray<Buffer> _buffers;
        private NativeArray<ulong> _offsets;
        private NativeArray<ulong> _sizes;
        private NativeArray<ulong> _strides;

        public VertexBufferUpdater(VulkanRenderer gd)
        {
            _gd = gd;

            _buffers = new NativeArray<Buffer>(MaxVertexBuffers);
            _offsets = new NativeArray<ulong>(MaxVertexBuffers);
            _sizes = new NativeArray<ulong>(MaxVertexBuffers);
            _strides = new NativeArray<ulong>(MaxVertexBuffers);
        }

        public void BindVertexBuffer(CommandBufferScoped cbs, uint binding, Buffer buffer, ulong offset, ulong size, ulong stride)
        {
            if (_count == 0)
            {
                _baseBinding = binding;
            }
            else if (_baseBinding + _count != binding)
            {
                Commit(cbs);
                _baseBinding = binding;
            }

            int index = (int)_count;

            _buffers[index] = buffer;
            _offsets[index] = offset;
            _sizes[index] = size;
            _strides[index] = stride;

            _count++;
        }

        public unsafe void Commit(CommandBufferScoped cbs)
        {
            if (_count != 0)
            {
                if (_gd.Capabilities.SupportsExtendedDynamicState)
                {
                    _gd.ExtendedDynamicStateApi.CmdBindVertexBuffers2(
                        cbs.CommandBuffer,
                        _baseBinding,
                        _count,
                        _buffers.Pointer,
                        _offsets.Pointer,
                        _sizes.Pointer,
                        _strides.Pointer);
                }
                else
                {
                    _gd.Api.CmdBindVertexBuffers(cbs.CommandBuffer, _baseBinding, _count, _buffers.Pointer, _offsets.Pointer);
                }

                _count = 0;
            }
        }
    }
}
