using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetTextureAndSamplerCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetTextureAndSampler;
        private int _binding;
        private TableRef<ITexture> _texture;
        private TableRef<ISampler> _sampler;

        public void Set(int binding, TableRef<ITexture> texture, TableRef<ISampler> sampler)
        {
            _binding = binding;
            _texture = texture;
            _sampler = sampler;
        }

        public static void Run(ref SetTextureAndSamplerCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetTextureAndSampler(command._binding, command._texture.GetAs<ThreadedTexture>(threaded)?.Base, command._sampler.GetAs<ThreadedSampler>(threaded)?.Base);
        }
    }
}
