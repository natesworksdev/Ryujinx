using Ryujinx.Graphics.Gal;

namespace Ryujinx.Graphics
{
    public class NvGpu
    {
        public IGalRenderer Renderer { get; private set; }

        public GpuResourceManager ResourceManager { get; private set; }

        public NvGpuFifo Fifo { get; private set; }

        internal NvGpuEngine2d   Engine2D   { get; private set; }
        internal NvGpuEngine3d   Engine3D   { get; private set; }
        internal NvGpuEngineM2mf EngineM2Mf { get; private set; }
        internal NvGpuEngineP2mf EngineP2Mf { get; private set; }

        public NvGpu(IGalRenderer renderer)
        {
            this.Renderer = renderer;

            ResourceManager = new GpuResourceManager(this);

            Fifo = new NvGpuFifo(this);

            Engine2D   = new NvGpuEngine2d(this);
            Engine3D   = new NvGpuEngine3d(this);
            EngineM2Mf = new NvGpuEngineM2mf(this);
            EngineP2Mf = new NvGpuEngineP2mf(this);
        }
    }
}