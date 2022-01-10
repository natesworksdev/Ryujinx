using Ryujinx.Common;
using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Texture
{
    public static class BCnDecoder
    {
        private const int BlockWidth = 4;
        private const int BlockHeight = 4;

        public static byte[] DecodeBC1(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers)
        {
            int size = 0;

            for (int l = 0; l < levels; l++)
            {
                size += Math.Max(1, width >> l) * Math.Max(1, height >> l) * Math.Max(1, depth >> l) * layers * 4;
            }

            byte[] output = new byte[size];

            Span<byte> tile = stackalloc byte[BlockWidth * BlockHeight * 4];

            int baseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int w = BitUtils.DivRoundUp(width, BlockWidth);
                int h = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int baseY = y * BlockHeight;

                            for (int x = 0; x < w; x++)
                            {
                                int baseX = x * BlockWidth;
                                int lineBaseOOffs = baseOOffs + baseX;

                                BCnDecodeTile(tile, data, 0, bc1: true);

                                for (int texel = 0; texel < BlockWidth * BlockHeight; texel++)
                                {
                                    int tX = texel & 3;
                                    int tY = texel >> 2;

                                    if (baseX + tX >= width || baseY + tY >= height)
                                    {
                                        continue;
                                    }

                                    int oOffs = (lineBaseOOffs + tY * width + tX) * 4;

                                    output[oOffs + 0] = tile[texel * 4];
                                    output[oOffs + 1] = tile[texel * 4 + 1];
                                    output[oOffs + 2] = tile[texel * 4 + 2];
                                    output[oOffs + 3] = tile[texel * 4 + 3];
                                }

                                data = data.Slice(8);
                            }

                            baseOOffs += width * (baseY + BlockHeight > height ? (height & (BlockHeight - 1)) : BlockHeight);
                        }
                    }
                }

                width  = Math.Max(1, width  >> 1);
                height = Math.Max(1, height >> 1);
                depth  = Math.Max(1, depth  >> 1);
            }

            return output;
        }

        public static byte[] DecodeBC2(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers)
        {
            int size = 0;

            for (int l = 0; l < levels; l++)
            {
                size += Math.Max(1, width >> l) * Math.Max(1, height >> l) * Math.Max(1, depth >> l) * layers * 4;
            }

            byte[] output = new byte[size];

            Span<byte> tile = stackalloc byte[BlockWidth * BlockHeight * 4];
            Span<byte> rPal = stackalloc byte[8];

            int baseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int w = BitUtils.DivRoundUp(width, BlockWidth);
                int h = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int baseY = y * BlockHeight;

                            for (int x = 0; x < w; x++)
                            {
                                int baseX = x * BlockWidth;
                                int lineBaseOOffs = baseOOffs + baseX;

                                BCnDecodeTile(tile, data.Slice(8), 0, bc1: false);

                                ulong block = BinaryPrimitives.ReadUInt64LittleEndian(data);

                                rPal[0] = (byte)block;
                                rPal[1] = (byte)(block >> 8);

                                CalculateBC3Alpha(rPal);

                                ulong rI = block >> 16;

                                for (int texel = 0; texel < BlockWidth * BlockHeight; texel++)
                                {
                                    int tX = texel & 3;
                                    int tY = texel >> 2;

                                    if (baseX + tX >= width || baseY + tY >= height)
                                    {
                                        continue;
                                    }

                                    int shift = texel * 4;

                                    int oOffs = (lineBaseOOffs + tY * width + tX) * 4;

                                    output[oOffs + 0] = tile[texel * 4];
                                    output[oOffs + 1] = tile[texel * 4 + 1];
                                    output[oOffs + 2] = tile[texel * 4 + 2];
                                    output[oOffs + 3] = (byte)(((rI >> shift) & 0xf) * 0x11);
                                }

                                data = data.Slice(16);
                            }

                            baseOOffs += width * (baseY + BlockHeight > height ? (height & (BlockHeight - 1)) : BlockHeight);
                        }
                    }
                }

                width  = Math.Max(1, width  >> 1);
                height = Math.Max(1, height >> 1);
                depth  = Math.Max(1, depth  >> 1);
            }

            return output;
        }

        public static byte[] DecodeBC3(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers)
        {
            int size = 0;

            for (int l = 0; l < levels; l++)
            {
                size += Math.Max(1, width >> l) * Math.Max(1, height >> l) * Math.Max(1, depth >> l) * layers * 4;
            }

            byte[] output = new byte[size];

            Span<byte> tile = stackalloc byte[BlockWidth * BlockHeight * 4];
            Span<byte> rPal = stackalloc byte[8];

            int baseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int w = BitUtils.DivRoundUp(width, BlockWidth);
                int h = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int baseY = y * BlockHeight;

                            for (int x = 0; x < w; x++)
                            {
                                int baseX = x * BlockWidth;
                                int lineBaseOOffs = baseOOffs + baseX;

                                BCnDecodeTile(tile, data.Slice(8), 0, bc1: false);

                                ulong block = BinaryPrimitives.ReadUInt64LittleEndian(data);

                                rPal[0] = (byte)block;
                                rPal[1] = (byte)(block >> 8);

                                CalculateBC3Alpha(rPal);

                                ulong rI = block >> 16;

                                for (int texel = 0; texel < BlockWidth * BlockHeight; texel++)
                                {
                                    int tX = texel & 3;
                                    int tY = texel >> 2;

                                    if (baseX + tX >= width || baseY + tY >= height)
                                    {
                                        continue;
                                    }

                                    int shift = texel * 3;

                                    int oOffs = (lineBaseOOffs + tY * width + tX) * 4;

                                    output[oOffs + 0] = tile[texel * 4];
                                    output[oOffs + 1] = tile[texel * 4 + 1];
                                    output[oOffs + 2] = tile[texel * 4 + 2];
                                    output[oOffs + 3] = rPal[(int)((rI >> shift) & 7)];
                                }

                                data = data.Slice(16);
                            }

                            baseOOffs += width * (baseY + BlockHeight > height ? (height & (BlockHeight - 1)) : BlockHeight);
                        }
                    }
                }

                width  = Math.Max(1, width  >> 1);
                height = Math.Max(1, height >> 1);
                depth  = Math.Max(1, depth  >> 1);
            }

            return output;
        }

        public static byte[] DecodeBC4(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers, bool signed)
        {
            int size = 0;

            for (int l = 0; l < levels; l++)
            {
                size += Math.Max(1, width >> l) * Math.Max(1, height >> l) * Math.Max(1, depth >> l) * layers;
            }

            byte[] output = new byte[size];

            ReadOnlySpan<ulong> data64 = MemoryMarshal.Cast<byte, ulong>(data);

            Span<byte> rPal = stackalloc byte[8];

            int baseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int w = BitUtils.DivRoundUp(width, BlockWidth);
                int h = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int baseY = y * BlockHeight;

                            for (int x = 0; x < w; x++)
                            {
                                int baseX = x * BlockWidth;
                                int lineBaseOOffs = baseOOffs + baseX;

                                ulong block = data64[0];

                                rPal[0] = (byte)block;
                                rPal[1] = (byte)(block >> 8);

                                if (signed)
                                {
                                    CalculateBC3AlphaS(rPal);
                                }
                                else
                                {
                                    CalculateBC3Alpha(rPal);
                                }

                                ulong rI = block >> 16;

                                for (int texel = 0; texel < BlockWidth * BlockHeight; texel++)
                                {
                                    int tX = texel & 3;
                                    int tY = texel >> 2;

                                    if (baseX + tX >= width || baseY + tY >= height)
                                    {
                                        continue;
                                    }

                                    int shift = texel * 3;

                                    byte r = rPal[(int)((rI >> shift) & 7)];

                                    int oOffs = lineBaseOOffs + tY * width + tX;

                                    output[oOffs] = r;
                                }

                                data64 = data64.Slice(1);
                            }

                            baseOOffs += width * (baseY + BlockHeight > height ? (height & (BlockHeight - 1)) : BlockHeight);
                        }
                    }
                }

                width  = Math.Max(1, width  >> 1);
                height = Math.Max(1, height >> 1);
                depth  = Math.Max(1, depth  >> 1);
            }

            return output;
        }

        public static byte[] DecodeBC5(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers, bool signed)
        {
            int size = 0;

            for (int l = 0; l < levels; l++)
            {
                size += Math.Max(1, width >> l) * Math.Max(1, height >> l) * Math.Max(1, depth >> l) * layers * 2;
            }

            byte[] output = new byte[size];

            ReadOnlySpan<ulong> data64 = MemoryMarshal.Cast<byte, ulong>(data);

            Span<byte> rPal = stackalloc byte[8];
            Span<byte> gPal = stackalloc byte[8];

            int baseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int w = BitUtils.DivRoundUp(width, BlockWidth);
                int h = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int baseY = y * BlockHeight;

                            for (int x = 0; x < w; x++)
                            {
                                int baseX = x * BlockWidth;
                                int lineBaseOOffs = baseOOffs + baseX;

                                ulong blockL = data64[0];
                                ulong blockH = data64[1];

                                rPal[0] = (byte)blockL;
                                rPal[1] = (byte)(blockL >> 8);
                                gPal[0] = (byte)blockH;
                                gPal[1] = (byte)(blockH >> 8);

                                if (signed)
                                {
                                    CalculateBC3AlphaS(rPal);
                                    CalculateBC3AlphaS(gPal);
                                }
                                else
                                {
                                    CalculateBC3Alpha(rPal);
                                    CalculateBC3Alpha(gPal);
                                }

                                ulong rI = blockL >> 16;
                                ulong gI = blockH >> 16;

                                for (int texel = 0; texel < BlockWidth * BlockHeight; texel++)
                                {
                                    int tX = texel & 3;
                                    int tY = texel >> 2;

                                    if (baseX + tX >= width || baseY + tY >= height)
                                    {
                                        continue;
                                    }

                                    int shift = texel * 3;

                                    byte r = rPal[(int)((rI >> shift) & 7)];
                                    byte g = gPal[(int)((gI >> shift) & 7)];

                                    int oOffs = (lineBaseOOffs + tY * width + tX) * 2;

                                    output[oOffs + 0] = r;
                                    output[oOffs + 1] = g;
                                }

                                data64 = data64.Slice(2);
                            }

                            baseOOffs += width * (baseY + BlockHeight > height ? (height & (BlockHeight - 1)) : BlockHeight);
                        }
                    }
                }

                width  = Math.Max(1, width  >> 1);
                height = Math.Max(1, height >> 1);
                depth  = Math.Max(1, depth  >> 1);
            }

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateBC3Alpha(Span<byte> alpha)
        {
            for (int i = 2; i < 8; i++)
            {
                if (alpha[0] > alpha[1])
                {
                    alpha[i] = (byte)(((8 - i) * alpha[0] + (i - 1) * alpha[1]) / 7);
                }
                else if (i < 6)
                {
                    alpha[i] = (byte)(((6 - i) * alpha[0] + (i - 1) * alpha[1]) / 7);
                }
                else if (i == 6)
                {
                    alpha[i] = 0;
                }
                else /* i == 7 */
                {
                    alpha[i] = 0xff;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateBC3AlphaS(Span<byte> alpha)
        {
            for (int i = 2; i < 8; i++)
            {
                if ((sbyte)alpha[0] > (sbyte)alpha[1])
                {
                    alpha[i] = (byte)(((8 - i) * (sbyte)alpha[0] + (i - 1) * (sbyte)alpha[1]) / 7);
                }
                else if (i < 6)
                {
                    alpha[i] = (byte)(((6 - i) * (sbyte)alpha[0] + (i - 1) * (sbyte)alpha[1]) / 7);
                }
                else if (i == 6)
                {
                    alpha[i] = 0x80;
                }
                else /* i == 7 */
                {
                    alpha[i] = 0x7f;
                }
            }
        }

        private static void BCnDecodeTile(Span<byte> output, ReadOnlySpan<byte> input, int offset, bool bc1)
        {
            Color[] clut = new Color[4];

            int c0 = BinaryPrimitives.ReadUInt16LittleEndian(input.Slice(offset));
            int c1 = BinaryPrimitives.ReadUInt16LittleEndian(input.Slice(offset + 2));

            clut[0] = DecodeRGB565(c0);
            clut[1] = DecodeRGB565(c1);
            clut[2] = CalculateCLUT2(clut[0], clut[1], c0, c1, bc1);
            clut[3] = CalculateCLUT3(clut[0], clut[1], c0, c1, bc1);

            uint indices = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(offset + 4));

            int idxShift = 0;

            int oOffset = 0;

            for (int tY = 0; tY < BlockHeight; tY++)
            {
                for (int tX = 0; tX < BlockWidth; tX++)
                {
                    uint clutIndex = (indices >> idxShift) & 3;

                    idxShift += 2;

                    Color pixel = clut[clutIndex];

                    output[oOffset + 0] = pixel.R;
                    output[oOffset + 1] = pixel.G;
                    output[oOffset + 2] = pixel.B;
                    output[oOffset + 3] = pixel.A;

                    oOffset += 4;
                }
            }
        }

        private static Color CalculateCLUT2(Color color0, Color color1, int c0, int c1, bool bc1)
        {
            if (c0 > c1 || !bc1)
            {
                return Color.FromArgb(
                    (2 * color0.R + color1.R) / 3,
                    (2 * color0.G + color1.G) / 3,
                    (2 * color0.B + color1.B) / 3);
            }
            else
            {
                return Color.FromArgb(
                    (color0.R + color1.R) / 2,
                    (color0.G + color1.G) / 2,
                    (color0.B + color1.B) / 2);
            }
        }

        private static Color CalculateCLUT3(Color color0, Color color1, int c0, int c1, bool bc1)
        {
            if (c0 > c1 || !bc1)
            {
                return Color.FromArgb(
                    (2 * color1.R + color0.R) / 3,
                    (2 * color1.G + color0.G) / 3,
                    (2 * color1.B + color0.B) / 3);
            }

            return Color.Transparent;
        }

        private static Color DecodeRGB565(int value)
        {
            int b = ((value >> 0) & 0x1f) << 3;
            int g = ((value >> 5) & 0x3f) << 2;
            int r = ((value >> 11) & 0x1f) << 3;

            return Color.FromArgb(r | (r >> 5), g | (g >> 6), b | (b >> 5));
        }
    }
}