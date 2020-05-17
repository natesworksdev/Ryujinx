using Ryujinx.Common;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static Ryujinx.Graphics.Texture.BlockLinearConstants;

namespace Ryujinx.Graphics.Texture
{
    public static class LayoutConverter
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
        private struct Bpp12Pixel
        {
            private ulong _elem1;
            private uint _elem2;
        }

        private const int HostStrideAlignment = 4;

        public static Span<byte> ConvertBlockLinearToLinear(
            int width,
            int height,
            int depth,
            int levels,
            int layers,
            int blockWidth,
            int blockHeight,
            int bytesPerPixel,
            int gobBlocksInY,
            int gobBlocksInZ,
            int gobBlocksInTileX,
            SizeInfo sizeInfo,
            ReadOnlySpan<byte> data)
        {
            int outSize = GetTextureSize(
                width,
                height,
                depth,
                levels,
                layers,
                blockWidth,
                blockHeight,
                bytesPerPixel);

            Span<byte> output = new byte[outSize];

            int outOffs = 0;

            int mipGobBlocksInY = gobBlocksInY;
            int mipGobBlocksInZ = gobBlocksInZ;

            int gobWidth = (GobStride / bytesPerPixel) * gobBlocksInTileX;
            int gobHeight = gobBlocksInY * GobHeight;

            for (int level = 0; level < levels; level++)
            {
                int w = Math.Max(1, width >> level);
                int h = Math.Max(1, height >> level);
                int d = Math.Max(1, depth >> level);

                w = BitUtils.DivRoundUp(w, blockWidth);
                h = BitUtils.DivRoundUp(h, blockHeight);

                while (h <= (mipGobBlocksInY >> 1) * GobHeight && mipGobBlocksInY != 1)
                {
                    mipGobBlocksInY >>= 1;
                }

                while (d <= (mipGobBlocksInZ >> 1) && mipGobBlocksInZ != 1)
                {
                    mipGobBlocksInZ >>= 1;
                }

                int strideTrunc = BitUtils.AlignDown(w * bytesPerPixel, 16);

                int xStart = strideTrunc / bytesPerPixel;

                int stride = BitUtils.AlignUp(w * bytesPerPixel, HostStrideAlignment);

                int alignment = gobWidth;

                if (d < gobBlocksInZ || w <= gobWidth || h <= gobHeight)
                {
                    alignment = GobStride / bytesPerPixel;
                }

                int wAligned = BitUtils.AlignUp(w, alignment);

                BlockLinearLayout layoutConverter = new BlockLinearLayout(
                    wAligned,
                    h,
                    d,
                    mipGobBlocksInY,
                    mipGobBlocksInZ,
                    bytesPerPixel);

                unsafe void Convert<T>(Span<byte> output, ReadOnlySpan<byte> data) where T : unmanaged
                {
                    fixed (byte* outputBPtr = output, dataBPtr = data)
                    {
                        for (int layer = 0; layer < layers; layer++)
                        {
                            int inBaseOffset = layer * sizeInfo.LayerSize + sizeInfo.GetMipOffset(level);

                            for (int z = 0; z < d; z++)
                            {
                                layoutConverter.SetZ(z);
                                for (int y = 0; y < h; y++)
                                {
                                    layoutConverter.SetY(y);
                                    for (int x = 0; x < strideTrunc; x += 16)
                                    {
                                        int offset = inBaseOffset + layoutConverter.GetOffsetWithLineOffset(x);

                                        *(Vector128<byte>*)(outputBPtr + outOffs + x) = *(Vector128<byte>*)(dataBPtr + offset);
                                    }

                                    for (int x = xStart; x < w; x++)
                                    {
                                        int offset = inBaseOffset + layoutConverter.GetOffset(x);

                                        ((T*)(outputBPtr + outOffs))[x] = *(T*)(dataBPtr + offset);
                                    }

                                    outOffs += stride;
                                }
                            }
                        }
                    }
                }

                switch (bytesPerPixel)
                {
                    case 1:
                        Convert<byte>(output, data);
                        break;
                    case 2:
                        Convert<ushort>(output, data);
                        break;
                    case 4:
                        Convert<uint>(output, data);
                        break;
                    case 8:
                        Convert<ulong>(output, data);
                        break;
                    case 12:
                        Convert<Bpp12Pixel>(output, data);
                        break;
                    case 16:
                        Convert<Vector128<byte>>(output, data);
                        break;

                    default:
                        throw new NotSupportedException($"Unable to convert ${bytesPerPixel} bpp pixel format.");
                }
            }
            return output;
        }

