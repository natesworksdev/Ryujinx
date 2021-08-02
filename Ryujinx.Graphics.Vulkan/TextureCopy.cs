using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    static class TextureCopy
    {
        public static void Blit(
            Vk api,
            CommandBuffer commandBuffer,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            Extents2D srcRegion,
            Extents2D dstRegion,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel,
            bool linearFilter,
            ImageAspectFlags srcAspectFlags = 0,
            ImageAspectFlags dstAspectFlags = 0)
        {
            static (Offset3D, Offset3D) ExtentsToOffset3D(Extents2D extents, int width, int height)
            {
                static int Clamp(int value, int max)
                {
                    return Math.Clamp(value, 0, max);
                }

                var xy1 = new Offset3D(Clamp(extents.X1, width), Clamp(extents.Y1, height), 0);
                var xy2 = new Offset3D(Clamp(extents.X2, width), Clamp(extents.Y2, height), 1);

                return (xy1, xy2);
            }

            if (srcAspectFlags == 0)
            {
                srcAspectFlags = srcInfo.Format.ConvertAspectFlags();
            }

            if (dstAspectFlags == 0)
            {
                dstAspectFlags = dstInfo.Format.ConvertAspectFlags();
            }

            var srcSl = new ImageSubresourceLayers(srcAspectFlags, (uint)srcLevel, (uint)srcLayer, 1);
            var dstSl = new ImageSubresourceLayers(dstAspectFlags, (uint)dstLevel, (uint)dstLayer, 1);

            var srcOffsets = new ImageBlit.SrcOffsetsBuffer();
            var dstOffsets = new ImageBlit.DstOffsetsBuffer();

            (srcOffsets.Element0, srcOffsets.Element1) = ExtentsToOffset3D(srcRegion, srcInfo.Width, srcInfo.Height);
            (dstOffsets.Element0, dstOffsets.Element1) = ExtentsToOffset3D(dstRegion, dstInfo.Width, dstInfo.Height);

            var region = new ImageBlit()
            {
                SrcSubresource = srcSl,
                SrcOffsets = srcOffsets,
                DstSubresource = dstSl,
                DstOffsets = dstOffsets
            };

            var filter = linearFilter && !dstInfo.Format.IsDepthOrStencil() ? Filter.Linear : Filter.Nearest;

            api.CmdBlitImage(commandBuffer, srcImage, ImageLayout.General, dstImage, ImageLayout.General, 1, region, filter);
        }

        public static void Copy(
            Vk api,
            CommandBuffer commandBuffer,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            int srcViewLayer,
            int dstViewLayer,
            int srcViewLevel,
            int dstViewLevel,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel)
        {
            int srcDepth = srcInfo.GetDepthOrLayers();
            int srcLevels = srcInfo.Levels;

            int dstDepth = dstInfo.GetDepthOrLayers();
            int dstLevels = dstInfo.Levels;

            if (dstInfo.Target == Target.Texture3D)
            {
                dstDepth = Math.Max(1, dstDepth >> dstLevel);
            }

            int depth = Math.Min(srcDepth, dstDepth);
            int levels = Math.Min(srcLevels, dstLevels);

            Copy(
                api,
                commandBuffer,
                srcImage,
                dstImage,
                srcInfo,
                dstInfo,
                srcViewLayer,
                dstViewLayer,
                srcViewLevel,
                dstViewLevel,
                srcLayer,
                dstLayer,
                srcLevel,
                dstLevel,
                depth,
                levels);
        }

        private static int ClampLevels(TextureCreateInfo info, int levels)
        {
            int width = info.Width;
            int height = info.Height;
            int depth = info.Target == Target.Texture3D ? info.Depth : 1;

            int maxLevels = 1 + BitOperations.Log2((uint)Math.Max(Math.Max(width, height), depth));

            if (levels > maxLevels)
            {
                levels = maxLevels;
            }

            return levels;
        }

        public static void Copy(
            Vk api,
            CommandBuffer commandBuffer,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            int srcViewLayer,
            int dstViewLayer,
            int srcViewLevel,
            int dstViewLevel,
            int srcDepthOrLayer,
            int dstDepthOrLayer,
            int srcLevel,
            int dstLevel,
            int depthOrLayers,
            int levels)
        {
            int srcZ;
            int srcLayer;
            int srcDepth;
            int srcLayers;

            if (srcInfo.Target == Target.Texture3D)
            {
                srcZ = srcDepthOrLayer;
                srcLayer = 0;
                srcDepth = depthOrLayers;
                srcLayers = 1;
            }
            else
            {
                srcZ = 0;
                srcLayer = srcDepthOrLayer;
                srcDepth = 1;
                srcLayers = depthOrLayers;
            }

            int dstZ;
            int dstLayer;
            int dstDepth;
            int dstLayers;

            if (dstInfo.Target == Target.Texture3D)
            {
                dstZ = dstDepthOrLayer;
                dstLayer = 0;
                dstDepth = depthOrLayers;
                dstLayers = 1;
            }
            else
            {
                dstZ = 0;
                dstLayer = dstDepthOrLayer;
                dstDepth = 1;
                dstLayers = depthOrLayers;
            }

            int srcWidth = srcInfo.Width;
            int srcHeight = srcInfo.Height;

            int dstWidth = dstInfo.Width;
            int dstHeight = dstInfo.Height;

            srcWidth = Math.Max(1, srcWidth >> srcLevel);
            srcHeight = Math.Max(1, srcHeight >> srcLevel);

            dstWidth = Math.Max(1, dstWidth >> dstLevel);
            dstHeight = Math.Max(1, dstHeight >> dstLevel);

            int blockWidth = 1;
            int blockHeight = 1;
            bool sizeInBlocks = false;

            // When copying from a compressed to a non-compressed format,
            // the non-compressed texture will have the size of the texture
            // in blocks (not in texels), so we must adjust that size to
            // match the size in texels of the compressed texture.
            if (!srcInfo.IsCompressed && dstInfo.IsCompressed)
            {
                srcWidth *= dstInfo.BlockWidth;
                srcHeight *= dstInfo.BlockHeight;
                blockWidth = dstInfo.BlockWidth;
                blockHeight = dstInfo.BlockHeight;

                sizeInBlocks = true;
            }
            else if (srcInfo.IsCompressed && !dstInfo.IsCompressed)
            {
                dstWidth *= srcInfo.BlockWidth;
                dstHeight *= srcInfo.BlockHeight;
                blockWidth = srcInfo.BlockWidth;
                blockHeight = srcInfo.BlockHeight;
            }

            int width = Math.Min(srcWidth, dstWidth);
            int height = Math.Min(srcHeight, dstHeight);

            for (int level = 0; level < levels; level++)
            {
                // Stop copy if we are already out of the levels range.
                if (level >= srcInfo.Levels || dstLevel + level >= dstInfo.Levels)
                {
                    break;
                }

                var srcSl = new ImageSubresourceLayers(
                    srcInfo.Format.ConvertAspectFlags(),
                    (uint)(srcViewLevel + srcLevel + level),
                    (uint)(srcViewLayer + srcLayer),
                    (uint)srcLayers);

                var dstSl = new ImageSubresourceLayers(
                    dstInfo.Format.ConvertAspectFlags(),
                    (uint)(dstViewLevel + dstLevel + level),
                    (uint)(dstViewLayer + dstLayer),
                    (uint)dstLayers);

                int copyWidth = sizeInBlocks ? BitUtils.DivRoundUp(width, blockWidth) : width;
                int copyHeight = sizeInBlocks ? BitUtils.DivRoundUp(height, blockHeight) : height;

                var extent = new Extent3D((uint)copyWidth, (uint)copyHeight, (uint)srcDepth);

                var region = new ImageCopy(srcSl, new Offset3D(0, 0, srcZ), dstSl, new Offset3D(0, 0, dstZ), extent);

                api.CmdCopyImage(commandBuffer, srcImage, ImageLayout.General, dstImage, ImageLayout.General, 1, region);

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);

                if (srcInfo.Target == Target.Texture3D)
                {
                    srcDepth = Math.Max(1, srcDepth >> 1);
                }
            }
        }
    }
}
