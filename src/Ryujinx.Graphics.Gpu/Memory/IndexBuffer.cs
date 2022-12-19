using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU Index Buffer information.
    /// </summary>
    struct IndexBuffer
    {
        public BufferCache BufferCache;
        public ulong Address;
        public ulong Size;

        public IndexType Type;
    }
}
