using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct RegisterBindlessTextureAndSamplerCommand : IGALCommand, IGALCommand<RegisterBindlessTextureAndSamplerCommand>
    {
        public CommandType CommandType => CommandType.RegisterBindlessTextureAndSampler;
        private int _textureId;
        private int _samplerId;
        private TableRef<ITexture> _texture;
        private TableRef<ISampler> _sampler;
        private float _textureScale;

        public void Set(int textureId, TableRef<ITexture> texture, float textureScale, int samplerId, TableRef<ISampler> sampler)
        {
            _textureId = textureId;
            _samplerId = samplerId;
            _textureScale = textureScale;
            _texture = texture;
            _sampler = sampler;
        }

        public static void Run(ref RegisterBindlessTextureAndSamplerCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.RegisterBindlessTextureAndSampler(
                command._textureId,
                command._texture.GetAs<ThreadedTexture>(threaded)?.Base,
                command._textureScale,
                command._samplerId,
                command._sampler.GetAs<ThreadedSampler>(threaded)?.Base);
        }
    }
}
