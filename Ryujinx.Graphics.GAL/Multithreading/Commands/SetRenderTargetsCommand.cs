using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;
using System.Linq;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetRenderTargetsCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetRenderTargets;
        private TableRef<ITexture[]> _colors;
        private TableRef<ITexture> _depthStencil;

        public void Set(TableRef<ITexture[]> colors, TableRef<ITexture> depthStencil)
        {
            _colors = colors;
            _depthStencil = depthStencil;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetRenderTargets(_colors.Get(threaded).Select(color => ((ThreadedTexture)color)?.Base).ToArray(), ((ThreadedTexture)_depthStencil.Get(threaded))?.Base);
        }
    }
}
