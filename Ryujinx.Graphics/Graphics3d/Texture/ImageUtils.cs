using ChocolArm64.Memory;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common;
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
            public int BlockDepth    { get; private set; }

            public TargetBuffer Target { get; private set; }

            public ImageDescriptor(int bytesPerPixel, int blockWidth, int blockHeight, int blockDepth, TargetBuffer target)
            {
                this.BytesPerPixel = bytesPerPixel;
                this.BlockWidth    = blockWidth;
                this.BlockHeight   = blockHeight;
                this.BlockDepth    = blockDepth;
                this.Target        = target;
            }
        }

        private const GalImageFormat Snorm = GalImageFormat.Snorm;
        private const GalImageFormat Unorm = GalImageFormat.Unorm;
        private const GalImageFormat Sint  = GalImageFormat.Sint;
        private const GalImageFormat Uint  = GalImageFormat.Uint;
        private const GalImageFormat Float = GalImageFormat.Float;
        private const GalImageFormat Srgb  = GalImageFormat.Srgb;

        private static readonly Dictionary<GalTextureFormat, GalImageFormat> TextureTable =
                            new Dictionary<GalTextureFormat, GalImageFormat>()
        {
            { GalTextureFormat.RGBA32,     GalImageFormat.RGBA32                    | Sint | Uint | Float        },
            { GalTextureFormat.RGBA16,     GalImageFormat.RGBA16    | Snorm | Unorm | Sint | Uint | Float        },
            { GalTextureFormat.RG32,       GalImageFormat.RG32                      | Sint | Uint | Float        },
            { GalTextureFormat.RGBA8,      GalImageFormat.RGBA8     | Snorm | Unorm | Sint | Uint         | Srgb },
            { GalTextureFormat.RGB10A2,    GalImageFormat.RGB10A2   | Snorm | Unorm | Sint | Uint                },
            { GalTextureFormat.RG8,        GalImageFormat.RG8       | Snorm | Unorm | Sint | Uint                },
            { GalTextureFormat.R16,        GalImageFormat.R16       | Snorm | Unorm | Sint | Uint | Float        },
            { GalTextureFormat.R8,         GalImageFormat.R8        | Snorm | Unorm | Sint | Uint                },
            { GalTextureFormat.RG16,       GalImageFormat.RG16      | Snorm | Unorm | Sint        | Float        },
            { GalTextureFormat.R32,        GalImageFormat.R32                       | Sint | Uint | Float        },
            { GalTextureFormat.RGBA4,      GalImageFormat.RGBA4             | Unorm                              },
            { GalTextureFormat.RGB5A1,     GalImageFormat.RGB5A1            | Unorm                              },
            { GalTextureFormat.RGB565,     GalImageFormat.RGB565            | Unorm                              },
            { GalTextureFormat.R11G11B10F, GalImageFormat.R11G11B10                               | Float        },
            { GalTextureFormat.D24S8,      GalImageFormat.D24S8             | Unorm        | Uint                },
            { GalTextureFormat.D32F,       GalImageFormat.D32                                     | Float        },
            { GalTextureFormat.D32FX24S8,  GalImageFormat.D32S8                                   | Float        },
            { GalTextureFormat.D16,        GalImageFormat.D16               | Unorm                              },

            //Compressed formats
            { GalTextureFormat.BptcSfloat,  GalImageFormat.BptcSfloat                  | Float        },
            { GalTextureFormat.BptcUfloat,  GalImageFormat.BptcUfloat                  | Float        },
            { GalTextureFormat.BptcUnorm,   GalImageFormat.BptcUnorm   | Unorm                 | Srgb },
            { GalTextureFormat.BC1,         GalImageFormat.BC1         | Unorm                 | Srgb },
            { GalTextureFormat.BC2,         GalImageFormat.BC2         | Unorm                 | Srgb },
            { GalTextureFormat.BC3,         GalImageFormat.BC3         | Unorm                 | Srgb },
            { GalTextureFormat.BC4,         GalImageFormat.BC4         | Unorm | Snorm                },
            { GalTextureFormat.BC5,         GalImageFormat.BC5         | Unorm | Snorm                },
            { GalTextureFormat.Astc2D4x4,   GalImageFormat.Astc2D4x4   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D5x5,   GalImageFormat.Astc2D5x5   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D6x6,   GalImageFormat.Astc2D6x6   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D8x8,   GalImageFormat.Astc2D8x8   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10x10, GalImageFormat.Astc2D10x10 | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D12x12, GalImageFormat.Astc2D12x12 | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D5x4,   GalImageFormat.Astc2D5x4   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D6x5,   GalImageFormat.Astc2D6x5   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D8x6,   GalImageFormat.Astc2D8x6   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10x8,  GalImageFormat.Astc2D10x8  | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D12x10, GalImageFormat.Astc2D12x10 | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D8x5,   GalImageFormat.Astc2D8x5   | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10x5,  GalImageFormat.Astc2D10x5  | Unorm                 | Srgb },
            { GalTextureFormat.Astc2D10x6,  GalImageFormat.Astc2D10x6  | Unorm                 | Srgb }
        };

        private static readonly Dictionary<GalImageFormat, ImageDescriptor> ImageTable =
                            new Dictionary<GalImageFormat, ImageDescriptor>()
        {
            { GalImageFormat.RGBA32,      new ImageDescriptor(16, 1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGBA16,      new ImageDescriptor(8,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RG32,        new ImageDescriptor(8,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGBX8,       new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGBA8,       new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BGRA8,       new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGB10A2,     new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R32,         new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGBA4,       new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BptcSfloat,  new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.BptcUfloat,  new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.BGR5A1,      new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGB5A1,      new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RGB565,      new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BGR565,      new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BptcUnorm,   new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.RG16,        new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.RG8,         new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R16,         new ImageDescriptor(2,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R8,          new ImageDescriptor(1,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.R11G11B10,   new ImageDescriptor(4,  1,  1,  1,  TargetBuffer.Color) },
            { GalImageFormat.BC1,         new ImageDescriptor(8,  4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.BC2,         new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.BC3,         new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.BC4,         new ImageDescriptor(8,  4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.BC5,         new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D4x4,   new ImageDescriptor(16, 4,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D5x5,   new ImageDescriptor(16, 5,  5,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D6x6,   new ImageDescriptor(16, 6,  6,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D8x8,   new ImageDescriptor(16, 8,  8,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10x10, new ImageDescriptor(16, 10, 10, 1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D12x12, new ImageDescriptor(16, 12, 12, 1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D5x4,   new ImageDescriptor(16, 5,  4,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D6x5,   new ImageDescriptor(16, 6,  5,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D8x6,   new ImageDescriptor(16, 8,  6,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10x8,  new ImageDescriptor(16, 10, 8,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D12x10, new ImageDescriptor(16, 12, 10, 1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D8x5,   new ImageDescriptor(16, 8,  5,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10x5,  new ImageDescriptor(16, 10, 5,  1,  TargetBuffer.Color) },
            { GalImageFormat.Astc2D10x6,  new ImageDescriptor(16, 10, 6,  1,  TargetBuffer.Color) },

            { GalImageFormat.D16,   new ImageDescriptor(2, 1, 1, 1, TargetBuffer.Depth)        },
            { GalImageFormat.D24,   new ImageDescriptor(4, 1, 1, 1, TargetBuffer.Depth)        },
            { GalImageFormat.D24S8, new ImageDescriptor(4, 1, 1, 1, TargetBuffer.DepthStencil) },
            { GalImageFormat.D32,   new ImageDescriptor(4, 1, 1, 1, TargetBuffer.Depth)        },
            { GalImageFormat.D32S8, new ImageDescriptor(8, 1, 1, 1, TargetBuffer.DepthStencil) }
        };

        public static GalImageFormat ConvertTexture(
            GalTextureFormat format,
            GalTextureType   rType,
            GalTextureType   gType,
            GalTextureType   bType,
            GalTextureType   aType,
            bool             convSrgb)
        {
            if (!TextureTable.TryGetValue(format, out GalImageFormat imageFormat))
            {
                throw new NotImplementedException($"Format 0x{((int)format):x} not implemented!");
            }

            if (!HasDepth(imageFormat) && (rType != gType || rType != bType || rType != aType))
            {
                throw new NotImplementedException($"Per component types are not implemented!");
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
                case GalSurfaceFormat.RGBA32Float:    return GalImageFormat.RGBA32    | Float;
                case GalSurfaceFormat.RGBA32Uint:     return GalImageFormat.RGBA32    | Uint;
                case GalSurfaceFormat.RGBA16Float:    return GalImageFormat.RGBA16    | Float;
                case GalSurfaceFormat.RGBA16Unorm:    return GalImageFormat.RGBA16    | Unorm;
                case GalSurfaceFormat.RG32Float:      return GalImageFormat.RG32      | Float;
                case GalSurfaceFormat.RG32Sint:       return GalImageFormat.RG32      | Sint;
                case GalSurfaceFormat.RG32Uint:       return GalImageFormat.RG32      | Uint;
                case GalSurfaceFormat.BGRA8Unorm:     return GalImageFormat.BGRA8     | Unorm;
                case GalSurfaceFormat.BGRA8Srgb:      return GalImageFormat.BGRA8     | Srgb;
                case GalSurfaceFormat.RGB10A2Unorm:   return GalImageFormat.RGB10A2   | Unorm;
                case GalSurfaceFormat.RGBA8Unorm:     return GalImageFormat.RGBA8     | Unorm;
                case GalSurfaceFormat.RGBA8Srgb:      return GalImageFormat.RGBA8     | Srgb;
                case GalSurfaceFormat.RGBA8Snorm:     return GalImageFormat.RGBA8     | Snorm;
                case GalSurfaceFormat.RG16Snorm:      return GalImageFormat.RG16      | Snorm;
                case GalSurfaceFormat.RG16Unorm:      return GalImageFormat.RG16      | Unorm;
                case GalSurfaceFormat.RG16Sint:       return GalImageFormat.RG16      | Sint;
                case GalSurfaceFormat.RG16Float:      return GalImageFormat.RG16      | Float;
                case GalSurfaceFormat.R11G11B10Float: return GalImageFormat.R11G11B10 | Float;
                case GalSurfaceFormat.R32Float:       return GalImageFormat.R32       | Float;
                case GalSurfaceFormat.R32Uint:        return GalImageFormat.R32       | Uint;
                case GalSurfaceFormat.RG8Unorm:       return GalImageFormat.RG8       | Unorm;
                case GalSurfaceFormat.RG8Snorm:       return GalImageFormat.RG8       | Snorm;
                case GalSurfaceFormat.R16Float:       return GalImageFormat.R16       | Float;
                case GalSurfaceFormat.R16Unorm:       return GalImageFormat.R16       | Unorm;
                case GalSurfaceFormat.R16Uint:        return GalImageFormat.R16       | Uint;
                case GalSurfaceFormat.R8Unorm:        return GalImageFormat.R8        | Unorm;
                case GalSurfaceFormat.R8Uint:         return GalImageFormat.R8        | Uint;
                case GalSurfaceFormat.B5G6R5Unorm:    return GalImageFormat.RGB565    | Unorm;
                case GalSurfaceFormat.BGR5A1Unorm:    return GalImageFormat.BGR5A1    | Unorm;
                case GalSurfaceFormat.RGBX8Unorm:     return GalImageFormat.RGBX8     | Unorm;
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
                case GalZetaFormat.D24X8Unorm:    return GalImageFormat.D24   | Unorm;
                case GalZetaFormat.D24S8Unorm:    return GalImageFormat.D24S8 | Unorm;
                case GalZetaFormat.D32S8X24Float: return GalImageFormat.D32S8 | Float;
            }

            throw new NotImplementedException(format.ToString());
        }

        public static byte[] ReadTexture(IMemory memory, GalImage image, long position)
        {
            MemoryManager cpuMemory;

            if (memory is NvGpuVmm vmm)
            {
                cpuMemory = vmm.Memory;
            }
            else
            {
                cpuMemory = (MemoryManager)memory;
            }

            ISwizzle swizzle = TextureHelper.GetSwizzle(image);

            ImageDescriptor desc = GetImageDescriptor(image.Format);

            (int width, int height, int depth) = GetImageSizeInBlocks(image);

            int bytesPerPixel = desc.BytesPerPixel;

            //Note: Each row of the texture needs to be aligned to 4 bytes.
            int pitch = (width * bytesPerPixel + 3) & ~3;


            int dataLayerSize = height * pitch * depth;
            byte[] data = new byte[dataLayerSize * image.LayerCount];

            int targetMipLevel = image.MaxMipmapLevel <= 1 ? 1 : image.MaxMipmapLevel - 1;
            int layerOffset = ImageUtils.GetLayerOffset(image, targetMipLevel);

            for (int layer = 0; layer < image.LayerCount; layer++)
            {
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int outOffs = (dataLayerSize * layer) + y * pitch + (z * width * height * bytesPerPixel);

                        for (int x = 0; x < width; x++)
                        {
                            long offset = (uint)swizzle.GetSwizzleOffset(x, y, z);

                            cpuMemory.ReadBytes(position + (layerOffset * layer) + offset, data, outOffs, bytesPerPixel);

                            outOffs += bytesPerPixel;
                        }
                    }
                }
            }

            return data;
        }

        public static void WriteTexture(NvGpuVmm vmm, GalImage image, long position, byte[] data)
        {
            ISwizzle swizzle = TextureHelper.GetSwizzle(image);

            ImageDescriptor desc = GetImageDescriptor(image.Format);

            (int width, int height, int depth) = ImageUtils.GetImageSizeInBlocks(image);

            int bytesPerPixel = desc.BytesPerPixel;

            int inOffs = 0;

            for (int z = 0; z < depth; z++)
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                long offset = (uint)swizzle.GetSwizzleOffset(x, y, z);

                vmm.Memory.WriteBytes(position + offset, data, inOffs, bytesPerPixel);

                inOffs += bytesPerPixel;
            }
        }

        // TODO: Support non 2D
        public static bool CopyTexture(
            NvGpuVmm vmm,
            GalImage srcImage,
            GalImage dstImage,
            long     srcAddress,
            long     dstAddress,
            int      srcX,
            int      srcY,
            int      dstX,
            int      dstY,
            int      width,
            int      height)
        {
            ISwizzle srcSwizzle = TextureHelper.GetSwizzle(srcImage);
            ISwizzle dstSwizzle = TextureHelper.GetSwizzle(dstImage);

            ImageDescriptor desc = GetImageDescriptor(srcImage.Format);

            if (GetImageDescriptor(dstImage.Format).BytesPerPixel != desc.BytesPerPixel)
            {
                return false;
            }

            int bytesPerPixel = desc.BytesPerPixel;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                long srcOffset = (uint)srcSwizzle.GetSwizzleOffset(srcX + x, srcY + y, 0);
                long dstOffset = (uint)dstSwizzle.GetSwizzleOffset(dstX + x, dstY + y, 0);

                byte[] texel = vmm.ReadBytes(srcAddress + srcOffset, bytesPerPixel);

                vmm.WriteBytes(dstAddress + dstOffset, texel);
            }

            return true;
        }

        public static int GetSize(GalImage image)
        {
            ImageDescriptor desc = GetImageDescriptor(image.Format);

            int componentCount = GetCoordsCountTextureTarget(image.TextureTarget);

            if (IsArray(image.TextureTarget))
                componentCount--;

            int width  = DivRoundUp(image.Width,  desc.BlockWidth);
            int height = DivRoundUp(image.Height, desc.BlockHeight);
            int depth  = DivRoundUp(image.Depth,  desc.BlockDepth);

            switch (componentCount)
            {
                case 1:
                    return desc.BytesPerPixel * width * image.LayerCount;
                case 2:
                    return desc.BytesPerPixel * width * height * image.LayerCount;
                case 3:
                    return desc.BytesPerPixel * width * height * depth * image.LayerCount;
                default:
                    throw new InvalidOperationException($"Invalid component count: {componentCount}");
            }
        }

        public static int GetGpuSize(GalImage image, bool forcePitch = false)
        {
            return TextureHelper.GetSwizzle(image).GetImageSize(image.MaxMipmapLevel) * image.LayerCount;
        }

        public static int GetLayerOffset(GalImage image, int mipLevel)
        {
            if (mipLevel <= 0)
            {
                mipLevel = 1;
            }

            return TextureHelper.GetSwizzle(image).GetMipOffset(mipLevel);
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

        public static int GetBlockDepth(GalImageFormat format)
        {
            return GetImageDescriptor(format).BlockDepth;
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

        public static (int Width, int Height, int Depth) GetImageSizeInBlocks(GalImage image)
        {
            ImageDescriptor desc = GetImageDescriptor(image.Format);

            return (DivRoundUp(image.Width,  desc.BlockWidth),
                    DivRoundUp(image.Height, desc.BlockHeight),
                    DivRoundUp(image.Depth, desc.BlockDepth));
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

            if (ImageTable.TryGetValue(pixelFormat, out ImageDescriptor descriptor))
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

        public static TextureTarget GetTextureTarget(GalTextureTarget galTextureTarget)
        {
            switch (galTextureTarget)
            {
                case GalTextureTarget.OneD:
                    return TextureTarget.Texture1D;
                case GalTextureTarget.TwoD:
                case GalTextureTarget.TwoDNoMipMap:
                    return TextureTarget.Texture2D;
                case GalTextureTarget.ThreeD:
                    return TextureTarget.Texture3D;
                case GalTextureTarget.OneDArray:
                    return TextureTarget.Texture1DArray;
                case GalTextureTarget.OneDBuffer:
                    return TextureTarget.TextureBuffer;
                case GalTextureTarget.TwoDArray:
                    return TextureTarget.Texture2DArray;
                case GalTextureTarget.CubeMap:
                    return TextureTarget.TextureCubeMap;
                case GalTextureTarget.CubeArray:
                    return TextureTarget.TextureCubeMapArray;
                default:
                    throw new NotSupportedException($"Texture target {galTextureTarget} currently not supported!");
            }
        }

        public static bool IsArray(GalTextureTarget textureTarget)
        {
            switch (textureTarget)
            {
                case GalTextureTarget.OneDArray:
                case GalTextureTarget.TwoDArray:
                case GalTextureTarget.CubeArray:
                    return true;
                default:
                    return false;
            }
        }

        public static int GetCoordsCountTextureTarget(GalTextureTarget textureTarget)
        {
            switch (textureTarget)
            {
                case GalTextureTarget.OneD:
                    return 1;
                case GalTextureTarget.OneDArray:
                case GalTextureTarget.OneDBuffer:
                case GalTextureTarget.TwoD:
                case GalTextureTarget.TwoDNoMipMap:
                    return 2;
                case GalTextureTarget.ThreeD:
                case GalTextureTarget.TwoDArray:
                case GalTextureTarget.CubeMap:
                    return 3;
                case GalTextureTarget.CubeArray:
                    return 4;
                default:
                    throw new NotImplementedException($"TextureTarget.{textureTarget} not implemented yet.");
            }
        }
    }
}
