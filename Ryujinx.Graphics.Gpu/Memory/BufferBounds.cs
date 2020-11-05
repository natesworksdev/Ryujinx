namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Memory range used for buffers.
    /// </summary>
    struct BufferBounds
    {
        public ulong Address { get; }
        public ulong Size { get; }

        public BufferBounds(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
    }
}