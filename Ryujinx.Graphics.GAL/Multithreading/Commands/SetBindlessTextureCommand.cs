using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetBindlessTextureCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetBindlessTexture;
        private int _textureId;
        private int _samplerId;
        private TableRef<ITexture> _texture;
        private TableRef<ISampler> _sampler;

        public void Set(int textureId, TableRef<ITexture> texture, int samplerId, TableRef<ISampler> sampler)
        {
            _textureId = textureId;
            _samplerId = samplerId;
            _texture = texture;
            _sampler = sampler;
        }

        public static void Run(ref SetBindlessTextureCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetBindlessTexture(
                command._textureId,
                command._texture.GetAs<ThreadedTexture>(threaded)?.Base,
                command._samplerId,
                command._sampler.GetAs<ThreadedSampler>(threaded)?.Base);
        }
    }
}
