using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    /// <summary>
    /// Draw state.
    /// </summary>
    class DrawState
    {
        /// <summary>
        /// First index to be used for the draw on the index buffer.
        /// </summary>
        public int FirstIndex;

        /// <summary>
        /// Number of indices to be used for the draw on the index buffer.
        /// </summary>
        public int IndexCount;

        /// <summary>
        /// Indicates if the next draw will be a indexed draw.
        /// </summary>
        public bool DrawIndexed;

        /// <summary>
        /// Indicates if any of the currently used vertex shaders reads the instance ID.
        /// </summary>
        public bool VsUsesInstanceId;

        /// <summary>
        /// Indicates if any of the currently used vertex buffers is instanced.
        /// </summary>
        public bool IsAnyVbInstanced;

        /// <summary>
        /// Indicates that the draw is writing the base vertex and base instance to Constant Buffer 0.
        /// </summary>
        public bool HasConstantBufferBaseIds;

        /// <summary>
        /// Primitive topology for the next draw.
        /// </summary>
        public PrimitiveTopology Topology;

        /// <summary>
        /// Index buffer data streamer for inline index buffer updates, such as those used in legacy OpenGL.
        /// </summary>
        public IbStreamer IbStreamer = new IbStreamer();
    }
}
