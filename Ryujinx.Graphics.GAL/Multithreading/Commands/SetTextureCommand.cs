using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetTextureCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetTexture;
        private int _binding;
        private TableRef<ITexture> _texture;

        public void Set(int binding, TableRef<ITexture> texture)
        {
            _binding = binding;
            _texture = texture;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetTexture(_binding, ((ThreadedTexture)_texture.Get(threaded))?.Base);
        }
    }
}
