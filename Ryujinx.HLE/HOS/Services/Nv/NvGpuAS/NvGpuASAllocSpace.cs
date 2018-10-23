namespace Ryujinx.HLE.HOS.Services.Nv.NvGpuAS
{
    struct NvGpuAsAllocSpace
    {
        public int  Pages;
        public int  PageSize;
        public int  Flags;
        public int  Padding;
        public long Offset;
    }
}