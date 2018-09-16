using Ryujinx.Graphics.Gal;

namespace Ryujinx.Graphics
{
    public class NvGpu
    {
        public IGalRenderer Renderer { get; private set; }

        internal GpuResourceManager ResourceManager { get; private set; }

        public NvGpuFifo Fifo { get; private set; }

        public NvGpuEngine2d  Engine2d  { get; private set; }
        public NvGpuEngine3d  Engine3d  { get; private set; }
        public NvGpuEngineDma EngineDma { get; private set; }

        public NvGpu(IGalRenderer Renderer)
        {
            this.Renderer = Renderer;

            ResourceManager = new GpuResourceManager(this);

            Fifo = new NvGpuFifo(this);

            Engine2d  = new NvGpuEngine2d(this);
            Engine3d  = new NvGpuEngine3d(this);
            EngineDma = new NvGpuEngineDma(this);
        }
    }
}