        public static Span<byte> ConvertLinearStridedToLinear(
            int width,
            int height,
            int blockWidth,
            int blockHeight,
            int stride,
            int bytesPerPixel,
            ReadOnlySpan<byte> data)
        {
            int w = BitUtils.DivRoundUp(width,  blockWidth);
            int h = BitUtils.DivRoundUp(height, blockHeight);

            int outStride = BitUtils.AlignUp(w * bytesPerPixel, HostStrideAlignment);

            Span<byte> output = new byte[h * outStride];

            int outOffs = 0;

            unsafe void Convert<T>(Span<byte> output, ReadOnlySpan<byte> data) where T : unmanaged
            {
                fixed (byte* outputBPtr = output, dataBPtr = data)
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int offset = y * stride + x * bytesPerPixel;

                            ((T*)(outputBPtr + outOffs))[x] = *(T*)(dataBPtr + offset);
                        }

                        outOffs += outStride;
                    }
                }
            }

            switch (bytesPerPixel)
            {
                case 1:
                    Convert<byte>(output, data);
                    break;
                case 2:
                    Convert<ushort>(output, data);
                    break;
                case 4:
                    Convert<uint>(output, data);
                    break;
                case 8:
                    Convert<ulong>(output, data);
                    break;
                case 12:
                    Convert<Bpp12Pixel>(output, data);
                    break;
                case 16:
                    Convert<Vector128<byte>>(output, data);
                    break;

                default:
                    throw new NotSupportedException($"Unable to convert ${bytesPerPixel} bpp pixel format.");
            }

            return output;
        }

        public static Span<byte> ConvertLinearToBlockLinear(
            int width,
            int height,
            int depth,
            int levels,
            int layers,
            int blockWidth,
            int blockHeight,
            int bytesPerPixel,
            int gobBlocksInY,
            int gobBlocksInZ,
            int gobBlocksInTileX,
            SizeInfo sizeInfo,
            ReadOnlySpan<byte> data)
        {
            Span<byte> output = new byte[sizeInfo.TotalSize];

            int inOffs = 0;

            int mipGobBlocksInY = gobBlocksInY;
            int mipGobBlocksInZ = gobBlocksInZ;

            int gobWidth  = (GobStride / bytesPerPixel) * gobBlocksInTileX;
            int gobHeight = gobBlocksInY * GobHeight;

            for (int level = 0; level < levels; level++)
            {
                int w = Math.Max(1, width  >> level);
                int h = Math.Max(1, height >> level);
                int d = Math.Max(1, depth  >> level);

                w = BitUtils.DivRoundUp(w, blockWidth);
                h = BitUtils.DivRoundUp(h, blockHeight);

                while (h <= (mipGobBlocksInY >> 1) * GobHeight && mipGobBlocksInY != 1)
                {
                    mipGobBlocksInY >>= 1;
                }

                while (d <= (mipGobBlocksInZ >> 1) && mipGobBlocksInZ != 1)
                {
                    mipGobBlocksInZ >>= 1;
                }

                int stride = BitUtils.AlignUp(w * bytesPerPixel, HostStrideAlignment);

                int alignment = gobWidth;

                if (d < gobBlocksInZ || w <= gobWidth || h <= gobHeight)
                {
                    alignment = GobStride / bytesPerPixel;
                }

                int wAligned = BitUtils.AlignUp(w, alignment);

                BlockLinearLayout layoutConverter = new BlockLinearLayout(
                    wAligned,
                    h,
                    d,
                    mipGobBlocksInY,
                    mipGobBlocksInZ,
                    bytesPerPixel);

                unsafe void Convert<T>(Span<byte> output, ReadOnlySpan<byte> data) where T : unmanaged
                {
                    fixed (byte* outputBPtr = output, dataBPtr = data)
                    {
                        T* outputPtr = (T*)outputBPtr, dataPtr = (T*)dataBPtr;
                        for (int layer = 0; layer < layers; layer++)
                        {
                            int outBaseOffset = layer * sizeInfo.LayerSize + sizeInfo.GetMipOffset(level);

                            for (int z = 0; z < d; z++)
                            {
                                layoutConverter.SetZ(z);
                                for (int y = 0; y < h; y++)
                                {
                                    layoutConverter.SetY(y);
                                    for (int x = 0; x < w; x++)
                                    {
                                        int offset = outBaseOffset + layoutConverter.GetOffset(x);

                                        *(T*)(outputBPtr + offset) = ((T*)(dataBPtr + inOffs))[x];
                                    }

                                    inOffs += stride;
                                }
                            }
                        }
                    }
                }

                switch (bytesPerPixel)
                {
                    case 1:
                        Convert<byte>(output, data);
                        break;
                    case 2:
                        Convert<ushort>(output, data);
                        break;
                    case 4:
                        Convert<uint>(output, data);
                        break;
                    case 8:
                        Convert<ulong>(output, data);
                        break;
                    case 12:
                        Convert<Bpp12Pixel>(output, data);
                        break;
                    case 16:
                        Convert<Vector128<byte>>(output, data);
                        break;

                    default:
                        throw new NotSupportedException($"Unable to convert ${bytesPerPixel} bpp pixel format.");
                }
            }

            return output;
        }

        public static Span<byte> ConvertLinearToLinearStrided(
            int width,
            int height,
            int blockWidth,
            int blockHeight,
            int stride,
            int bytesPerPixel,
            ReadOnlySpan<byte> data)
        {
            int w = BitUtils.DivRoundUp(width,  blockWidth);
            int h = BitUtils.DivRoundUp(height, blockHeight);

            int inStride = BitUtils.AlignUp(w * bytesPerPixel, HostStrideAlignment);

            Span<byte> output = new byte[h * stride];

            int inOffs = 0;

            unsafe void Convert<T>(Span<byte> output, ReadOnlySpan<byte> data) where T : unmanaged
            {
                fixed (byte* outputBPtr = output, dataBPtr = data)
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int offset = y * stride + x * bytesPerPixel;

                            *(T*)(outputBPtr + offset) = ((T*)(dataBPtr + inOffs))[x];
                        }

                        inOffs += inStride;
                    }
                }
            }

            switch (bytesPerPixel)
            {
                case 1:
                    Convert<byte>(output, data);
                    break;
                case 2:
                    Convert<ushort>(output, data);
                    break;
                case 4:
                    Convert<uint>(output, data);
                    break;
                case 8:
                    Convert<ulong>(output, data);
                    break;
                case 12:
                    Convert<Bpp12Pixel>(output, data);
                    break;
                case 16:
                    Convert<Vector128<byte>>(output, data);
                    break;

                default:
                    throw new NotSupportedException($"Unable to convert ${bytesPerPixel} bpp pixel format.");
            }

            return output;
        }

        private static int GetTextureSize(
            int width,
            int height,
            int depth,
            int levels,
            int layers,
            int blockWidth,
            int blockHeight,
            int bytesPerPixel)
        {
            int layerSize = 0;

            for (int level = 0; level < levels; level++)
            {
                int w = Math.Max(1, width  >> level);
                int h = Math.Max(1, height >> level);
                int d = Math.Max(1, depth  >> level);

                w = BitUtils.DivRoundUp(w, blockWidth);
                h = BitUtils.DivRoundUp(h, blockHeight);

                int stride = BitUtils.AlignUp(w * bytesPerPixel, HostStrideAlignment);

                layerSize += stride * h * d;
            }

            return layerSize * layers;
        }
    }
}