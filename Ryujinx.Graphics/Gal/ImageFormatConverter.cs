using System;

namespace Ryujinx.Graphics.Gal
{
    public static class ImageFormatConverter
    {
        public static GalImageFormat ConvertTexture(GalTextureFormat Format)
        {
            switch (Format)
            {
                case GalTextureFormat.R32G32B32A32: return GalImageFormat.R32G32B32A32;
                case GalTextureFormat.R16G16B16A16: return GalImageFormat.R16G16B16A16;
                case GalTextureFormat.A8B8G8R8:     return GalImageFormat.A8B8G8R8;
                case GalTextureFormat.A2B10G10R10:  return GalImageFormat.A2B10G10R10;
                case GalTextureFormat.R32:          return GalImageFormat.R32;
                case GalTextureFormat.BC6H_SF16:    return GalImageFormat.BC6H_SF16;
                case GalTextureFormat.BC6H_UF16:    return GalImageFormat.BC6H_UF16;
                case GalTextureFormat.A1B5G5R5:     return GalImageFormat.A1B5G5R5;
                case GalTextureFormat.B5G6R5:       return GalImageFormat.B5G6R5;
                case GalTextureFormat.BC7U:         return GalImageFormat.BC7U;
                case GalTextureFormat.G8R8:         return GalImageFormat.G8R8;
                case GalTextureFormat.R16:          return GalImageFormat.R16;
                case GalTextureFormat.R8:           return GalImageFormat.R8;
                case GalTextureFormat.BF10GF11RF11: return GalImageFormat.BF10GF11RF11;
                case GalTextureFormat.BC1:          return GalImageFormat.BC1;
                case GalTextureFormat.BC2:          return GalImageFormat.BC2;
                case GalTextureFormat.BC3:          return GalImageFormat.BC3;
                case GalTextureFormat.BC4:          return GalImageFormat.BC4;
                case GalTextureFormat.BC5:          return GalImageFormat.BC5;
                case GalTextureFormat.Z24S8:        return GalImageFormat.Z24S8;
                case GalTextureFormat.ZF32:         return GalImageFormat.ZF32;
                case GalTextureFormat.Astc2D4x4:    return GalImageFormat.Astc2D4x4;
                case GalTextureFormat.Astc2D5x5:    return GalImageFormat.Astc2D5x5;
                case GalTextureFormat.Astc2D6x6:    return GalImageFormat.Astc2D6x6;
                case GalTextureFormat.Astc2D8x8:    return GalImageFormat.Astc2D8x8;
                case GalTextureFormat.Astc2D10x10:  return GalImageFormat.Astc2D10x10;
                case GalTextureFormat.Astc2D12x12:  return GalImageFormat.Astc2D12x12;
                case GalTextureFormat.Astc2D5x4:    return GalImageFormat.Astc2D5x4;
                case GalTextureFormat.Astc2D6x5:    return GalImageFormat.Astc2D6x5;
                case GalTextureFormat.Astc2D8x6:    return GalImageFormat.Astc2D8x6;
                case GalTextureFormat.Astc2D10x8:   return GalImageFormat.Astc2D10x8;
                case GalTextureFormat.Astc2D12x10:  return GalImageFormat.Astc2D12x10;
                case GalTextureFormat.Astc2D8x5:    return GalImageFormat.Astc2D8x5;
                case GalTextureFormat.Astc2D10x5:   return GalImageFormat.Astc2D10x5;
                case GalTextureFormat.Astc2D10x6:   return GalImageFormat.Astc2D10x6;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static GalImageFormat ConvertFrameBuffer(GalFrameBufferFormat Format)
        {
            switch (Format)
            {
                case GalFrameBufferFormat.R32Float:       return GalImageFormat.R32;
                case GalFrameBufferFormat.RGB10A2Unorm:   return GalImageFormat.A2B10G10R10;
                case GalFrameBufferFormat.RGBA8Srgb:      return GalImageFormat.A8B8G8R8; //Stubbed
                case GalFrameBufferFormat.RGBA16Float:    return GalImageFormat.R16G16B16A16;
                case GalFrameBufferFormat.R16Float:       return GalImageFormat.R16;
                case GalFrameBufferFormat.R8Unorm:        return GalImageFormat.R8;
                case GalFrameBufferFormat.RGBA8Unorm:     return GalImageFormat.A8B8G8R8;
                case GalFrameBufferFormat.R11G11B10Float: return GalImageFormat.BF10GF11RF11;
                case GalFrameBufferFormat.RGBA32Float:    return GalImageFormat.R32G32B32A32;
                case GalFrameBufferFormat.RG16Snorm:      return GalImageFormat.G16R16;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static GalImageFormat ConvertZeta(GalZetaFormat Format)
        {
            switch (Format)
            {
                case GalZetaFormat.Z32Float:   return GalImageFormat.ZF32;
                case GalZetaFormat.S8Z24Unorm: return GalImageFormat.Z24S8;
                case GalZetaFormat.Z16Unorm:   return GalImageFormat.Z16;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static bool HasColor(GalImageFormat Format)
        {
            switch (Format)
            {
                case GalImageFormat.R32G32B32A32:
                case GalImageFormat.R16G16B16A16:
                case GalImageFormat.A8B8G8R8:
                case GalImageFormat.A2B10G10R10:
                case GalImageFormat.R32:
                case GalImageFormat.BC6H_SF16:
                case GalImageFormat.BC6H_UF16:
                case GalImageFormat.A1B5G5R5:
                case GalImageFormat.B5G6R5:
                case GalImageFormat.BC7U:
                case GalImageFormat.G16R16:
                case GalImageFormat.G8R8:
                case GalImageFormat.R16:
                case GalImageFormat.R8:
                case GalImageFormat.BF10GF11RF11:
                case GalImageFormat.BC1:
                case GalImageFormat.BC2:
                case GalImageFormat.BC3:
                case GalImageFormat.BC4:
                case GalImageFormat.BC5:
                case GalImageFormat.Astc2D4x4:
                case GalImageFormat.Astc2D5x5:
                case GalImageFormat.Astc2D6x6:
                case GalImageFormat.Astc2D8x8:
                case GalImageFormat.Astc2D10x10:
                case GalImageFormat.Astc2D12x12:
                case GalImageFormat.Astc2D5x4:
                case GalImageFormat.Astc2D6x5:
                case GalImageFormat.Astc2D8x6:
                case GalImageFormat.Astc2D10x8:
                case GalImageFormat.Astc2D12x10:
                case GalImageFormat.Astc2D8x5:
                case GalImageFormat.Astc2D10x5:
                case GalImageFormat.Astc2D10x6:
                    return true;
            }

            return false;
        }

        public static bool HasDepth(GalImageFormat Format)
        {
            switch (Format)
            {
                case GalImageFormat.Z24S8:
                case GalImageFormat.ZF32:
                case GalImageFormat.Z16:
                    return true;
            }

            return false;
        }

        public static bool HasStencil(GalImageFormat Format)
        {
            switch (Format)
            {
                case GalImageFormat.Z24S8:
                    return true;
            }

            return false;
        }
    }
}