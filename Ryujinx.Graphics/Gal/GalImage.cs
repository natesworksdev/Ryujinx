namespace Ryujinx.Graphics.Gal
{
    public struct GalImage
    {
        public int Width;
        public int Height;
        public int Depth;

        public GalImageFormat Format;
        public GalImageTarget Target;

        public GalTextureSource XSource;
        public GalTextureSource YSource;
        public GalTextureSource ZSource;
        public GalTextureSource WSource;

        public GalImage(
            int              Width,
            int              Height,
            int              Depth,
            GalImageFormat   Format,
            GalImageTarget   Target,
            GalTextureSource XSource = GalTextureSource.Red,
            GalTextureSource YSource = GalTextureSource.Green,
            GalTextureSource ZSource = GalTextureSource.Blue,
            GalTextureSource WSource = GalTextureSource.Alpha)
        {
            this.Width   = Width;
            this.Height  = Height;
            this.Depth   = Depth;
            this.Format  = Format;
            this.Target  = Target;
            this.XSource = XSource;
            this.YSource = YSource;
            this.ZSource = ZSource;
            this.WSource = WSource;
        }
    }
}