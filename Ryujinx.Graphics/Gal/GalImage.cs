using Ryujinx.Graphics.Texture;

namespace Ryujinx.Graphics.Gal
{
    public struct GalImage
    {
        public int Width;
        public int Height;
        public int TileWidth;
        public int GobBlockHeight;
        public int Pitch;

        public GalImageFormat   Format;
        public GalMemoryLayout  Layout;
        public GalTextureSource XSource;
        public GalTextureSource YSource;
        public GalTextureSource ZSource;
        public GalTextureSource WSource;

        public GalImage(
            int              width,
            int              height,
            int              tileWidth,
            int              gobBlockHeight,
            GalMemoryLayout  layout,
            GalImageFormat   format,
            GalTextureSource xSource = GalTextureSource.Red,
            GalTextureSource ySource = GalTextureSource.Green,
            GalTextureSource zSource = GalTextureSource.Blue,
            GalTextureSource wSource = GalTextureSource.Alpha)
        {
            this.Width          = width;
            this.Height         = height;
            this.TileWidth      = tileWidth;
            this.GobBlockHeight = gobBlockHeight;
            this.Layout         = layout;
            this.Format         = format;
            this.XSource        = xSource;
            this.YSource        = ySource;
            this.ZSource        = zSource;
            this.WSource        = wSource;

            Pitch = ImageUtils.GetPitch(format, width);
        }

        public bool SizeMatches(GalImage image)
        {
            if (ImageUtils.GetBytesPerPixel(Format) !=
                ImageUtils.GetBytesPerPixel(image.Format))
            {
                return false;
            }

            if (ImageUtils.GetAlignedWidth(this) !=
                ImageUtils.GetAlignedWidth(image))
            {
                return false;
            }

            return Height == image.Height;
        }
    }
}