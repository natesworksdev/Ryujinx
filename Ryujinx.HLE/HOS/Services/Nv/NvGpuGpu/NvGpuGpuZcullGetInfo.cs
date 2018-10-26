namespace Ryujinx.HLE.HOS.Services.Nv.NvGpuGpu
{
    internal struct NvGpuGpuZcullGetInfo
    {
        public int WidthAlignPixels;
        public int HeightAlignPixels;
        public int PixelSquaresByAliquots;
        public int AliquotTotal;
        public int RegionByteMultiplier;
        public int RegionHeaderSize;
        public int SubregionHeaderSize;
        public int SubregionWidthAlignPixels;
        public int SubregionHeightAlignPixels;
        public int SubregionCount;
    }
}
