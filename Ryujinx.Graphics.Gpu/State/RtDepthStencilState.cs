namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Render target depth-stencil buffer state.
    /// </summary>
    struct RtDepthStencilState
    {
        public GpuVa        Address;
#pragma warning disable CS0649
        public RtFormat     Format;
#pragma warning restore CS0649
        public MemoryLayout MemoryLayout;
#pragma warning disable CS0649
        public int          LayerSize;
#pragma warning restore CS0649
    }
}
