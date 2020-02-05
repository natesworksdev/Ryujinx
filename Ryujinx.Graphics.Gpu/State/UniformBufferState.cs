namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Uniform buffer state for the uniform buffer currently being modified.
    /// </summary>
    struct UniformBufferState
    {
        public GpuVa Address;
#pragma warning disable CS0649
        public int   Size;
        public int   Offset;
#pragma warning restore CS0649
    }
}
