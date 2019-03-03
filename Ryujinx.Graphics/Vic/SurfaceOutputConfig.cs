namespace Ryujinx.Graphics.Vic
{
    struct SurfaceOutputConfig
    {
        public SurfacePixelFormat PixelFormat;

        public int SurfaceWidth;
        public int SurfaceHeight;
        public int GobBlockHeight;

        public long SurfaceLumaAddress;
        public long SurfaceChromaUAddress;
        public long SurfaceChromaVAddress;

        public SurfaceOutputConfig(
            SurfacePixelFormat pixelFormat,
            int                surfaceWidth,
            int                surfaceHeight,
            int                gobBlockHeight,
            long               outputSurfaceLumaAddress,
            long               outputSurfaceChromaUAddress,
            long               outputSurfaceChromaVAddress)
        {
            this.PixelFormat           = pixelFormat;
            this.SurfaceWidth          = surfaceWidth;
            this.SurfaceHeight         = surfaceHeight;
            this.GobBlockHeight        = gobBlockHeight;
            this.SurfaceLumaAddress    = outputSurfaceLumaAddress;
            this.SurfaceChromaUAddress = outputSurfaceChromaUAddress;
            this.SurfaceChromaVAddress = outputSurfaceChromaVAddress;
        }
    }
}