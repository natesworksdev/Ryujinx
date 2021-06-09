using Ryujinx.Graphics.GAL.Multithreading.Model;
using System;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetStorageBuffersCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetStorageBuffers;
        private SpanRef<BufferRange> _buffers;

        public void Set(SpanRef<BufferRange> buffers)
        {
            _buffers = buffers;
        }

        public static void Run(ref SetStorageBuffersCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            Span<BufferRange> buffers = command._buffers.Get(threaded);
            renderer.Pipeline.SetStorageBuffers(threaded.Buffers.MapBufferRanges(buffers));
            command._buffers.Dispose(threaded);
        }
    }
}
