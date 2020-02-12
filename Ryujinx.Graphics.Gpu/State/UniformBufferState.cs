namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Uniform buffer state for the uniform buffer currently being modified.
    /// </summary>
    struct UniformBufferState
    {
#pragma warning disable CS0649
        public int   Size;
#pragma warning restore CS0649
        public GpuVa Address;
#pragma warning disable CS0649
        public int   Offset;
#pragma warning restore CS0649
    }
}
