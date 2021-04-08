using Ryujinx.Graphics.GAL.Multithreading.Model;
using System;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetUniformBuffersCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetUniformBuffers;
        private SpanRef<BufferRange> _buffers;

        public void Set(SpanRef<BufferRange> buffers)
        {
            _buffers = buffers;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            Span<BufferRange> buffers = _buffers.Get(threaded);
            renderer.Pipeline.SetUniformBuffers(threaded.Buffers.MapBufferRanges(buffers));
            _buffers.Dispose(threaded);
        }
    }
}
