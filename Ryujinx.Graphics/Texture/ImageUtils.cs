using ChocolArm64.Memory;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Texture
{
    public static class ImageUtils
    {
        private struct ImageDescriptor
        {
            public int BytesPerPixel;
            public int BlockWidth;
            public int BlockHeight;

            public bool HasColor;
            public bool HasDepth;
            public bool HasStencil;

            public bool Compressed;

            public ImageDescriptor(
                int  BytesPerPixel,
                int  BlockWidth,
                int  BlockHeight,
                bool HasColor,
                bool HasDepth,
                bool HasStencil,
                bool Compressed)
            {
                this.BytesPerPixel = BytesPerPixel;
                this.BlockWidth    = BlockWidth;
                this.BlockHeight   = BlockHeight;
                this.HasColor      = HasColor;
                this.HasDepth      = HasDepth;
                this.HasStencil    = HasStencil;
                this.Compressed    = Compressed;
            }
        }

        private const GalImageFormat Snorm  = GalImageFormat.Snorm;
        private const GalImageFormat Unorm  = GalImageFormat.Unorm;
        private const GalImageFormat Sint   = GalImageFormat.Sint;
        private const GalImageFormat Uint   = GalImageFormat.Uint;
        private const GalImageFormat Sfloat = GalImageFormat.Sfloat;

        private static readonly Dictionary<GalTextureFormat, GalImageFormat> s_TextureTable =
                            new Dictionary<GalTextureFormat, GalImageFormat>()
            {
                { GalTextureFormat.R32G32B32A32, GalImageFormat.R32G32B32A32                 | Sint | Uint | Sfloat },
                { GalTextureFormat.R16G16B16A16, GalImageFormat.R16G16B16A16 | Snorm | Unorm | Sint | Uint | Sfloat },
                { GalTextureFormat.R32G32,       GalImageFormat.R32G32                       | Sint | Uint | Sfloat },
                { GalTextureFormat.A8B8G8R8,     GalImageFormat.A8B8G8R8     | Snorm | Unorm | Sint | Uint          },
                { GalTextureFormat.A2B10G10R10,  GalImageFormat.A2B10G10R10  | Snorm | Unorm | Sint | Uint          },
                { GalTextureFormat.G8R8,         GalImageFormat.G8R8         | Snorm | Unorm | Sint | Uint          },
                { GalTextureFormat.R16,          GalImageFormat.R16          | Snorm | Unorm | Sint | Uint | Sfloat },
                { GalTextureFormat.R8,           GalImageFormat.R8           | Snorm | Unorm | Sint | Uint          },
                { GalTextureFormat.R16_G16,      GalImageFormat.R16G16       | Snorm                                },
                { GalTextureFormat.R32,          GalImageFormat.R32                          | Sint | Uint | Sfloat },
                { GalTextureFormat.A4B4G4R4,     GalImageFormat.A4B4G4R4             | Unorm                        },
                { GalTextureFormat.A1B5G5R5,     GalImageFormat.A1R5G5B5             | Unorm                        },
                { GalTextureFormat.B5G6R5,       GalImageFormat.B5G6R5               | Unorm                        },
                { GalTextureFormat.BF10GF11RF11, GalImageFormat.B10G11R11                                  | Sfloat },
                { GalTextureFormat.Z24S8,        GalImageFormat.D24_S8               | Unorm                        },
                { GalTextureFormat.ZF32,         GalImageFormat.D32                                        | Sfloat },
                { GalTextureFormat.ZF32_X24S8,   GalImageFormat.D32_S8               | Unorm                        },
                { GalTextureFormat.Z16,          GalImageFormat.D16                  | Unorm                        },

                //Compressed formats
                { GalTextureFormat.BC6H_SF16,   GalImageFormat.BC6H_SF16  | Unorm                  },
                { GalTextureFormat.BC6H_UF16,   GalImageFormat.BC6H_UF16                  | Sfloat },
                { GalTextureFormat.BC7U,        GalImageFormat.BC7        | Unorm                  },
                { GalTextureFormat.BC1,         GalImageFormat.BC1_RGBA   | Unorm                  },
                { GalTextureFormat.BC2,         GalImageFormat.BC2        | Unorm                  },
                { GalTextureFormat.BC3,         GalImageFormat.BC3        | Unorm                  },
                { GalTextureFormat.BC4,         GalImageFormat.BC4        | Unorm | Snorm          },
                { GalTextureFormat.BC5,         GalImageFormat.BC5        | Unorm | Snorm          },
                { GalTextureFormat.Astc2D4x4,   GalImageFormat.ASTC_4x4   | Unorm                  },
                { GalTextureFormat.Astc2D5x5,   GalImageFormat.ASTC_5x5   | Unorm                  },
                { GalTextureFormat.Astc2D6x6,   GalImageFormat.ASTC_6x6   | Unorm                  },
                { GalTextureFormat.Astc2D8x8,   GalImageFormat.ASTC_8x8   | Unorm                  },
                { GalTextureFormat.Astc2D10x10, GalImageFormat.ASTC_10x10 | Unorm                  },
                { GalTextureFormat.Astc2D12x12, GalImageFormat.ASTC_12x12 | Unorm                  },
                { GalTextureFormat.Astc2D5x4,   GalImageFormat.ASTC_5x4   | Unorm                  },
                { GalTextureFormat.Astc2D6x5,   GalImageFormat.ASTC_6x5   | Unorm                  },
                { GalTextureFormat.Astc2D8x6,   GalImageFormat.ASTC_8x6   | Unorm                  },
                { GalTextureFormat.Astc2D10x8,  GalImageFormat.ASTC_10x8  | Unorm                  },
                { GalTextureFormat.Astc2D12x10, GalImageFormat.ASTC_12x10 | Unorm                  },
                { GalTextureFormat.Astc2D8x5,   GalImageFormat.ASTC_8x5   | Unorm                  },
                { GalTextureFormat.Astc2D10x5,  GalImageFormat.ASTC_10x5  | Unorm                  },
                { GalTextureFormat.Astc2D10x6,  GalImageFormat.ASTC_10x6  | Unorm                  }
            };

        private static readonly Dictionary<GalImageFormat, ImageDescriptor> s_ImageTable =
                            new Dictionary<GalImageFormat, ImageDescriptor>()
        {
            { GalImageFormat.R32G32B32A32,  new ImageDescriptor(16, 1,  1,  true, false, false, false) },
            { GalImageFormat.R16G16B16A16,  new ImageDescriptor(8,  1,  1,  true, false, false, false) },
            { GalImageFormat.R32G32,        new ImageDescriptor(8,  1,  1,  true, false, false, false) },
            { GalImageFormat.A8B8G8R8,      new ImageDescriptor(4,  1,  1,  true, false, false, false) },
            { GalImageFormat.A2B10G10R10,   new ImageDescriptor(4,  1,  1,  true, false, false, false) },
            { GalImageFormat.R32,           new ImageDescriptor(4,  1,  1,  true, false, false, false) },
            { GalImageFormat.A4B4G4R4,      new ImageDescriptor(2,  1,  1,  true, false, false, false) },
            { GalImageFormat.BC6H_SF16,     new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.BC6H_UF16,     new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.A1R5G5B5,      new ImageDescriptor(2,  1,  1,  true, false, false, false) },
            { GalImageFormat.B5G6R5,        new ImageDescriptor(2,  1,  1,  true, false, false, false) },
            { GalImageFormat.BC7,           new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.R16G16,        new ImageDescriptor(4,  1,  1,  true, false, false, false) },
            { GalImageFormat.R8G8,          new ImageDescriptor(2,  1,  1,  true, false, false, false) },
            { GalImageFormat.G8R8,          new ImageDescriptor(2,  1,  1,  true, false, false, false) },
            { GalImageFormat.R16,           new ImageDescriptor(2,  1,  1,  true, false, false, false) },
            { GalImageFormat.R8,            new ImageDescriptor(1,  1,  1,  true, false, false, false) },
            { GalImageFormat.B10G11R11,     new ImageDescriptor(4,  1,  1,  true, false, false, false) },
            { GalImageFormat.A8B8G8R8_SRGB, new ImageDescriptor(4,  1,  1,  true, false, false, false) },
            { GalImageFormat.BC1_RGBA,      new ImageDescriptor(8,  4,  4,  true, false, false, true)  },
            { GalImageFormat.BC2,           new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.BC3,           new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.BC4,           new ImageDescriptor(8,  4,  4,  true, false, false, true)  },
            { GalImageFormat.BC5,           new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.ASTC_4x4,      new ImageDescriptor(16, 4,  4,  true, false, false, true)  },
            { GalImageFormat.ASTC_5x5,      new ImageDescriptor(16, 5,  5,  true, false, false, true)  },
            { GalImageFormat.ASTC_6x6,      new ImageDescriptor(16, 6,  6,  true, false, false, true)  },
            { GalImageFormat.ASTC_8x8,      new ImageDescriptor(16, 8,  8,  true, false, false, true)  },
            { GalImageFormat.ASTC_10x10,    new ImageDescriptor(16, 10, 10, true, false, false, true)  },
            { GalImageFormat.ASTC_12x12,    new ImageDescriptor(16, 12, 12, true, false, false, true)  },
            { GalImageFormat.ASTC_5x4,      new ImageDescriptor(16, 5,  4,  true, false, false, true)  },
            { GalImageFormat.ASTC_6x5,      new ImageDescriptor(16, 6,  5,  true, false, false, true)  },
            { GalImageFormat.ASTC_8x6,      new ImageDescriptor(16, 8,  6,  true, false, false, true)  },
            { GalImageFormat.ASTC_10x8,     new ImageDescriptor(16, 10, 8,  true, false, false, true)  },
            { GalImageFormat.ASTC_12x10,    new ImageDescriptor(16, 12, 10, true, false, false, true)  },
            { GalImageFormat.ASTC_8x5,      new ImageDescriptor(16, 8,  5,  true, false, false, true)  },
            { GalImageFormat.ASTC_10x5,     new ImageDescriptor(16, 10, 5,  true, false, false, true)  },
            { GalImageFormat.ASTC_10x6,     new ImageDescriptor(16, 10, 6,  true, false, false, true)  },

            { GalImageFormat.D24_S8, new ImageDescriptor(4, 1, 1, false, true, true,  false) },
            { GalImageFormat.D32,    new ImageDescriptor(4, 1, 1, false, true, false, false) },
            { GalImageFormat.D16,    new ImageDescriptor(2, 1, 1, false, true, false, false) },
            { GalImageFormat.D32_S8, new ImageDescriptor(8, 1, 1, false, true, true,  false) },
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

            if (!s_TextureTable.TryGetValue(Format, out GalImageFormat ImageFormat))
            {
                throw new NotImplementedException("Texture with format " + ((int)Format).ToString("x2") + " not implemented");
            }

            GalTextureType Type = RType;

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

        public static GalImageFormat ConvertSurface(GalSurfaceFormat Format)
        {
            switch (Format)
            {
                case GalSurfaceFormat.RGBA32Float:    return GalImageFormat.R32G32B32A32   | Sfloat;
                case GalSurfaceFormat.RGBA32Uint:     return GalImageFormat.R32G32B32A32   | Uint;
                case GalSurfaceFormat.RGBA16Float:    return GalImageFormat.R16G16B16A16   | Sfloat;
                case GalSurfaceFormat.RG32Float:      return GalImageFormat.R32G32         | Sfloat;
                case GalSurfaceFormat.RG32Sint:       return GalImageFormat.R32G32         | Sint;
                case GalSurfaceFormat.RG32Uint:       return GalImageFormat.R32G32         | Uint;
                case GalSurfaceFormat.BGRA8Unorm:     return GalImageFormat.R8G8B8A8       | Unorm; //Is this right?
                case GalSurfaceFormat.BGRA8Srgb:      return GalImageFormat.A8B8G8R8_SRGB;          //This one might be wrong
                case GalSurfaceFormat.RGB10A2Unorm:   return GalImageFormat.A2B10G10R10    | Unorm;
                case GalSurfaceFormat.RGBA8Unorm:     return GalImageFormat.A8B8G8R8       | Unorm;
                case GalSurfaceFormat.RGBA8Srgb:      return GalImageFormat.A8B8G8R8_SRGB;
                case GalSurfaceFormat.RGBA8Snorm:     return GalImageFormat.A8B8G8R8       | Snorm;
                case GalSurfaceFormat.RG16Snorm:      return GalImageFormat.R16G16         | Snorm;
                case GalSurfaceFormat.RG16Float:      return GalImageFormat.R16G16         | Sfloat;
                case GalSurfaceFormat.R11G11B10Float: return GalImageFormat.B10G11R11      | Sfloat;
                case GalSurfaceFormat.R32Float:       return GalImageFormat.R32            | Sfloat;
                case GalSurfaceFormat.RG8Unorm:       return GalImageFormat.R8G8           | Unorm;
                case GalSurfaceFormat.RG8Snorm:       return GalImageFormat.R8             | Snorm;
                case GalSurfaceFormat.R16Float:       return GalImageFormat.R16            | Sfloat;
                case GalSurfaceFormat.R16Unorm:       return GalImageFormat.R16            | Unorm;
                case GalSurfaceFormat.R8Unorm:        return GalImageFormat.R8             | Unorm;
                case GalSurfaceFormat.B5G6R5Unorm:    return GalImageFormat.B5G6R5         | Unorm;
                case GalSurfaceFormat.BGR5A1Unorm:    return GalImageFormat.A1R5G5B5       | Unorm;
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

        public static byte[] ReadTexture(IAMemory Memory, GalImage Image, long Position)
        {
            AMemory CpuMemory;

            if (Memory is NvGpuVmm Vmm)
            {
                CpuMemory = Vmm.Memory;
            }
            else
            {
                CpuMemory = (AMemory)Memory;
            }

            ISwizzle Swizzle = TextureHelper.GetSwizzle(Image);

            ImageDescriptor Desc = GetImageDescriptor(Image.Format);

            (int Width, int Height) = GetImageSizeInBlocks(Image);

            int BytesPerPixel = Desc.BytesPerPixel;

            int OutOffs = 0;

            byte[] Data = new byte[Width * Height * BytesPerPixel];

            for (int Y = 0; Y < Height; Y++)
            for (int X = 0; X < Width;  X++)
            {
                long Offset = (uint)Swizzle.GetSwizzleOffset(X, Y);

                CpuMemory.ReadBytes(Position + Offset, Data, OutOffs, BytesPerPixel);

                OutOffs += BytesPerPixel;
            }

            return Data;
        }

        public static void WriteTexture(NvGpuVmm Vmm, GalImage Image, long Position, byte[] Data)
        {
            ISwizzle Swizzle = TextureHelper.GetSwizzle(Image);

            ImageDescriptor Desc = GetImageDescriptor(Image.Format);

            (int Width, int Height) = ImageUtils.GetImageSizeInBlocks(Image);

            int BytesPerPixel = Desc.BytesPerPixel;

            int InOffs = 0;

            for (int Y = 0; Y < Height; Y++)
            for (int X = 0; X < Width;  X++)
            {
                long Offset = (uint)Swizzle.GetSwizzleOffset(X, Y);

                Vmm.Memory.WriteBytes(Position + Offset, Data, InOffs, BytesPerPixel);

                InOffs += BytesPerPixel;
            }
        }

        public static int GetSize(GalImage Image)
        {
            ImageDescriptor Desc = GetImageDescriptor(Image.Format);

            int Width  = DivRoundUp(Image.Width,  Desc.BlockWidth);
            int Height = DivRoundUp(Image.Height, Desc.BlockHeight);

            return Desc.BytesPerPixel * Width * Height;
        }

        public static int GetPitch(GalImageFormat Format, int Width)
        {
            ImageDescriptor Desc = GetImageDescriptor(Format);

            return Desc.BytesPerPixel * DivRoundUp(Width, Desc.BlockWidth);
        }

        public static int GetBlockWidth(GalImageFormat Format)
        {
            return GetImageDescriptor(Format).BlockWidth;
        }

        public static (int Width, int Height) GetImageSizeInBlocks(GalImage Image)
        {
            ImageDescriptor Desc = GetImageDescriptor(Image.Format);

            return (DivRoundUp(Image.Width,  Desc.BlockWidth),
                    DivRoundUp(Image.Height, Desc.BlockHeight));
        }

        public static int GetBytesPerPixel(GalImageFormat Format)
        {
            return GetImageDescriptor(Format).BytesPerPixel;
        }

        private static int DivRoundUp(int LHS, int RHS)
        {
            return (LHS + (RHS - 1)) / RHS;
        }

        public static bool HasColor(GalImageFormat Format)
        {
            return GetImageDescriptor(Format).HasColor;
        }

        public static bool HasDepth(GalImageFormat Format)
        {
            return GetImageDescriptor(Format).HasDepth;
        }

        public static bool HasStencil(GalImageFormat Format)
        {
            return GetImageDescriptor(Format).HasStencil;
        }

        public static bool IsCompressed(GalImageFormat Format)
        {
            return GetImageDescriptor(Format).Compressed;
        }

        private static ImageDescriptor GetImageDescriptor(GalImageFormat Format)
        {
            GalImageFormat TypeLess = (Format & GalImageFormat.FormatMask);

            if (s_ImageTable.TryGetValue(TypeLess, out ImageDescriptor Descriptor))
            {
                return Descriptor;
            }

            throw new NotImplementedException("Image with format " + TypeLess.ToString() + " not implemented");
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
    }
}