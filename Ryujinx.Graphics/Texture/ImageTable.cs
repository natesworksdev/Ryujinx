using Ryujinx.Graphics.Gal;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Texture
{
    static class ImageTable
    {
        struct ImageDescriptor
        {
            public bool HasColor;
            public bool HasDepth;
            public bool HasStencil;

            public ImageDescriptor(
                bool HasColor,
                bool HasDepth,
                bool HasStencil)
            {
                this.HasColor = HasColor;
                this.HasDepth = HasDepth;
                this.HasStencil = HasStencil;
            }
        }

        private static readonly Dictionary<GalTextureFormat, TextureDescriptor> s_TextureTable =
            new Dictionary<GalTextureFormat, TextureDescriptor>()
            {
                { GalTextureFormat.R16G16B16A16, new TextureDescriptor(TextureReader.Read8Bpp,  GalImageFormat.R16G16B16A16_SNORM,       GalImageFormat.R16G16B16A16_UNORM,       GalImageFormat.R16G16B16A16_SINT,       GalImageFormat.R16G16B16A16_UINT,       GalImageFormat.R16G16B16A16_SFLOAT, null, null) },
                { GalTextureFormat.A8B8G8R8,     new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.A8B8G8R8_SNORM_PACK32,    GalImageFormat.A8B8G8R8_UNORM_PACK32,    GalImageFormat.A8B8G8R8_SINT_PACK32,    GalImageFormat.A8B8G8R8_UINT_PACK32,    null,                               null, null) },
                { GalTextureFormat.A2B10G10R10,  new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.A2B10G10R10_SNORM_PACK32, GalImageFormat.A2B10G10R10_UNORM_PACK32, GalImageFormat.A2B10G10R10_SINT_PACK32, GalImageFormat.A2B10G10R10_UINT_PACK32, null,                               null, null) },
                { GalTextureFormat.G8R8,         new TextureDescriptor(TextureReader.Read2Bpp,  GalImageFormat.R8G8_SNORM,               GalImageFormat.R8G8_UNORM,               GalImageFormat.R8G8_SINT,               GalImageFormat.R8G8_UINT,               null,                               null, null) },
                { GalTextureFormat.R16,          new TextureDescriptor(TextureReader.Read2Bpp,  GalImageFormat.R16_SNORM,                GalImageFormat.R16_UNORM,                GalImageFormat.R16_SINT,                GalImageFormat.R16_UINT,                GalImageFormat.R16_SFLOAT,          null, null) },
                { GalTextureFormat.R8,           new TextureDescriptor(TextureReader.Read1Bpp,  GalImageFormat.R8_SNORM,                 GalImageFormat.R8_UNORM,                 GalImageFormat.R8_SINT,                 GalImageFormat.R8_UINT,                 null,                               null, null) },
                { GalTextureFormat.R32G32B32A32, new TextureDescriptor(TextureReader.Read16Bpp, null,                                    null,                                    GalImageFormat.R32G32B32A32_SINT,       GalImageFormat.R32G32B32A32_UINT,       GalImageFormat.R32G32B32A32_SFLOAT, null, null) },
                { GalTextureFormat.R32G32,       new TextureDescriptor(TextureReader.Read8Bpp,  null,                                    null,                                    GalImageFormat.R32G32_SINT,             GalImageFormat.R32G32_UINT,             GalImageFormat.R32G32_SFLOAT,       null, null) },
                { GalTextureFormat.R32,          new TextureDescriptor(TextureReader.Read4Bpp,  null,                                    null,                                    GalImageFormat.R32_SINT,                GalImageFormat.R32_UINT,                GalImageFormat.R32_SFLOAT,          null, null) },

                { GalTextureFormat.A4B4G4R4,     new TextureDescriptor(TextureReader.Read2Bpp, Unorm: GalImageFormat.R4G4B4A4_UNORM_PACK16_REVERSED) }, //TODO: Reverse this one in the reader
                { GalTextureFormat.A1B5G5R5,     new TextureDescriptor(TextureReader.Read5551, Unorm: GalImageFormat.A1R5G5B5_UNORM_PACK16)          },
                { GalTextureFormat.B5G6R5,       new TextureDescriptor(TextureReader.Read565,  Unorm: GalImageFormat.B5G6R5_UNORM_PACK16)            },
                { GalTextureFormat.BF10GF11RF11, new TextureDescriptor(TextureReader.Read4Bpp, Float: GalImageFormat.B10G11R11_UFLOAT_PACK32)        },

                //Zeta formats
                { GalTextureFormat.ZF32,       new TextureDescriptor(TextureReader.Read4Bpp, Float: GalImageFormat.D32_SFLOAT)         },
                { GalTextureFormat.Z24S8,      new TextureDescriptor(TextureReader.Read4Bpp, Unorm: GalImageFormat.D24_UNORM_S8_UINT)  },
                { GalTextureFormat.ZF32_X24S8, new TextureDescriptor(TextureReader.Read8Bpp, Unorm: GalImageFormat.D32_SFLOAT_S8_UINT) },

                //Compressed formats
                { GalTextureFormat.BC4,  new TextureDescriptor(TextureReader.Read8Bpt4x4,                   Snorm: GalImageFormat.BC4_SNORM_BLOCK, Unorm: GalImageFormat.BC4_UNORM_BLOCK) },
                { GalTextureFormat.BC5,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4, Snorm: GalImageFormat.BC5_SNORM_BLOCK, Unorm: GalImageFormat.BC5_UNORM_BLOCK) },

                { GalTextureFormat.BC7U,      new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4, Unorm: GalImageFormat.BC7_UNORM_BLOCK)      },
                { GalTextureFormat.BC1,       new TextureDescriptor(TextureReader.Read8Bpt4x4,                   Unorm: GalImageFormat.BC1_RGBA_UNORM_BLOCK) },
                { GalTextureFormat.BC2,       new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4, Unorm: GalImageFormat.BC2_UNORM_BLOCK)      },
                { GalTextureFormat.BC3,       new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4, Unorm: GalImageFormat.BC3_UNORM_BLOCK)      },
                { GalTextureFormat.BC6H_SF16, new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4, Unorm: GalImageFormat.BC6H_SFLOAT_BLOCK)    },
                { GalTextureFormat.BC6H_UF16, new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4, Unorm: GalImageFormat.BC6H_UFLOAT_BLOCK)    },

                { GalTextureFormat.Astc2D4x4,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   Unorm: GalImageFormat.ASTC_4x4_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D5x5,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture5x5,   Unorm: GalImageFormat.ASTC_5x5_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D6x6,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture6x6,   Unorm: GalImageFormat.ASTC_6x6_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D8x8,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture8x8,   Unorm: GalImageFormat.ASTC_8x8_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D10x10, new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x10, Unorm: GalImageFormat.ASTC_10x10_UNORM_BLOCK) },
                { GalTextureFormat.Astc2D12x12, new TextureDescriptor(TextureReader.Read16BptCompressedTexture12x12, Unorm: GalImageFormat.ASTC_12x12_UNORM_BLOCK) },
                { GalTextureFormat.Astc2D5x4,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture5x4,   Unorm: GalImageFormat.ASTC_5x4_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D6x5,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture6x5,   Unorm: GalImageFormat.ASTC_6x5_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D8x6,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture8x6,   Unorm: GalImageFormat.ASTC_8x6_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D10x8,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x8,  Unorm: GalImageFormat.ASTC_10x8_UNORM_BLOCK)  },
                { GalTextureFormat.Astc2D12x10, new TextureDescriptor(TextureReader.Read16BptCompressedTexture12x10, Unorm: GalImageFormat.ASTC_12x10_UNORM_BLOCK) },
                { GalTextureFormat.Astc2D8x5,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture8x5,   Unorm: GalImageFormat.ASTC_8x5_UNORM_BLOCK)   },
                { GalTextureFormat.Astc2D10x5,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x5,  Unorm: GalImageFormat.ASTC_10x5_UNORM_BLOCK)  },
                { GalTextureFormat.Astc2D10x6,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x6,  Unorm: GalImageFormat.ASTC_10x6_UNORM_BLOCK)  },

            };

        private static readonly Dictionary<GalImageFormat, ImageDescriptor> s_ImageTable =
            new Dictionary<GalImageFormat, ImageDescriptor>()
            {
                { GalImageFormat.R32G32B32A32_SFLOAT,      new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32G32B32A32_SINT,        new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32G32B32A32_UINT,        new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16B16A16_SFLOAT,      new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16B16A16_SINT,        new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16B16A16_UINT,        new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32G32_SFLOAT,            new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32G32_SINT,              new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32G32_UINT,              new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A8B8G8R8_SNORM_PACK32,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A8B8G8R8_UNORM_PACK32,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A8B8G8R8_SINT_PACK32,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A8B8G8R8_UINT_PACK32,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A2B10G10R10_SINT_PACK32,  new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A2B10G10R10_SNORM_PACK32, new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A2B10G10R10_UINT_PACK32,  new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A2B10G10R10_UNORM_PACK32, new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32_SFLOAT,               new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32_SINT,                 new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32_UINT,                 new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC6H_SFLOAT_BLOCK,        new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC6H_UFLOAT_BLOCK,        new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A1R5G5B5_UNORM_PACK16,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.B5G6R5_UNORM_PACK16,      new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC7_UNORM_BLOCK,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16_SFLOAT,            new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16_SINT,              new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16_SNORM,             new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16_UNORM,             new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8G8_SINT,                new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8G8_SNORM,               new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8G8_UINT,                new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8G8_UNORM,               new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16_SFLOAT,               new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16_SINT,                 new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16_SNORM,                new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16_UINT,                 new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16_UNORM,                new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8_SINT,                  new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8_SNORM,                 new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8_UINT,                  new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8_UNORM,                 new ImageDescriptor(true,  false, false) },
                { GalImageFormat.B10G11R11_UFLOAT_PACK32,  new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC1_RGBA_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC2_UNORM_BLOCK,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC3_UNORM_BLOCK,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC4_UNORM_BLOCK,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC5_UNORM_BLOCK,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_4x4_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_5x5_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_6x6_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_8x8_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x10_UNORM_BLOCK,   new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_12x12_UNORM_BLOCK,   new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_5x4_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_6x5_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_8x6_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x8_UNORM_BLOCK,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_12x10_UNORM_BLOCK,   new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_8x5_UNORM_BLOCK,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x5_UNORM_BLOCK,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x6_UNORM_BLOCK,    new ImageDescriptor(true,  false, false) },

                { GalImageFormat.D24_UNORM_S8_UINT,  new ImageDescriptor(false, true,  true)  },
                { GalImageFormat.D32_SFLOAT,         new ImageDescriptor(false, true,  false) },
                { GalImageFormat.D16_UNORM,          new ImageDescriptor(false, true,  false) },
                { GalImageFormat.D32_SFLOAT_S8_UINT, new ImageDescriptor(false, true,  true)  },

                { GalImageFormat.R4G4B4A4_UNORM_PACK16_REVERSED, new ImageDescriptor(true,  false, false) }
            };

        public static GalImageFormat ConvertTexture(
            GalTextureFormat Format,
            GalTextureType RType,
            GalTextureType GType,
            GalTextureType BType,
            GalTextureType AType)
        {
            if (RType != GType || RType != BType || RType != AType)
            {
                throw new NotImplementedException("Per component types are not implemented");
            }

            TextureDescriptor Descriptor = GetTextureDescriptor(Format);

            GalTextureType Type = RType;

            GalImageFormat? ImageFormat;

            switch (Type)
            {
                case GalTextureType.Snorm:            ImageFormat = Descriptor.Snorm;            break;
                case GalTextureType.Unorm:            ImageFormat = Descriptor.Unorm;            break;
                case GalTextureType.Sint:             ImageFormat = Descriptor.Sint;             break;
                case GalTextureType.Uint:             ImageFormat = Descriptor.Uint;             break;
                case GalTextureType.Float:            ImageFormat = Descriptor.Float;            break;
                case GalTextureType.Snorm_Force_Fp16: ImageFormat = Descriptor.Snorm_Force_Fp16; break;
                case GalTextureType.Unorm_Force_Fp16: ImageFormat = Descriptor.Unorm_Force_Fp16; break;

                default: throw new NotImplementedException("Unknown component type " + ((int)Type).ToString("x2"));
            }

            if (!ImageFormat.HasValue)
            {
                throw new NotImplementedException("Texture with format " + Format +
                                                  " and component type " + Type + " is not implemented");
            }

            return ImageFormat.Value;
        }

        public static GalImageFormat ConvertFrameBuffer(GalFrameBufferFormat Format)
        {
            switch (Format)
            {
                case GalFrameBufferFormat.R32Float:       return GalImageFormat.R32_SFLOAT;
                case GalFrameBufferFormat.RGB10A2Unorm:   return GalImageFormat.A2B10G10R10_UNORM_PACK32;
                case GalFrameBufferFormat.RGBA8Srgb:      return GalImageFormat.A8B8G8R8_SRGB_PACK32;
                case GalFrameBufferFormat.RGBA16Float:    return GalImageFormat.R16G16B16A16_SFLOAT;
                case GalFrameBufferFormat.R16Float:       return GalImageFormat.R16_SFLOAT;
                case GalFrameBufferFormat.R8Unorm:        return GalImageFormat.R8_UNORM;
                case GalFrameBufferFormat.RGBA8Unorm:     return GalImageFormat.A8B8G8R8_UNORM_PACK32;
                case GalFrameBufferFormat.R11G11B10Float: return GalImageFormat.B10G11R11_UFLOAT_PACK32;
                case GalFrameBufferFormat.RGBA32Float:    return GalImageFormat.R32G32B32A32_SFLOAT;
                case GalFrameBufferFormat.RG16Snorm:      return GalImageFormat.R16G16_SNORM;
                case GalFrameBufferFormat.RG16Float:      return GalImageFormat.R16G16_SFLOAT;
                case GalFrameBufferFormat.RG8Snorm:       return GalImageFormat.R8_SNORM;
                case GalFrameBufferFormat.RGBA8Snorm:     return GalImageFormat.A8B8G8R8_SNORM_PACK32;
                case GalFrameBufferFormat.RG8Unorm:       return GalImageFormat.R8G8_UNORM;
                case GalFrameBufferFormat.BGRA8Unorm:     return GalImageFormat.A8B8G8R8_UNORM_PACK32;
                case GalFrameBufferFormat.BGRA8Srgb:      return GalImageFormat.A8B8G8R8_SRGB_PACK32;
                case GalFrameBufferFormat.RG32Float:      return GalImageFormat.R32G32_SFLOAT;
                case GalFrameBufferFormat.RG32Sint:       return GalImageFormat.R32G32_SINT;
                case GalFrameBufferFormat.RG32Uint:       return GalImageFormat.R32G32_UINT;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static GalImageFormat ConvertZeta(GalZetaFormat Format)
        {
            switch (Format)
            {
                case GalZetaFormat.Z32Float: return GalImageFormat.D32_SFLOAT;
                case GalZetaFormat.S8Z24Unorm: return GalImageFormat.D24_UNORM_S8_UINT;
                case GalZetaFormat.Z16Unorm: return GalImageFormat.D16_UNORM;
                case GalZetaFormat.Z32S8X24Float: return GalImageFormat.D32_SFLOAT_S8_UINT;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static int GetImageSize(GalImage Image)
        {
            switch (Image.Format)
            {
                case GalImageFormat.R32G32B32A32_SFLOAT:
                case GalImageFormat.R32G32B32A32_SINT:
                case GalImageFormat.R32G32B32A32_UINT:
                    return Image.Width * Image.Height * 16;

                case GalImageFormat.R16G16B16A16_SFLOAT:
                case GalImageFormat.R16G16B16A16_SINT:
                case GalImageFormat.R16G16B16A16_SNORM:
                case GalImageFormat.R16G16B16A16_UINT:
                case GalImageFormat.R16G16B16A16_UNORM:
                case GalImageFormat.D32_SFLOAT_S8_UINT:
                case GalImageFormat.R32G32_SFLOAT:
                case GalImageFormat.R32G32_SINT:
                case GalImageFormat.R32G32_UINT:
                    return Image.Width * Image.Height * 8;

                case GalImageFormat.A8B8G8R8_SINT_PACK32:
                case GalImageFormat.A8B8G8R8_SNORM_PACK32:
                case GalImageFormat.A8B8G8R8_UINT_PACK32:
                case GalImageFormat.A8B8G8R8_UNORM_PACK32:
                case GalImageFormat.A8B8G8R8_SRGB_PACK32:
                case GalImageFormat.A2B10G10R10_SINT_PACK32:
                case GalImageFormat.A2B10G10R10_SNORM_PACK32:
                case GalImageFormat.A2B10G10R10_UINT_PACK32:
                case GalImageFormat.A2B10G10R10_UNORM_PACK32:
                case GalImageFormat.R16G16_SFLOAT:
                case GalImageFormat.R16G16_SINT:
                case GalImageFormat.R16G16_SNORM:
                case GalImageFormat.R16G16_UINT:
                case GalImageFormat.R16G16_UNORM:
                case GalImageFormat.R32_SFLOAT:
                case GalImageFormat.R32_SINT:
                case GalImageFormat.R32_UINT:
                case GalImageFormat.D32_SFLOAT:
                case GalImageFormat.B10G11R11_UFLOAT_PACK32:
                case GalImageFormat.D24_UNORM_S8_UINT:
                    return Image.Width * Image.Height * 4;

                case GalImageFormat.B4G4R4A4_UNORM_PACK16:
                case GalImageFormat.A1R5G5B5_UNORM_PACK16:
                case GalImageFormat.B5G6R5_UNORM_PACK16:
                case GalImageFormat.R8G8_SINT:
                case GalImageFormat.R8G8_SNORM:
                case GalImageFormat.R8G8_UINT:
                case GalImageFormat.R8G8_UNORM:
                case GalImageFormat.R16_SFLOAT:
                case GalImageFormat.R16_SINT:
                case GalImageFormat.R16_SNORM:
                case GalImageFormat.R16_UINT:
                case GalImageFormat.R16_UNORM:
                case GalImageFormat.D16_UNORM:
                    return Image.Width * Image.Height * 2;

                case GalImageFormat.R8_SINT:
                case GalImageFormat.R8_SNORM:
                case GalImageFormat.R8_UINT:
                case GalImageFormat.R8_UNORM:
                    return Image.Width * Image.Height;

                case GalImageFormat.BC1_RGBA_UNORM_BLOCK:
                case GalImageFormat.BC4_SNORM_BLOCK:
                case GalImageFormat.BC4_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 4, 4, 8);
                }

                case GalImageFormat.BC6H_SFLOAT_BLOCK:
                case GalImageFormat.BC6H_UFLOAT_BLOCK:
                case GalImageFormat.BC7_UNORM_BLOCK:
                case GalImageFormat.BC2_UNORM_BLOCK:
                case GalImageFormat.BC3_UNORM_BLOCK:
                case GalImageFormat.BC5_SNORM_BLOCK:
                case GalImageFormat.BC5_UNORM_BLOCK:
                case GalImageFormat.ASTC_4x4_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 4, 4, 16);
                }

                case GalImageFormat.ASTC_5x5_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 5, 5, 16);
                }

                case GalImageFormat.ASTC_6x6_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 6, 6, 16);
                }

                case GalImageFormat.ASTC_8x8_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 8, 16);
                }

                case GalImageFormat.ASTC_10x10_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 10, 16);
                }

                case GalImageFormat.ASTC_12x12_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 12, 12, 16);
                }

                case GalImageFormat.ASTC_5x4_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 5, 4, 16);
                }

                case GalImageFormat.ASTC_6x5_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 6, 5, 16);
                }

                case GalImageFormat.ASTC_8x6_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 6, 16);
                }

                case GalImageFormat.ASTC_10x8_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 8, 16);
                }

                case GalImageFormat.ASTC_12x10_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 12, 10, 16);
                }

                case GalImageFormat.ASTC_8x5_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 5, 16);
                }

                case GalImageFormat.ASTC_10x5_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 5, 16);
                }

                case GalImageFormat.ASTC_10x6_UNORM_BLOCK:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 6, 16);
                }
            }

            throw new NotImplementedException(Image.Format.ToString());
        }

        public static bool HasColor(GalImage Image)
        {
            return s_ImageTable[Image.Format].HasColor;
        }

        public static bool HasDepth(GalImage Image)
        {
            return s_ImageTable[Image.Format].HasDepth;
        }

        public static bool HasStencil(GalImage Image)
        {
            return s_ImageTable[Image.Format].HasStencil;
        }

        public static TextureDescriptor GetTextureDescriptor(GalTextureFormat Format)
        {
            if (s_TextureTable.TryGetValue(Format, out TextureDescriptor Descriptor))
            {
                return Descriptor;
            }

            throw new NotImplementedException("Texture with format code " + ((int)Format).ToString("x2") + " not implemented");
        }

        private static int CompressedTextureSize(int TextureWidth, int TextureHeight, int BlockWidth, int BlockHeight, int Bpb)
        {
            int W = (TextureWidth + (BlockWidth - 1)) / BlockWidth;
            int H = (TextureHeight + (BlockHeight - 1)) / BlockHeight;

            return W * H * Bpb;
        }
    }
}