using Ryujinx.Common.Pools;
using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;
using System;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands.Texture
{
    struct TextureSetDataSliceCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.TextureSetDataSlice;
        private TableRef<ThreadedTexture> _texture;
        private TableRef<PooledBuffer<byte>> _data;
        private int _layer;
        private int _level;

        public void Set(TableRef<ThreadedTexture> texture, TableRef<PooledBuffer<byte>> data, int layer, int level)
        {
            _texture = texture;
            _data = data;
            _layer = layer;
            _level = level;
        }

        public static void Run(ref TextureSetDataSliceCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            ThreadedTexture texture = command._texture.Get(threaded);
            using (PooledBuffer<byte> pooledData = command._data.Get(threaded))
            {
                texture.Base.SetData(pooledData.AsReadOnlySpan, command._layer, command._level);
            }
        }
    }
}
