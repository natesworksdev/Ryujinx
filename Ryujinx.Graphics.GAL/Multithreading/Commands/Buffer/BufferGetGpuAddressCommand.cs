using Ryujinx.Graphics.GAL.Multithreading.Model;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands.Buffer
{
    struct BufferGetGpuAddressCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.BufferGetGpuAddress;
        private BufferHandle _buffer;
        private TableRef<ResultBox<ulong>> _result;

        public void Set(BufferHandle buffer, TableRef<ResultBox<ulong>> result)
        {
            _buffer = buffer;
            _result = result;
        }

        public static void Run(ref BufferGetGpuAddressCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            ulong result = renderer.GetBufferGpuAddress(threaded.Buffers.MapBuffer(command._buffer));

            command._result.Get(threaded).Result = result;
        }
    }
}
