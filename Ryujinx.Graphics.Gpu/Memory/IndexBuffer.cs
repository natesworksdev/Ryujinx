
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU Index Buffer information.
    /// </summary>
    struct IndexBuffer
    {
        public ulong GpuVa;
        public ulong Size;
        public IndexType Type;
    }
}