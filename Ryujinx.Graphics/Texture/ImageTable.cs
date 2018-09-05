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

        private const GalImageFormat Snorm  = GalImageFormat.Snorm;
        private const GalImageFormat Unorm  = GalImageFormat.Unorm;
        private const GalImageFormat Sint   = GalImageFormat.Sint;
        private const GalImageFormat Uint   = GalImageFormat.Uint;
        private const GalImageFormat Sfloat = GalImageFormat.Sfloat;

        private static readonly Dictionary<GalTextureFormat, TextureDescriptor> s_TextureTable =
            new Dictionary<GalTextureFormat, TextureDescriptor>()
            {
                { GalTextureFormat.R32G32B32A32, new TextureDescriptor(TextureReader.Read16Bpp, GalImageFormat.R32G32B32A32                 | Sint | Uint | Sfloat) },
                { GalTextureFormat.R16G16B16A16, new TextureDescriptor(TextureReader.Read8Bpp,  GalImageFormat.R16G16B16A16 | Snorm | Unorm | Sint | Uint | Sfloat) },
                { GalTextureFormat.R32G32,       new TextureDescriptor(TextureReader.Read8Bpp,  GalImageFormat.R32G32                       | Sint | Uint | Sfloat) },
                { GalTextureFormat.A8B8G8R8,     new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.A8B8G8R8     | Snorm | Unorm | Sint | Uint         ) },
                { GalTextureFormat.A2B10G10R10,  new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.A2B10G10R10  | Snorm | Unorm | Sint | Uint         ) },
                { GalTextureFormat.G8R8,         new TextureDescriptor(TextureReader.Read2Bpp,  GalImageFormat.R8G8         | Snorm | Unorm | Sint | Uint         ) },
                { GalTextureFormat.R16,          new TextureDescriptor(TextureReader.Read2Bpp,  GalImageFormat.R16          | Snorm | Unorm | Sint | Uint | Sfloat) },
                { GalTextureFormat.R8,           new TextureDescriptor(TextureReader.Read1Bpp,  GalImageFormat.R8           | Snorm | Unorm | Sint | Uint         ) },
                { GalTextureFormat.R32,          new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.R32                          | Sint | Uint | Sfloat) },
                //TODO: Reverse this one in the reader
                { GalTextureFormat.A4B4G4R4,     new TextureDescriptor(TextureReader.Read2Bpp,  GalImageFormat.R4G4B4A4_REVERSED    | Unorm                       ) },
                { GalTextureFormat.A1B5G5R5,     new TextureDescriptor(TextureReader.Read5551,  GalImageFormat.A1R5G5B5             | Unorm                       ) },
                { GalTextureFormat.B5G6R5,       new TextureDescriptor(TextureReader.Read565,   GalImageFormat.B5G6R5               | Unorm                       ) },
                { GalTextureFormat.BF10GF11RF11, new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.B10G11R11                                  | Sfloat) },
                { GalTextureFormat.Z24S8,        new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.D24_S8               | Unorm                       ) },
                { GalTextureFormat.ZF32,         new TextureDescriptor(TextureReader.Read4Bpp,  GalImageFormat.D32                                        | Sfloat) },
                { GalTextureFormat.ZF32_X24S8,   new TextureDescriptor(TextureReader.Read8Bpp,  GalImageFormat.D32_S8               | Unorm                       ) },

                //Compressed formats
                { GalTextureFormat.BC6H_SF16,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.BC6H_SF16  | Unorm        ) },
                { GalTextureFormat.BC6H_UF16,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.BC6H_UF16  | Unorm        ) },
                { GalTextureFormat.BC7U,        new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.BC7        | Unorm        ) },
                { GalTextureFormat.BC1,         new TextureDescriptor(TextureReader.Read8Bpt4x4,                     GalImageFormat.BC1_RGBA   | Unorm        ) },
                { GalTextureFormat.BC2,         new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.BC2        | Unorm        ) },
                { GalTextureFormat.BC3,         new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.BC3        | Unorm        ) },
                { GalTextureFormat.BC4,         new TextureDescriptor(TextureReader.Read8Bpt4x4,                     GalImageFormat.BC4        | Unorm | Snorm) },
                { GalTextureFormat.BC5,         new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.BC5        | Unorm | Snorm) },
                { GalTextureFormat.Astc2D4x4,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture4x4,   GalImageFormat.ASTC_4x4   | Unorm        ) },
                { GalTextureFormat.Astc2D5x5,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture5x5,   GalImageFormat.ASTC_5x5   | Unorm        ) },
                { GalTextureFormat.Astc2D6x6,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture6x6,   GalImageFormat.ASTC_6x6   | Unorm        ) },
                { GalTextureFormat.Astc2D8x8,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture8x8,   GalImageFormat.ASTC_8x8   | Unorm        ) },
                { GalTextureFormat.Astc2D10x10, new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x10, GalImageFormat.ASTC_10x10 | Unorm        ) },
                { GalTextureFormat.Astc2D12x12, new TextureDescriptor(TextureReader.Read16BptCompressedTexture12x12, GalImageFormat.ASTC_12x12 | Unorm        ) },
                { GalTextureFormat.Astc2D5x4,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture5x4,   GalImageFormat.ASTC_5x4   | Unorm        ) },
                { GalTextureFormat.Astc2D6x5,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture6x5,   GalImageFormat.ASTC_6x5   | Unorm        ) },
                { GalTextureFormat.Astc2D8x6,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture8x6,   GalImageFormat.ASTC_8x6   | Unorm        ) },
                { GalTextureFormat.Astc2D10x8,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x8,  GalImageFormat.ASTC_10x8  | Unorm        ) },
                { GalTextureFormat.Astc2D12x10, new TextureDescriptor(TextureReader.Read16BptCompressedTexture12x10, GalImageFormat.ASTC_12x10 | Unorm        ) },
                { GalTextureFormat.Astc2D8x5,   new TextureDescriptor(TextureReader.Read16BptCompressedTexture8x5,   GalImageFormat.ASTC_8x5   | Unorm        ) },
                { GalTextureFormat.Astc2D10x5,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x5,  GalImageFormat.ASTC_10x5  | Unorm        ) },
                { GalTextureFormat.Astc2D10x6,  new TextureDescriptor(TextureReader.Read16BptCompressedTexture10x6,  GalImageFormat.ASTC_10x6  | Unorm        ) }
            };

        private static readonly Dictionary<GalImageFormat, ImageDescriptor> s_ImageTable =
            new Dictionary<GalImageFormat, ImageDescriptor>()
            {
                { GalImageFormat.R32G32B32A32, new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16B16A16, new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32G32,       new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A8B8G8R8,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A2B10G10R10,  new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R32,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC6H_SF16,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC6H_UF16,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.A1R5G5B5,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.B5G6R5,       new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC7,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16G16,       new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8G8,         new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R16,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.R8,           new ImageDescriptor(true,  false, false) },
                { GalImageFormat.B10G11R11,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC1_RGBA,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC2,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC3,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC4,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.BC5,          new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_4x4,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_5x5,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_6x6,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_8x8,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x10,   new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_12x12,   new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_5x4,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_6x5,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_8x6,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x8,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_12x10,   new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_8x5,     new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x5,    new ImageDescriptor(true,  false, false) },
                { GalImageFormat.ASTC_10x6,    new ImageDescriptor(true,  false, false) },

                { GalImageFormat.D24_S8, new ImageDescriptor(false, true,  true)  },
                { GalImageFormat.D32,    new ImageDescriptor(false, true,  false) },
                { GalImageFormat.D16,    new ImageDescriptor(false, true,  false) },
                { GalImageFormat.D32_S8, new ImageDescriptor(false, true,  true)  },

                { GalImageFormat.R4G4B4A4_REVERSED, new ImageDescriptor(true,  false, false) }
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

            GalTextureType Type = RType;

            GalImageFormat ImageFormat = GetTextureDescriptor(Format).Format;

            GalImageFormat FormatType = GetFormatType(RType);

            if (ImageFormat.HasFlag(FormatType))
            {
                return (ImageFormat & GalImageFormat.FormatMask) | FormatType;
            }
            else
            {
                throw new NotImplementedException("Texture with format " + Format +
                                                  " and component type " + Type + " is not implemented");
            }
        }

        public static GalImageFormat ConvertFrameBuffer(GalFrameBufferFormat Format)
        {
            switch (Format)
            {
                case GalFrameBufferFormat.RGBA32Float:    return GalImageFormat.R32G32B32A32   | Sfloat;
                case GalFrameBufferFormat.RGBA16Float:    return GalImageFormat.R16G16B16A16   | Sfloat;
                case GalFrameBufferFormat.RG32Float:      return GalImageFormat.R32G32         | Sfloat;
                case GalFrameBufferFormat.RG32Sint:       return GalImageFormat.R32G32         | Sint;
                case GalFrameBufferFormat.RG32Uint:       return GalImageFormat.R32G32         | Uint;
                case GalFrameBufferFormat.BGRA8Unorm:     return GalImageFormat.R8G8B8A8       | Unorm; //Is this right?
                case GalFrameBufferFormat.BGRA8Srgb:      return GalImageFormat.A8B8G8R8_SRGB;          //This one might be wrong
                case GalFrameBufferFormat.RGB10A2Unorm:   return GalImageFormat.A2B10G10R10    | Unorm;
                case GalFrameBufferFormat.RGBA8Unorm:     return GalImageFormat.A8B8G8R8       | Unorm;
                case GalFrameBufferFormat.RGBA8Srgb:      return GalImageFormat.A8B8G8R8_SRGB;
                case GalFrameBufferFormat.RGBA8Snorm:     return GalImageFormat.A8B8G8R8       | Snorm;
                case GalFrameBufferFormat.RG16Snorm:      return GalImageFormat.R16G16         | Snorm;
                case GalFrameBufferFormat.RG16Float:      return GalImageFormat.R16G16         | Sfloat;
                case GalFrameBufferFormat.R11G11B10Float: return GalImageFormat.B10G11R11      | Sfloat;
                case GalFrameBufferFormat.R32Float:       return GalImageFormat.R32            | Sfloat;
                case GalFrameBufferFormat.RG8Unorm:       return GalImageFormat.R8G8           | Unorm;
                case GalFrameBufferFormat.RG8Snorm:       return GalImageFormat.R8             | Snorm;
                case GalFrameBufferFormat.R16Float:       return GalImageFormat.R16            | Sfloat;
                case GalFrameBufferFormat.R8Unorm:        return GalImageFormat.R8             | Unorm;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static GalImageFormat ConvertZeta(GalZetaFormat Format)
        {
            switch (Format)
            {
                case GalZetaFormat.Z32Float:      return GalImageFormat.D32    | Sfloat;
                case GalZetaFormat.S8Z24Unorm:    return GalImageFormat.D24_S8 | Unorm;
                case GalZetaFormat.Z16Unorm:      return GalImageFormat.D16    | Unorm;
                //This one might not be Uint, change when a texture uses this format
                case GalZetaFormat.Z32S8X24Float: return GalImageFormat.D32_S8 | Uint;
            }

            throw new NotImplementedException(Format.ToString());
        }

        public static int GetImageSize(GalImage Image)
        {
            switch (Image.Format & GalImageFormat.FormatMask)
            {
                case GalImageFormat.R32G32B32A32:
                    return Image.Width * Image.Height * 16;

                case GalImageFormat.R16G16B16A16:
                case GalImageFormat.D32_S8:
                case GalImageFormat.R32G32:
                    return Image.Width * Image.Height * 8;

                case GalImageFormat.A8B8G8R8:
                case GalImageFormat.A8B8G8R8_SRGB:
                case GalImageFormat.A2B10G10R10:
                case GalImageFormat.R16G16:
                case GalImageFormat.R32:
                case GalImageFormat.D32:
                case GalImageFormat.B10G11R11:
                case GalImageFormat.D24_S8:
                    return Image.Width * Image.Height * 4;

                case GalImageFormat.B4G4R4A4:
                case GalImageFormat.A1R5G5B5:
                case GalImageFormat.B5G6R5:
                case GalImageFormat.R8G8:
                case GalImageFormat.R16:
                case GalImageFormat.D16:
                    return Image.Width * Image.Height * 2;

                case GalImageFormat.R8:
                    return Image.Width * Image.Height;

                case GalImageFormat.BC1_RGBA:
                case GalImageFormat.BC4:
                {
                    return CompressedTextureSize(Image.Width, Image.Height, 4, 4, 8);
                }

                case GalImageFormat.BC6H_SF16:
                case GalImageFormat.BC6H_UF16:
                case GalImageFormat.BC7:
                case GalImageFormat.BC2:
                case GalImageFormat.BC3:
                case GalImageFormat.BC5:
                case GalImageFormat.ASTC_4x4:
                    return CompressedTextureSize(Image.Width, Image.Height, 4, 4, 16);

                case GalImageFormat.ASTC_5x5:
                    return CompressedTextureSize(Image.Width, Image.Height, 5, 5, 16);

                case GalImageFormat.ASTC_6x6:
                    return CompressedTextureSize(Image.Width, Image.Height, 6, 6, 16);

                case GalImageFormat.ASTC_8x8:
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 8, 16);

                case GalImageFormat.ASTC_10x10:
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 10, 16);

                case GalImageFormat.ASTC_12x12:
                    return CompressedTextureSize(Image.Width, Image.Height, 12, 12, 16);

                case GalImageFormat.ASTC_5x4:
                    return CompressedTextureSize(Image.Width, Image.Height, 5, 4, 16);

                case GalImageFormat.ASTC_6x5:
                    return CompressedTextureSize(Image.Width, Image.Height, 6, 5, 16);

                case GalImageFormat.ASTC_8x6:
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 6, 16);

                case GalImageFormat.ASTC_10x8:
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 8, 16);

                case GalImageFormat.ASTC_12x10:
                    return CompressedTextureSize(Image.Width, Image.Height, 12, 10, 16);

                case GalImageFormat.ASTC_8x5:
                    return CompressedTextureSize(Image.Width, Image.Height, 8, 5, 16);

                case GalImageFormat.ASTC_10x5:
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 5, 16);

                case GalImageFormat.ASTC_10x6:
                    return CompressedTextureSize(Image.Width, Image.Height, 10, 6, 16);
            }

            throw new NotImplementedException((Image.Format & GalImageFormat.FormatMask).ToString());
        }

        public static bool HasColor(GalImage Image)
        {
            return GetImageDescriptor(Image.Format).HasColor;
        }

        public static bool HasDepth(GalImage Image)
        {
            return GetImageDescriptor(Image.Format).HasDepth;
        }

        public static bool HasStencil(GalImage Image)
        {
            return GetImageDescriptor(Image.Format).HasStencil;
        }

        public static TextureDescriptor GetTextureDescriptor(GalTextureFormat Format)
        {
            if (s_TextureTable.TryGetValue(Format, out TextureDescriptor Descriptor))
            {
                return Descriptor;
            }

            throw new NotImplementedException("Texture with format code " + ((int)Format).ToString("x2") + " not implemented");
        }

        private static ImageDescriptor GetImageDescriptor(GalImageFormat Format)
        {
            GalImageFormat TypeLess = (Format & GalImageFormat.FormatMask);

            if (s_ImageTable.TryGetValue(TypeLess, out ImageDescriptor Descriptor))
            {
                return Descriptor;
            }

            throw new NotImplementedException("Image with format " + TypeLess.ToString() + "not implemented");
        }

        private static GalImageFormat GetFormatType(GalTextureType Type)
        {
            switch (Type)
            {
                case GalTextureType.Snorm: return Snorm;
                case GalTextureType.Unorm: return Unorm;
                case GalTextureType.Sint:  return Sint;
                case GalTextureType.Uint:  return Uint;
                case GalTextureType.Float: return Sfloat;

                default: throw new NotImplementedException(((int)Type).ToString());
            }
        }

        private static int CompressedTextureSize(int TextureWidth, int TextureHeight, int BlockWidth, int BlockHeight, int Bpb)
        {
            int W = (TextureWidth + (BlockWidth - 1)) / BlockWidth;
            int H = (TextureHeight + (BlockHeight - 1)) / BlockHeight;

            return W * H * Bpb;
        }
    }
}