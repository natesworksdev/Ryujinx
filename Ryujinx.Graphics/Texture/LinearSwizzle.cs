namespace Ryujinx.Graphics.Texture
{
    internal class LinearSwizzle : ISwizzle
    {
        private int _pitch;
        private int _bpp;

        public LinearSwizzle(int pitch, int bpp)
        {
            _pitch = pitch;
            _bpp   = bpp;
        }

        public int GetSwizzleOffset(int x, int y)
        {
            return x * _bpp + y * _pitch;
        }
    }
}