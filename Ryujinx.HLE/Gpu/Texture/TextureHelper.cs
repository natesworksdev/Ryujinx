using ChocolArm64.Memory;
using Ryujinx.Graphics.Gal;
using Ryujinx.HLE.Gpu.Memory;
using System;

namespace Ryujinx.HLE.Gpu.Texture
{
    static class TextureHelper
    {
        public static ISwizzle GetSwizzle(TextureInfo Texture, int BlockWidth, int Bpp)
        {
            int Width = (Texture.Width + (BlockWidth - 1)) / BlockWidth;

            int AlignMask = Texture.TileWidth * (64 / Bpp) - 1;

            Width = (Width + AlignMask) & ~AlignMask;

            switch (Texture.Swizzle)
            {
                case TextureSwizzle._1dBuffer:
                case TextureSwizzle.Pitch:
                case TextureSwizzle.PitchColorKey:
                     return new LinearSwizzle(Texture.Pitch, Bpp);

                case TextureSwizzle.BlockLinear:
                case TextureSwizzle.BlockLinearColorKey:
                    return new BlockLinearSwizzle(Width, Bpp, Texture.BlockHeight);
            }

            throw new NotImplementedException(Texture.Swizzle.ToString());
        }

        public static int GetTextureSize(GalImage Image)
        {
            switch (Image.Format)
            {
                case GalImageFormat.R32G32B32A32:
                    return Image.Width * Image.Height * 16;

                case GalImageFormat.R16G16B16A16:
                    return Image.Width * Image.Height * 8;

                case GalImageFormat.A8B8G8R8:
                case GalImageFormat.A2B10G10R10:
                case GalImageFormat.G16R16:
                case GalImageFormat.R32:
                case GalImageFormat.ZF32:
                case GalImageFormat.BF10GF11RF11:
                case GalImageFormat.Z24S8:
                    return Image.Width * Image.Height * 4;

                case GalImageFormat.A1B5G5R5:
                case GalImageFormat.B5G6R5:
                case GalImageFormat.G8R8:
                case GalImageFormat.R16:
                case GalImageFormat.Z16:
                    return Image.Width * Image.Height * 2;

                case GalImageFormat.R8:
                    return Image.Width * Image.Height;

                case GalImageFormat.BC1:
                case GalImageFormat.BC4:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 4, 4, 8);
                }

                case GalImageFormat.BC6H_SF16:
                case GalImageFormat.BC6H_UF16:
                case GalImageFormat.BC7U:
                case GalImageFormat.BC2:
                case GalImageFormat.BC3:
                case GalImageFormat.BC5:
                case GalImageFormat.Astc2D4x4:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 4, 4, 16);
                }

                case GalImageFormat.Astc2D5x5:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 5, 5, 16);
                }

                case GalImageFormat.Astc2D6x6:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 6, 6, 16);
                }

                case GalImageFormat.Astc2D8x8:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 8, 16);
                }

                case GalImageFormat.Astc2D10x10:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 10, 16);
                }

                case GalImageFormat.Astc2D12x12:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 12, 12, 16);
                }

                case GalImageFormat.Astc2D5x4:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 5, 4, 16);
                }

                case GalImageFormat.Astc2D6x5:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 6, 5, 16);
                }

                case GalImageFormat.Astc2D8x6:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 6, 16);
                }

                case GalImageFormat.Astc2D10x8:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 8, 16);
                }

                case GalImageFormat.Astc2D12x10:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 12, 10, 16);
                }

                case GalImageFormat.Astc2D8x5:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 5, 16);
                }

                case GalImageFormat.Astc2D10x5:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 5, 16);
                }

                case GalImageFormat.Astc2D10x6:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 6, 16);
                }
            }

            throw new NotImplementedException(Image.Format.ToString());
        }

        public static int CompressedTextureSize(int TextureWidth, int TextureHeight, int BlockWidth, int BlockHeight, int Bpb)
        {
            int W = (TextureWidth  + (BlockWidth - 1)) / BlockWidth;
            int H = (TextureHeight + (BlockHeight - 1)) / BlockHeight;

            return W * H * Bpb;
        }

        public static (AMemory Memory, long Position) GetMemoryAndPosition(
            IAMemory Memory,
            long     Position)
        {
            if (Memory is NvGpuVmm Vmm)
            {
                return (Vmm.Memory, Vmm.GetPhysicalAddress(Position));
            }

            return ((AMemory)Memory, Position);
        }
    }
}
