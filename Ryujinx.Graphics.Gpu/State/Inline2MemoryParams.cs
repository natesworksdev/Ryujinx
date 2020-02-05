namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// Inline-to-memory copy parameters.
    /// </summary>
    struct Inline2MemoryParams
    {
        public MemoryLayout DstMemoryLayout;
        public GpuVa        DstAddress;
#pragma warning disable CS0649
        public int          DstStride;
        public int          DstWidth;
        public int          DstHeight;
        public int          DstDepth;
        public int          DstZ;
        public int          DstX;
        public int          DstY;
        public int          LineLengthIn;
        public int          LineCount;
#pragma warning restore CS0649
    }
}