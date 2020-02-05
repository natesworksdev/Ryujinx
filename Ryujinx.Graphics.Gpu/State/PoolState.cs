namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Texture or sampler pool state.
    /// </summary>
    struct PoolState
    {
        public GpuVa Address;
#pragma warning disable CS0649
        public int   MaximumId;
#pragma warning restore CS0649
    }
}