namespace Ryujinx.Graphics.Texture
{
    class LinearSwizzle : ISwizzle
    {
        private int Pitch;
        private int Bpp;

        private int ZLayer;

        public LinearSwizzle(int Pitch, int Bpp, int Width, int Height)
        {
            this.Pitch  = Pitch;
            this.Bpp    = Bpp;
            this.ZLayer = Width * Height * Bpp;
        }

        public int GetSwizzleOffset(int X, int Y, int Z)
        {
            return Z * ZLayer + X * Bpp + Y * Pitch;
        }
    }
}