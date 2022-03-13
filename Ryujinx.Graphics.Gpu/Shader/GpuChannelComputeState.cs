using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.Threed;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// State used by the <see cref="GpuAccessor"/>.
    /// </summary>
    struct GpuChannelComputeState
    {
        public readonly int LocalSizeX;
        public readonly int LocalSizeY;
        public readonly int LocalSizeZ;
        public readonly int LocalMemorySize;
        public readonly int SharedMemorySize;

        public GpuChannelComputeState(
            int localSizeX,
            int localSizeY,
            int localSizeZ,
            int localMemorySize,
            int sharedMemorySize)
        {
            LocalSizeX = localSizeX;
            LocalSizeY = localSizeY;
            LocalSizeZ = localSizeZ;
            LocalMemorySize = localMemorySize;
            SharedMemorySize = sharedMemorySize;
        }
    }
}