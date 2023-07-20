using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct RegisterBindlessTextureCommand : IGALCommand, IGALCommand<RegisterBindlessTextureCommand>
    {
        public CommandType CommandType => CommandType.RegisterBindlessTexture;
        private int _textureId;
        private TableRef<ITexture> _texture;
        private float _textureScale;

        public void Set(int textureId, TableRef<ITexture> texture, float textureScale)
        {
            _textureId = textureId;
            _texture = texture;
            _textureScale = textureScale;
        }

        public static void Run(ref RegisterBindlessTextureCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.RegisterBindlessTexture(command._textureId, command._texture.GetAs<ThreadedTexture>(threaded)?.Base, command._textureScale);
        }
    }
}
