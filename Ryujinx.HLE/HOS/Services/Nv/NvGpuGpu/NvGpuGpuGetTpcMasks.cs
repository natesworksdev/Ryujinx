namespace Ryujinx.HLE.HOS.Services.Nv.NvGpuGpu
{
    internal struct NvGpuGpuGetTpcMasks
    {
        public int  MaskBufferSize;
        public int  Reserved;
        public long MaskBufferAddress;
        public int  TpcMask;
        public int  Padding;
    }
}
