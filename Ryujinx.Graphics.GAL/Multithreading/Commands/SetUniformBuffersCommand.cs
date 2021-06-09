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

        public static void Run(ref SetUniformBuffersCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            Span<BufferRange> buffers = command._buffers.Get(threaded);
            renderer.Pipeline.SetUniformBuffers(threaded.Buffers.MapBufferRanges(buffers));
            command._buffers.Dispose(threaded);
        }
    }
}
