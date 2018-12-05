namespace Ryujinx.Graphics.Texture
{
    class LinearSwizzle : ISwizzle
    {
        private int Pitch;
        private int Bpp;

        private int SliceSize;

        public LinearSwizzle(int Pitch, int Bpp, int Width, int Height)
        {
            this.Pitch  = Pitch;
            this.Bpp    = Bpp;
            SliceSize   = Width * Height * Bpp;
        }

        public int GetSwizzleOffset(int X, int Y, int Z)
        {
            return Z * SliceSize + X * Bpp + Y * Pitch;
        }
    }
}