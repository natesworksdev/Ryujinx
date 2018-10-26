using System;

namespace Ryujinx.Graphics.Texture
{
    internal class BlockLinearSwizzle : ISwizzle
    {
        private int _bhShift;
        private int _bppShift;
        private int _bhMask;

        private int _xShift;
        private int _gobStride;

        public BlockLinearSwizzle(int width, int bpp, int blockHeight = 16)
        {
            _bhMask = blockHeight * 8 - 1;

            _bhShift  = CountLsbZeros(blockHeight * 8);
            _bppShift = CountLsbZeros(bpp);

            int widthInGobs = (int)MathF.Ceiling(width * bpp / 64f);

            _gobStride = 512 * blockHeight * widthInGobs;

            _xShift = CountLsbZeros(512 * blockHeight);
        }

        private int CountLsbZeros(int value)
        {
            int count = 0;

            while (((value >> count) & 1) == 0) count++;

            return count;
        }

        public int GetSwizzleOffset(int x, int y)
        {
            x <<= _bppShift;

            int position = (y >> _bhShift) * _gobStride;

            position += (x >> 6) << _xShift;

            position += ((y & _bhMask) >> 3) << 9;

            position += ((x & 0x3f) >> 5) << 8;
            position += ((y & 0x07) >> 1) << 6;
            position += ((x & 0x1f) >> 4) << 5;
            position += ((y & 0x01) >> 0) << 4;
            position += ((x & 0x0f) >> 0) << 0;

            return position;
        }
    }
}