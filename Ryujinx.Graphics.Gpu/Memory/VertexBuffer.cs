namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU Vertex Buffer information.
    /// </summary>
    struct VertexBuffer
    {
        public ulong GpuVa;
        public ulong Size;
        public int Stride;
        public int Divisor;
    }
}