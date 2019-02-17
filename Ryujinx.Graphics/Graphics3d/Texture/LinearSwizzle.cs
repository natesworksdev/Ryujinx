using System;

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

        public void SetMipLevel(int Level)
        {
            throw new NotImplementedException();
        }

        public int GetMipOffset(int Level)
        {
            throw new NotImplementedException();
        }

        public int GetImageSize(int MipsCount)
        {
            if (MipsCount == 1)
                return SliceSize;
            throw new NotImplementedException();
        }

        public int GetSwizzleOffset(int X, int Y, int Z)
        {
            return Z * SliceSize + X * Bpp + Y * Pitch;
        }
    }
}