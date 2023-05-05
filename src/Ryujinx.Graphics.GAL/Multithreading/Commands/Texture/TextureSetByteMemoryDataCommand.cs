using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;
using System.Buffers;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands.Texture
{
    struct TextureSetByteMemoryDataCommand : IGALCommand, IGALCommand<TextureSetByteMemoryDataCommand>
    {
        public CommandType CommandType => CommandType.TextureSetByteMemoryData;
        private TableRef<ThreadedTexture> _texture;
        private TableRef<IMemoryOwner<byte>> _data;

        public void Set(TableRef<ThreadedTexture> texture, TableRef<IMemoryOwner<byte>> data)
        {
            _texture = texture;
            _data = data;
        }

        public static void Run(ref TextureSetByteMemoryDataCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            ThreadedTexture texture = command._texture.Get(threaded);
            IMemoryOwner<byte> data = command._data.Get(threaded);
            texture.Base.SetData(data);
            data.Dispose();
        }
    }
}
