namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Render target depth-stencil buffer state.
    /// </summary>
    struct RtDepthStencilState
    {
        public GpuVa        Address;
        public MemoryLayout MemoryLayout;
#pragma warning disable CS0649
        public RtFormat     Format;
        public int          LayerSize;
#pragma warning restore CS0649
    }
}
