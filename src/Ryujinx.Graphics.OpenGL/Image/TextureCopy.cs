using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Silk.NET.OpenGL.Legacy;
using System;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureCopy : IDisposable
    {
        private readonly OpenGLRenderer _gd;

        private uint _srcFramebuffer;
        private uint _dstFramebuffer;

        private uint _copyPboHandle;
        private int _copyPboSize;

        public IntermediatePool IntermediatePool { get; }

        public TextureCopy(OpenGLRenderer gd)
        {
            _gd = gd;
            IntermediatePool = new IntermediatePool(gd);
        }

        public void Copy(
            TextureView src,
            TextureView dst,
            Extents2D srcRegion,
            Extents2D dstRegion,
            bool linearFilter,
            int srcLayer = 0,
            int dstLayer = 0,
            int srcLevel = 0,
            int dstLevel = 0)
        {
            int levels = Math.Min(src.Info.Levels - srcLevel, dst.Info.Levels - dstLevel);
            int layers = Math.Min(src.Info.GetLayers() - srcLayer, dst.Info.GetLayers() - dstLayer);

            Copy(src, dst, srcRegion, dstRegion, linearFilter, srcLayer, dstLayer, srcLevel, dstLevel, layers, levels);
        }

        public void Copy(
            TextureView src,
            TextureView dst,
            Extents2D srcRegion,
            Extents2D dstRegion,
            bool linearFilter,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel,
            int layers,
            int levels)
        {
            TextureView srcConverted = src.Format.IsBgr() != dst.Format.IsBgr() ? BgraSwap(src) : src;

            (uint oldDrawFramebufferHandle, uint oldReadFramebufferHandle) = ((Pipeline)_gd.Pipeline).GetBoundFramebuffers();

            _gd.Api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, GetSrcFramebufferLazy());
            _gd.Api.BindFramebuffer(FramebufferTarget.DrawFramebuffer, GetDstFramebufferLazy());

            if (srcLevel != 0)
            {
                srcRegion = srcRegion.Reduce(srcLevel);
            }

            if (dstLevel != 0)
            {
                dstRegion = dstRegion.Reduce(dstLevel);
            }

            for (int level = 0; level < levels; level++)
            {
                for (int layer = 0; layer < layers; layer++)
                {
                    if ((srcLayer | dstLayer) != 0 || layers > 1)
                    {
                        Attach(_gd.Api, FramebufferTarget.ReadFramebuffer, src.Format, srcConverted.Handle, srcLevel + level, srcLayer + layer);
                        Attach(_gd.Api, FramebufferTarget.DrawFramebuffer, dst.Format, dst.Handle, dstLevel + level, dstLayer + layer);
                    }
                    else
                    {
                        Attach(_gd.Api, FramebufferTarget.ReadFramebuffer, src.Format, srcConverted.Handle, srcLevel + level);
                        Attach(_gd.Api, FramebufferTarget.DrawFramebuffer, dst.Format, dst.Handle, dstLevel + level);
                    }

                    ClearBufferMask mask = GetMask(src.Format);

                    if ((mask & (ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit)) != 0 || src.Format.IsInteger())
                    {
                        linearFilter = false;
                    }

                    BlitFramebufferFilter filter = linearFilter
                        ? BlitFramebufferFilter.Linear
                        : BlitFramebufferFilter.Nearest;

                    _gd.Api.ReadBuffer(ReadBufferMode.ColorAttachment0);
                    _gd.Api.DrawBuffer(DrawBufferMode.ColorAttachment0);

                    _gd.Api.Disable(EnableCap.RasterizerDiscard);
                    _gd.Api.Disable(EnableCap.ScissorTest, 0);

                    _gd.Api.BlitFramebuffer(
                        srcRegion.X1,
                        srcRegion.Y1,
                        srcRegion.X2,
                        srcRegion.Y2,
                        dstRegion.X1,
                        dstRegion.Y1,
                        dstRegion.X2,
                        dstRegion.Y2,
                        mask,
                        filter);
                }

                if (level < levels - 1)
                {
                    srcRegion = srcRegion.Reduce(1);
                    dstRegion = dstRegion.Reduce(1);
                }
            }

            Attach(_gd.Api, FramebufferTarget.ReadFramebuffer, src.Format, 0);
            Attach(_gd.Api, FramebufferTarget.DrawFramebuffer, dst.Format, 0);

            _gd.Api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, oldReadFramebufferHandle);
            _gd.Api.BindFramebuffer(FramebufferTarget.DrawFramebuffer, oldDrawFramebufferHandle);

            ((Pipeline)_gd.Pipeline).RestoreScissor0Enable();
            ((Pipeline)_gd.Pipeline).RestoreRasterizerDiscard();

            if (srcConverted != src)
            {
                srcConverted.Dispose();
            }
        }

        public void CopyUnscaled(
            ITextureInfo src,
            ITextureInfo dst,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel)
        {
            TextureCreateInfo srcInfo = src.Info;
            TextureCreateInfo dstInfo = dst.Info;

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

            CopyUnscaled(src, dst, srcLayer, dstLayer, srcLevel, dstLevel, depth, levels);
        }

        public void CopyUnscaled(
            ITextureInfo src,
            ITextureInfo dst,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel,
            int depth,
            int levels)
        {
            TextureCreateInfo srcInfo = src.Info;
            TextureCreateInfo dstInfo = dst.Info;

            uint srcHandle = src.Handle;
            uint dstHandle = dst.Handle;

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

                if ((width % blockWidth != 0 || height % blockHeight != 0) && src is TextureView srcView && dst is TextureView dstView)
                {
                    PboCopy(srcView, dstView, srcLayer, dstLayer, srcLevel + level, dstLevel + level, width, height);
                }
                else
                {
                    int copyWidth = sizeInBlocks ? BitUtils.DivRoundUp(width, blockWidth) : width;
                    int copyHeight = sizeInBlocks ? BitUtils.DivRoundUp(height, blockHeight) : height;

                    if (_gd.Capabilities.GpuVendor == GpuVendor.IntelWindows)
                    {
                        _gd.Api.CopyImageSubData(
                            src.Storage.Handle,
                            src.Storage.Info.Target.ConvertToImageTarget(),
                            (int)src.FirstLevel + srcLevel + level,
                            0,
                            0,
                            (int)src.FirstLayer + srcLayer,
                            dst.Storage.Handle,
                            dst.Storage.Info.Target.ConvertToImageTarget(),
                            (int)dst.FirstLevel + dstLevel + level,
                            0,
                            0,
                            (int)dst.FirstLayer + dstLayer,
                            (uint)copyWidth,
                            (uint)copyHeight,
                            (uint)depth);
                    }
                    else
                    {
                        _gd.Api.CopyImageSubData(
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
                            (uint)copyWidth,
                            (uint)copyHeight,
                            (uint)depth);
                    }
                }

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);

                if (srcInfo.Target == Target.Texture3D)
                {
                    depth = Math.Max(1, depth >> 1);
                }
            }
        }

        private static FramebufferAttachment AttachmentForFormat(Format format)
        {
            if (format == Format.D24UnormS8Uint || format == Format.D32FloatS8Uint)
            {
                return FramebufferAttachment.DepthStencilAttachment;
            }
            else if (FormatTable.IsDepthOnly(format))
            {
                return FramebufferAttachment.DepthAttachment;
            }
            else if (format == Format.S8Uint)
            {
                return FramebufferAttachment.StencilAttachment;
            }
            else
            {
                return FramebufferAttachment.ColorAttachment0;
            }
        }

        private static void Attach(GL api, FramebufferTarget target, Format format, uint handle, int level = 0)
        {
            FramebufferAttachment attachment = AttachmentForFormat(format);

            api.FramebufferTexture(target, attachment, handle, level);
        }

        private static void Attach(GL api, FramebufferTarget target, Format format, uint handle, int level, int layer)
        {
            FramebufferAttachment attachment = AttachmentForFormat(format);

            api.FramebufferTextureLayer(target, attachment, handle, level, layer);
        }

        private static ClearBufferMask GetMask(Format format)
        {
            if (FormatTable.IsPackedDepthStencil(format))
            {
                return ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;
            }
            else if (FormatTable.IsDepthOnly(format))
            {
                return ClearBufferMask.DepthBufferBit;
            }
            else if (format == Format.S8Uint)
            {
                return ClearBufferMask.StencilBufferBit;
            }
            else
            {
                return ClearBufferMask.ColorBufferBit;
            }
        }

        public TextureView BgraSwap(TextureView from)
        {
            TextureView to = (TextureView)_gd.CreateTexture(from.Info);

            EnsurePbo(from);

            _gd.Api.BindBuffer(BufferTargetARB.PixelPackBuffer, _copyPboHandle);

            from.WriteToPbo(0, forceBgra: true);

            _gd.Api.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
            _gd.Api.BindBuffer(BufferTargetARB.PixelUnpackBuffer, _copyPboHandle);

            to.ReadFromPbo(0, _copyPboSize);

            _gd.Api.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);

            return to;
        }

        public void PboCopy(TextureView from, TextureView to, int srcLayer, int dstLayer, int srcLevel, int dstLevel, int width, int height)
        {
            int dstWidth = width;
            int dstHeight = height;

            // The size of the source texture.
            int unpackWidth = from.Width;
            int unpackHeight = from.Height;

            if (from.Info.IsCompressed != to.Info.IsCompressed)
            {
                if (from.Info.IsCompressed)
                {
                    // Dest size is in pixels, but should be in blocks
                    dstWidth = BitUtils.DivRoundUp(width, from.Info.BlockWidth);
                    dstHeight = BitUtils.DivRoundUp(height, from.Info.BlockHeight);

                    // When copying from a compressed texture, the source size must be taken in blocks for unpacking to the uncompressed block texture.
                    unpackWidth = BitUtils.DivRoundUp(from.Info.Width, from.Info.BlockWidth);
                    unpackHeight = BitUtils.DivRoundUp(from.Info.Height, from.Info.BlockHeight);
                }
                else
                {
                    // When copying to a compressed texture, the source size must be scaled by the block width for unpacking on the compressed target.
                    unpackWidth = from.Info.Width * to.Info.BlockWidth;
                    unpackHeight = from.Info.Height * to.Info.BlockHeight;
                }
            }

            EnsurePbo(from);

            _gd.Api.BindBuffer(BufferTargetARB.PixelPackBuffer, _copyPboHandle);

            // The source texture is written out in full, then the destination is taken as a slice from the data using unpack params.
            // The offset points to the base at which the requested layer is at.

            int offset = from.WriteToPbo2D(0, srcLayer, srcLevel);

            // If the destination size is not an exact match for the source unpack parameters, we need to set them to slice the data correctly.

            bool slice = (unpackWidth != dstWidth || unpackHeight != dstHeight);

            if (slice)
            {
                // Set unpack parameters to take a slice of width/height:
                _gd.Api.PixelStore(PixelStoreParameter.UnpackRowLength, unpackWidth);
                _gd.Api.PixelStore(PixelStoreParameter.UnpackImageHeight, unpackHeight);

                if (to.Info.IsCompressed)
                {
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockWidth, to.Info.BlockWidth);
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockHeight, to.Info.BlockHeight);
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockDepth, 1);
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockSize, to.Info.BytesPerPixel);
                }
            }

            _gd.Api.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
            _gd.Api.BindBuffer(BufferTargetARB.PixelUnpackBuffer, _copyPboHandle);

            to.ReadFromPbo2D(offset, dstLayer, dstLevel, dstWidth, dstHeight);

            if (slice)
            {
                // Reset unpack parameters
                _gd.Api.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
                _gd.Api.PixelStore(PixelStoreParameter.UnpackImageHeight, 0);

                if (to.Info.IsCompressed)
                {
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockWidth, 0);
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockHeight, 0);
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockDepth, 0);
                    _gd.Api.PixelStore(GLEnum.UnpackCompressedBlockSize, 0);
                }
            }

            _gd.Api.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
        }

        private void EnsurePbo(TextureView view)
        {
            int requiredSize = 0;

            for (int level = 0; level < view.Info.Levels; level++)
            {
                requiredSize += view.Info.GetMipSize(level);
            }

            if (_copyPboSize < requiredSize && _copyPboHandle != 0)
            {
                _gd.Api.DeleteBuffer(_copyPboHandle);

                _copyPboHandle = 0;
            }

            if (_copyPboHandle == 0)
            {
                _copyPboHandle = _gd.Api.GenBuffer();
                _copyPboSize = requiredSize;

                _gd.Api.BindBuffer(BufferTargetARB.PixelPackBuffer, _copyPboHandle);
                _gd.Api.BufferData(BufferTargetARB.PixelPackBuffer, (uint)requiredSize, in IntPtr.Zero, BufferUsageARB.DynamicCopy);
            }
        }

        private uint GetSrcFramebufferLazy()
        {
            if (_srcFramebuffer == 0)
            {
                _srcFramebuffer = _gd.Api.GenFramebuffer();
            }

            return _srcFramebuffer;
        }

        private uint GetDstFramebufferLazy()
        {
            if (_dstFramebuffer == 0)
            {
                _dstFramebuffer = _gd.Api.GenFramebuffer();
            }

            return _dstFramebuffer;
        }

        public void Dispose()
        {
            if (_srcFramebuffer != 0)
            {
                _gd.Api.DeleteFramebuffer(_srcFramebuffer);

                _srcFramebuffer = 0;
            }

            if (_dstFramebuffer != 0)
            {
                _gd.Api.DeleteFramebuffer(_dstFramebuffer);

                _dstFramebuffer = 0;
            }

            if (_copyPboHandle != 0)
            {
                _gd.Api.DeleteBuffer(_copyPboHandle);

                _copyPboHandle = 0;
            }

            IntermediatePool.Dispose();
        }
    }
}
