using ChocolArm64.Memory;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Texture
{
    public static class ImageUtils
    {
        [Flags]
        private enum TargetBuffer
        {
            Color   = 1 << 0,
            Depth   = 1 << 1,
            Stencil = 1 << 2,

            DepthStencil = Depth | Stencil
        }

        private struct ImageDescriptor
        {
            public int BytesPerPixel { get; private set; }
            public int BlockWidth    { get; private set; }
            public int BlockHeight   { get; private set; }

            public TargetBuffer Target { get; private set; }

            public ImageDescriptor(int bytesPerPixel, int blockWidth, int blockHeight, TargetBuffer target)
            {
                BytesPerPixel = bytesPerPixel;
                BlockWidth    = blockWidth;
                BlockHeight   = blockHeight;
                Target        = target;
            }
        }

        private const GalImageFormat Snorm = GalImageFormat.Snorm;
        private const GalImageFormat Unorm = GalImageFormat.Unorm;
        private const GalImageFormat Sint  = GalImageFormat.Sint;
        private const GalImageFormat Uint  = GalImageFormat.Uint;
        private const GalImageFormat Float = GalImageFormat.Float;
        private const GalImageFormat Srgb  = GalImageFormat.Srgb;

        private static readonly Dictionary<GalTextureFormat, GalImageFormat> STextureTable =
                            new Dictionary<GalTextureFormat, GalImageFormat>()
        {
            { GalTextureFormat.Rgba32,     GalImageFormat.Rgba32                    | Sint | Uint | Float        },
            { GalTextureFormat.Rgba16,     GalImageFormat.Rgba16    | Snorm | Unorm | Sint | Uint | Float        },
            { GalTextureFormat.Rg32,       GalImageFormat.Rg32                      | Sint | Uint | Float        },
            { GalTextureFormat.Rgba8,      GalImageFormat.Rgba8     | Snorm | Unorm | Sint | Uint         | Srgb },
            { GalTextureFormat.Rgb10A2,    GalImageFormat.Rgb10A2   | Snorm | Unorm | Sint | Uint                },
            { GalTextureFormat.Rg8,        GalImageFormat.Rg8       | Snorm | Unorm | Sint | Uint                },
            { GalTextureFormat.R16,        GalImageFormat.R16       | Snorm | Unorm | Sint | Uint | Float        },
            { GalTextureFormat.R8,         GalImageFormat.R8        | Snorm | Unorm | Sint | Uint                },
            { GalTextureFormat.Rg16,       GalImageFormat.Rg16      | Snorm | Unorm               | Float        },
            { GalTextureFormat.R32,        GalImageFormat.R32                       | Sint | Uint | Float        },
            { GalTextureFormat.Rgba4,      GalImageFormat.Rgba4             | Unorm                              },
            { GalTextureFormat.Rgb5A1,     GalImageFormat.Rgb5A1            | Unorm                              },
            { GalTextureFormat.Rgb565,     GalImageFormat.Rgb565            | Unorm                              },
            { GalTextureFormat.R11G11B10F, GalImageFormat.R11G11B10                               | Float        },
            { GalTextureFormat.D24S8,      GalImageFormat.D24S8             | Unorm        | Uint                },
            { GalTextureFormat.D32F,       GalImageFormat.D32                                     | Float        },
            { GalTextureFormat.D32Fx24S8,  GalImageFormat.D32S8             | Unorm                              },
            { GalTextureFormat.D16,        GalImageFormat.D16               | Unorm                              },

            //Compressed formats
            { GalTextureFormat.BptcSfloat,  GalImageFormat.BptcSfloat                  | Float        },
            { GalTextureFormat.BptcUfloat,  GalImageFormat.BptcUfloat                  | Float        },
            { GalTextureFormat.BptcUnorm,   GalImageFormat.BptcUnorm   | Unorm                 | Srgb },
            { GalTextureFormat.Bc1,         GalImageFormat.Bc1         | Unorm                 | Srgb },
            { GalTextureFormat.Bc2,         GalImageFormat.Bc2         | Unorm                 | Srgb },
            { GalTextureFormat.Bc3,         GalImageFormat.Bc3         | Unorm                 | Srgb },
            { GalTextureFormat.Bc4,         GalImageFormat.Bc4         | Unorm | Snorm                },
            { GalTextureFormat.Bc5,         GalImageFormat.Bc5         | Unorm | Snorm                },
            { GalTextureFormat.Astc2D4X4,   GalImageFormat.Astc2D4X4   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D5X5,   GalImageFormat.Astc2D5X5   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D6X6,   GalImageFormat.Astc2D6X6   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D8X8,   GalImageFormat.Astc2D8X8   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10X10, GalImageFormat.Astc2D10X10 | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D12X12, GalImageFormat.Astc2D12X12 | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D5X4,   GalImageFormat.Astc2D5X4   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D6X5,   GalImageFormat.Astc2D6X5   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D8X6,   GalImageFormat.Astc2D8X6   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10X8,  GalImageFormat.Astc2D10X8  | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D12X10, GalImageFormat.Astc2D12X10 | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D8X5,   GalImageFormat.Astc2D8X5   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10X5,  GalImageFormat.Astc2D10X5  | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10X6,  GalImageFormat.Astc2D10X6  | Unorm                 | Srgb }
        };

        private static readonly Dictionary<GalImageFormat, ImageDescriptor> SImageTable =
                            new Dictionary<GalImageFormat, ImageDescriptor>()
        {
            { GalImageFormat.Rgba32,      new ImageDescriptor(16, 1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rgba16,      new ImageDescriptor(8,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rg32,        new ImageDescriptor(8,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rgba8,       new ImageDescriptor(4,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Bgra8,       new ImageDescriptor(4,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rgb10A2,     new ImageDescriptor(4,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R32,         new ImageDescriptor(4,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rgba4,       new ImageDescriptor(2,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BptcSfloat,  new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.BptcUfloat,  new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Rgb5A1,      new ImageDescriptor(2,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rgb565,      new ImageDescriptor(2,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BptcUnorm,   new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Rg16,        new ImageDescriptor(4,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Rg8,         new ImageDescriptor(2,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R16,         new ImageDescriptor(2,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R8,          new ImageDescriptor(1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R11G11B10,   new ImageDescriptor(4,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.Bc1,         new ImageDescriptor(8,  4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Bc2,         new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Bc3,         new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Bc4,         new ImageDescriptor(8,  4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Bc5,         new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D4X4,   new ImageDescriptor(16, 4,  4,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D5X5,   new ImageDescriptor(16, 5,  5,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D6X6,   new ImageDescriptor(16, 6,  6,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D8X8,   new ImageDescriptor(16, 8,  8,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10X10, new ImageDescriptor(16, 10, 10, TargetBuffer.Color) },
            { GalImageFormat.Astc2D12X12, new ImageDescriptor(16, 12, 12, TargetBuffer.Color) },
            { GalImageFormat.Astc2D5X4,   new ImageDescriptor(16, 5,  4,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D6X5,   new ImageDescriptor(16, 6,  5,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D8X6,   new ImageDescriptor(16, 8,  6,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10X8,  new ImageDescriptor(16, 10, 8,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D12X10, new ImageDescriptor(16, 12, 10, TargetBuffer.Color) },
            { GalImageFormat.Astc2D8X5,   new ImageDescriptor(16, 8,  5,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10X5,  new ImageDescriptor(16, 10, 5,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10X6,  new ImageDescriptor(16, 10, 6,  TargetBuffer.Color) },

            { GalImageFormat.D24S8, new ImageDescriptor(4, 1, 1, TargetBuffer.DepthStencil) },
            { GalImageFormat.D32,   new ImageDescriptor(4, 1, 1, TargetBuffer.Depth)        },
            { GalImageFormat.D16,   new ImageDescriptor(2, 1, 1, TargetBuffer.Depth)        },
            { GalImageFormat.D32S8, new ImageDescriptor(8, 1, 1, TargetBuffer.DepthStencil) }
        };

        public static GalImageFormat ConvertTexture(
            GalTextureFormat format,
            GalTextureType   rType,
            GalTextureType   gType,
            GalTextureType   bType,
            GalTextureType   aType,
            bool             convSrgb)
        {
            if (rType != gType || rType != bType || rType != aType)
            {
                throw new NotImplementedException("Per component types are not implemented!");
            }

            if (!STextureTable.TryGetValue(format, out GalImageFormat imageFormat))
            {
                throw new NotImplementedException($"Format 0x{((int)format):x} not implemented!");
            }

            GalImageFormat formatType = convSrgb ? Srgb : GetFormatType(rType);

            GalImageFormat combinedFormat = (imageFormat & GalImageFormat.FormatMask) | formatType;

            if (!imageFormat.HasFlag(formatType))
            {
                throw new NotImplementedException($"Format \"{combinedFormat}\" not implemented!");
            }

            return combinedFormat;
        }

        public static GalImageFormat ConvertSurface(GalSurfaceFormat format)
        {
            switch (format)
            {
                case GalSurfaceFormat.Rgba32Float:    return GalImageFormat.Rgba32    | Float;
                case GalSurfaceFormat.Rgba32Uint:     return GalImageFormat.Rgba32    | Uint;
                case GalSurfaceFormat.Rgba16Float:    return GalImageFormat.Rgba16    | Float;
                case GalSurfaceFormat.Rg32Float:      return GalImageFormat.Rg32      | Float;
                case GalSurfaceFormat.Rg32Sint:       return GalImageFormat.Rg32      | Sint;
                case GalSurfaceFormat.Rg32Uint:       return GalImageFormat.Rg32      | Uint;
                case GalSurfaceFormat.Bgra8Unorm:     return GalImageFormat.Bgra8     | Unorm;
                case GalSurfaceFormat.Bgra8Srgb:      return GalImageFormat.Bgra8     | Srgb;
                case GalSurfaceFormat.Rgb10A2Unorm:   return GalImageFormat.Rgb10A2   | Unorm;
                case GalSurfaceFormat.Rgba8Unorm:     return GalImageFormat.Rgba8     | Unorm;
                case GalSurfaceFormat.Rgba8Srgb:      return GalImageFormat.Rgba8     | Srgb;
                case GalSurfaceFormat.Rgba8Snorm:     return GalImageFormat.Rgba8     | Snorm;
                case GalSurfaceFormat.Rg16Snorm:      return GalImageFormat.Rg16      | Snorm;
                case GalSurfaceFormat.Rg16Unorm:      return GalImageFormat.Rg16      | Unorm;
                case GalSurfaceFormat.Rg16Float:      return GalImageFormat.Rg16      | Float;
                case GalSurfaceFormat.R11G11B10Float: return GalImageFormat.R11G11B10 | Float;
                case GalSurfaceFormat.R32Float:       return GalImageFormat.R32       | Float;
                case GalSurfaceFormat.R32Uint:        return GalImageFormat.R32       | Uint;
                case GalSurfaceFormat.Rg8Unorm:       return GalImageFormat.Rg8       | Unorm;
                case GalSurfaceFormat.Rg8Snorm:       return GalImageFormat.Rg8       | Snorm;
                case GalSurfaceFormat.R16Float:       return GalImageFormat.R16       | Float;
                case GalSurfaceFormat.R16Unorm:       return GalImageFormat.R16       | Unorm;
                case GalSurfaceFormat.R16Uint:        return GalImageFormat.R16       | Uint;
                case GalSurfaceFormat.R8Unorm:        return GalImageFormat.R8        | Unorm;
                case GalSurfaceFormat.R8Uint:         return GalImageFormat.R8        | Uint;
                case GalSurfaceFormat.B5G6R5Unorm:    return GalImageFormat.Rgb565    | Unorm;
                case GalSurfaceFormat.Bgr5A1Unorm:    return GalImageFormat.Bgr5A1    | Unorm;
            }

            throw new NotImplementedException(format.ToString());
        }

        public static GalImageFormat ConvertZeta(GalZetaFormat format)
        {
            switch (format)
            {
                case GalZetaFormat.D32Float:      return GalImageFormat.D32   | Float;
                case GalZetaFormat.S8D24Unorm:    return GalImageFormat.D24S8 | Unorm;
                case GalZetaFormat.D16Unorm:      return GalImageFormat.D16   | Unorm;
                case GalZetaFormat.D24S8Unorm:    return GalImageFormat.D24S8 | Unorm;
                case GalZetaFormat.D32S8X24Float: return GalImageFormat.D32S8 | Float;
            }

            throw new NotImplementedException(format.ToString());
        }

        public static byte[] ReadTexture(IAMemory memory, GalImage image, long position)
        {
            AMemory cpuMemory;

            if (memory is NvGpuVmm vmm)
            {
                cpuMemory = vmm.Memory;
            }
            else
            {
                cpuMemory = (AMemory)memory;
            }

            ISwizzle swizzle = TextureHelper.GetSwizzle(image);

            ImageDescriptor desc = GetImageDescriptor(image.Format);

            (int width, int height) = GetImageSizeInBlocks(image);

            int bytesPerPixel = desc.BytesPerPixel;

            //Note: Each row of the texture needs to be aligned to 4 bytes.
            int pitch = (width * bytesPerPixel + 3) & ~3;

            byte[] data = new byte[height * pitch];

            for (int y = 0; y < height; y++)
            {
                int outOffs = y * pitch;

                for (int x = 0; x < width;  x++)
                {
                    long offset = (uint)swizzle.GetSwizzleOffset(x, y);

                    cpuMemory.ReadBytes(position + offset, data, outOffs, bytesPerPixel);

                    outOffs += bytesPerPixel;
                }
            }

            return data;
        }

        public static void WriteTexture(NvGpuVmm vmm, GalImage image, long position, byte[] data)
        {
            ISwizzle swizzle = TextureHelper.GetSwizzle(image);

            ImageDescriptor desc = GetImageDescriptor(image.Format);

            (int width, int height) = GetImageSizeInBlocks(image);

            int bytesPerPixel = desc.BytesPerPixel;

            int inOffs = 0;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                long offset = (uint)swizzle.GetSwizzleOffset(x, y);

                vmm.Memory.WriteBytes(position + offset, data, inOffs, bytesPerPixel);

                inOffs += bytesPerPixel;
            }
        }

        public static int GetSize(GalImage image)
        {
            ImageDescriptor desc = GetImageDescriptor(image.Format);

            int width  = DivRoundUp(image.Width,  desc.BlockWidth);
            int height = DivRoundUp(image.Height, desc.BlockHeight);

            return desc.BytesPerPixel * width * height;
        }

        public static int GetPitch(GalImageFormat format, int width)
        {
            ImageDescriptor desc = GetImageDescriptor(format);

            int pitch = desc.BytesPerPixel * DivRoundUp(width, desc.BlockWidth);

            pitch = (pitch + 0x1f) & ~0x1f;

            return pitch;
        }

        public static int GetBlockWidth(GalImageFormat format)
        {
            return GetImageDescriptor(format).BlockWidth;
        }

        public static int GetBlockHeight(GalImageFormat format)
        {
            return GetImageDescriptor(format).BlockHeight;
        }

        public static int GetAlignedWidth(GalImage image)
        {
            ImageDescriptor desc = GetImageDescriptor(image.Format);

             int alignMask;

            if (image.Layout == GalMemoryLayout.BlockLinear)
            {
                alignMask = image.TileWidth * (64 / desc.BytesPerPixel) - 1;
            }
            else
            {
                alignMask = (32 / desc.BytesPerPixel) - 1;
            }

            return (image.Width + alignMask) & ~alignMask;
        }

        public static (int Width, int Height) GetImageSizeInBlocks(GalImage image)
        {
            ImageDescriptor desc = GetImageDescriptor(image.Format);

            return (DivRoundUp(image.Width,  desc.BlockWidth),
                    DivRoundUp(image.Height, desc.BlockHeight));
        }

        public static int GetBytesPerPixel(GalImageFormat format)
        {
            return GetImageDescriptor(format).BytesPerPixel;
        }

        private static int DivRoundUp(int lhs, int rhs)
        {
            return (lhs + (rhs - 1)) / rhs;
        }

        public static bool HasColor(GalImageFormat format)
        {
            return (GetImageDescriptor(format).Target & TargetBuffer.Color) != 0;
        }

        public static bool HasDepth(GalImageFormat format)
        {
            return (GetImageDescriptor(format).Target & TargetBuffer.Depth) != 0;
        }

        public static bool HasStencil(GalImageFormat format)
        {
            return (GetImageDescriptor(format).Target & TargetBuffer.Stencil) != 0;
        }

        public static bool IsCompressed(GalImageFormat format)
        {
            ImageDescriptor desc = GetImageDescriptor(format);

            return (desc.BlockWidth | desc.BlockHeight) != 1;
        }

        private static ImageDescriptor GetImageDescriptor(GalImageFormat format)
        {
            GalImageFormat pixelFormat = format & GalImageFormat.FormatMask;

            if (SImageTable.TryGetValue(pixelFormat, out ImageDescriptor descriptor))
            {
                return descriptor;
            }

            throw new NotImplementedException($"Format \"{pixelFormat}\" not implemented!");
        }

        private static GalImageFormat GetFormatType(GalTextureType type)
        {
            switch (type)
            {
                case GalTextureType.Snorm: return Snorm;
                case GalTextureType.Unorm: return Unorm;
                case GalTextureType.Sint:  return Sint;
                case GalTextureType.Uint:  return Uint;
                case GalTextureType.Float: return Float;

                default: throw new NotImplementedException(((int)type).ToString());
            }
        }
    }
}