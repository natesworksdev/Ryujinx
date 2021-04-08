using Ryujinx.Graphics.GAL.Multithreading.Model;
using System;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetTransformFeedbackBuffersCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetTransformFeedbackBuffers;
        private SpanRef<BufferRange> _buffers;

        public void Set(SpanRef<BufferRange> buffers)
        {
            _buffers = buffers;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            Span<BufferRange> buffers = _buffers.Get(threaded);
            renderer.Pipeline.SetTransformFeedbackBuffers(threaded.Buffers.MapBufferRanges(buffers));
            _buffers.Dispose(threaded);
        }
    }
}
