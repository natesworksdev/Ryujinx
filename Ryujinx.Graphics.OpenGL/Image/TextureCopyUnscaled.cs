using OpenTK.Graphics.OpenGL;
using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.OpenGL.Image
{
    static class TextureCopyUnscaled
    {
        public static void Copy(
            TextureCopy copy,
            ITextureInfo src,
            ITextureInfo dst,
            int srcHandle,
            int dstHandle,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel)
        {
            TextureCreateInfo srcInfo = src.Info;
            TextureCreateInfo dstInfo = dst.Info;

            int srcWidth  = srcInfo.Width;
            int srcHeight = srcInfo.Height;
            int srcDepth  = srcInfo.GetDepthOrLayers();
            int srcLevels = srcInfo.Levels;

            int dstWidth  = dstInfo.Width;
            int dstHeight = dstInfo.Height;
            int dstDepth  = dstInfo.GetDepthOrLayers();
            int dstLevels = dstInfo.Levels;

            dstWidth = Math.Max(1, dstWidth >> dstLevel);
            dstHeight = Math.Max(1, dstHeight >> dstLevel);

            if (dstInfo.Target == Target.Texture3D)
            {
                dstDepth = Math.Max(1, dstDepth >> dstLevel);
            }

            int blockWidth = 1;
            int blockHeight = 1;

            // When copying from a compressed to a non-compressed format,
            // the non-compressed texture will have the size of the texture
            // in blocks (not in texels), so we must adjust that size to
            // match the size in texels of the compressed texture.
            if (!srcInfo.IsCompressed && dstInfo.IsCompressed)
            {
                dstWidth  = BitUtils.DivRoundUp(dstWidth,  dstInfo.BlockWidth);
                dstHeight = BitUtils.DivRoundUp(dstHeight, dstInfo.BlockHeight);
                blockWidth = dstInfo.BlockWidth;
                blockHeight = dstInfo.BlockHeight;
            }
            else if (srcInfo.IsCompressed && !dstInfo.IsCompressed)
            {
                dstWidth  *= srcInfo.BlockWidth;
                dstHeight *= srcInfo.BlockHeight;
                blockWidth = srcInfo.BlockWidth;
                blockHeight = srcInfo.BlockHeight;
            }

            int width  = Math.Min(srcWidth,  dstWidth);
            int height = Math.Min(srcHeight, dstHeight);
            int depth  = Math.Min(srcDepth,  dstDepth);
            int levels = Math.Min(srcLevels, dstLevels);

            for (int level = 0; level < levels; level++)
            {
                // Stop copy if we are already out of the levels range.
                if (level >= srcInfo.Levels || dstLevel + level >= dstInfo.Levels)
                {
                    break;
                }

                if (width % blockWidth != 0 && height % blockHeight != 0 && src is TextureView && dst is TextureView)
                {
                    copy.PboCopy((TextureView)src, (TextureView)dst, srcLayer, dstLayer, srcLevel + level, dstLevel + level);
                }
                else
                {
                    GL.CopyImageSubData(
                        srcHandle,
                        srcInfo.Target.ConvertToImageTarget(),
                        srcLevel + level,
                        0,
                        0,
                        srcLayer,
                        dstHandle,
                        dstInfo.Target.ConvertToImageTarget(),
                        dstLevel + level,
                        0,
                        0,
                        dstLayer,
                        width,
                        height,
                        depth);
                }

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);

                if (srcInfo.Target == Target.Texture3D)
                {
                    depth = Math.Max(1, depth >> 1);
                }
            }
        }
    }
}
