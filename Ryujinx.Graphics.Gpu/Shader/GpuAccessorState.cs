using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// State used by the <see cref="GpuAccessor"/>.
    /// </summary>
    struct GpuAccessorState
    {
        /// <summary>
        /// GPU virtual address of the texture pool.
        /// </summary>
        public ulong TexturePoolGpuVa { get; }

        /// <summary>
        /// Maximum ID of the texture pool.
        /// </summary>
        public int TexturePoolMaximumId { get; }

        /// <summary>
        /// Constant buffer slot where the texture handles are located.
        /// </summary>
        public int TextureBufferIndex { get; }

        /// <summary>
        /// Early Z force enable.
        /// </summary>
        public bool EarlyZForce { get; }

        /// <summary>
        /// Depth mode zero to one or minus one to one.
        /// </summary>
        public bool DepthMode { get; }

        /// <summary>
        /// Indicates if the point size is set on the shader or is fixed.
        /// </summary>
        public bool ProgramPointSizeEnable { get; }

        /// <summary>
        /// Point size if not set from shader.
        /// </summary>
        public float PointSize { get; }

        /// <summary>
        /// Primitive topology of current draw.
        /// </summary>
        public PrimitiveTopology Topology { get; }

        /// <summary>
        /// Creates a new instance of the GPU accessor state.
        /// </summary>
        /// <param name="texturePoolGpuVa">GPU virtual address of the texture pool</param>
        /// <param name="texturePoolMaximumId">Maximum ID of the texture pool</param>
        /// <param name="textureBufferIndex">Constant buffer slot where the texture handles are located</param>
        /// <param name="earlyZForce">Early Z force enable</param>
        /// <param name="depthMode">Depth mode zero to one or minus one to one</param>
        /// <param name="programPointSizeEnable">Indicates if the point size is set on the shader or is fixed</param>
        /// <param name="pointSize">Point size if not set from shader</param>
        /// <param name="topology">Primitive topology</param>
        public GpuAccessorState(
            ulong texturePoolGpuVa,
            int texturePoolMaximumId,
            int textureBufferIndex,
            bool earlyZForce,
            bool depthMode,
            bool programPointSizeEnable,
            float pointSize,
            PrimitiveTopology topology)
        {
            TexturePoolGpuVa = texturePoolGpuVa;
            TexturePoolMaximumId = texturePoolMaximumId;
            TextureBufferIndex = textureBufferIndex;
            EarlyZForce = earlyZForce;
            DepthMode = depthMode;
            ProgramPointSizeEnable = programPointSizeEnable;
            PointSize = pointSize;
            Topology = topology;
        }
    }
}