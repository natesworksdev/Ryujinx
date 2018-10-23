using Ryujinx.Graphics.Gal;

namespace Ryujinx.Graphics
{
    public class NvGpu
    {
        public IGalRenderer Renderer { get; private set; }

        public GpuResourceManager ResourceManager { get; private set; }

        public NvGpuFifo Fifo { get; private set; }

        internal NvGpuEngine2D   Engine2D   { get; private set; }
        internal NvGpuEngine3D   Engine3D   { get; private set; }
        internal NvGpuEngineM2Mf EngineM2Mf { get; private set; }
        internal NvGpuEngineP2Mf EngineP2Mf { get; private set; }

        public NvGpu(IGalRenderer renderer)
        {
            this.Renderer = renderer;

            ResourceManager = new GpuResourceManager(this);

            Fifo = new NvGpuFifo(this);

            Engine2D   = new NvGpuEngine2D(this);
            Engine3D   = new NvGpuEngine3D(this);
            EngineM2Mf = new NvGpuEngineM2Mf(this);
            EngineP2Mf = new NvGpuEngineP2Mf(this);
        }
    }
}