namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Buffer to texture copy parameters.
    /// </summary>
    struct CopyBufferTexture
    {
        public MemoryLayout MemoryLayout;
#pragma warning disable CS0649
        public int          Width;
        public int          Height;
        public int          Depth;
        public int          RegionZ;
        public ushort       RegionX;
        public ushort       RegionY;
#pragma warning restore CS0649
    }
}