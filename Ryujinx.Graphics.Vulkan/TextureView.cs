using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace Ryujinx.Graphics.Vulkan
{
    class TextureView : ITexture
    {
        private readonly VulkanGraphicsDevice _gd;

        private readonly Device _device;

        private readonly Auto<DisposableImageView> _imageView;
        private readonly Auto<DisposableImageView> _imageViewIdentity;
        private readonly Auto<DisposableImageView> _imageView2dArray;

        public TextureCreateInfo Info { get; }

        public TextureStorage Storage { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;
        public int Layers => Info.GetDepthOrLayers();
        public int FirstLayer { get; }
        public int FirstLevel { get; }
        public float ScaleFactor => Storage.ScaleFactor;
        public Silk.NET.Vulkan.Format VkFormat { get; }
        public bool Valid { get; private set; }

        public TextureView(
            VulkanGraphicsDevice gd,
            Device device,
            TextureCreateInfo info,
            TextureStorage storage,
            int firstLayer,
            int firstLevel)
        {
            _gd = gd;
            _device = device;
            Info = info;
            Storage = storage;
            FirstLayer = firstLayer;
            FirstLevel = firstLevel;

            var format = FormatTable.GetFormat(info.Format);
            var levels = (uint)info.Levels;
            var layers = (uint)info.GetLayers();

            VkFormat = format;

            var type = info.Target.ConvertView();

            var swizzleR = info.SwizzleR.Convert();
            var swizzleG = info.SwizzleG.Convert();
            var swizzleB = info.SwizzleB.Convert();
            var swizzleA = info.SwizzleA.Convert();

            if (info.Format == GAL.Format.R5G5B5A1Unorm ||
                info.Format == GAL.Format.R5G5B5X1Unorm ||
                info.Format == GAL.Format.R5G6B5Unorm)
            {
                var temp = swizzleR;

                swizzleR = swizzleB;
                swizzleB = temp;
            }
            else if (info.Format == GAL.Format.R4G4B4A4Unorm)
            {
                var tempR = swizzleR;
                var tempG = swizzleG;

                swizzleR = swizzleA;
                swizzleG = swizzleB;
                swizzleB = tempG;
                swizzleA = tempR;
            }

            var componentMapping = new ComponentMapping(swizzleR, swizzleG, swizzleB, swizzleA);

            var aspectFlags = info.Format.ConvertAspectFlags(info.DepthStencilMode);

            var subresourceRange = new ImageSubresourceRange(aspectFlags, (uint)firstLevel, levels, (uint)firstLayer, layers);

            unsafe Auto<DisposableImageView> CreateImageView(ComponentMapping cm, ImageSubresourceRange sr, ImageViewType viewType)
            {
                var imageCreateInfo = new ImageViewCreateInfo()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = storage.GetImageForViewCreation(),
                    ViewType = viewType,
                    Format = format,
                    Components = cm,
                    SubresourceRange = sr
                };

                gd.Api.CreateImageView(device, imageCreateInfo, null, out var imageView).ThrowOnError();
                return new Auto<DisposableImageView>(new DisposableImageView(gd.Api, device, imageView), null, storage.GetImage());
            }

            _imageView = CreateImageView(componentMapping, subresourceRange, type);

            // Framebuffer attachments and storage images requires a identity component mapping.
            var identityComponentMapping = new ComponentMapping(
                ComponentSwizzle.R,
                ComponentSwizzle.G,
                ComponentSwizzle.B,
                ComponentSwizzle.A);

            _imageViewIdentity = CreateImageView(identityComponentMapping, subresourceRange, type);

            // Framebuffer attachments also requires 3D textures to be bound as 2D array.
            if (info.Target == Target.Texture3D)
            {
                subresourceRange = new ImageSubresourceRange(aspectFlags, (uint)firstLevel, levels, (uint)firstLayer, (uint)info.Depth);

                _imageView2dArray = CreateImageView(identityComponentMapping, subresourceRange, ImageViewType.ImageViewType2DArray);
            }

            Valid = true;
        }

        public Auto<DisposableImage> GetImage()
        {
            return Storage.GetImage();
        }

        public Auto<DisposableImageView> GetImageView()
        {
            return _imageView;
        }

        public Auto<DisposableImageView> GetIdentityImageView()
        {
            return _imageViewIdentity;
        }

        public Auto<DisposableImageView> GetImageViewForAttachment()
        {
            return _imageView2dArray ?? _imageViewIdentity;
        }

        public void CopyTo(ITexture destination, int firstLayer, int firstLevel)
        {
            _gd.FlushAllCommands();

            var src = this;
            var dst = (TextureView)destination;

            if (!Valid || !dst.Valid)
            {
                return;
            }

            using var cbs = _gd.CommandBufferPool.Rent();

            var srcImage = src.GetImage().Get(cbs).Value;
            var dstImage = dst.GetImage().Get(cbs).Value;

            TextureCopy.Copy(
                _gd.Api,
                cbs.CommandBuffer,
                srcImage,
                dstImage,
                src.Info,
                dst.Info,
                src.FirstLayer,
                dst.FirstLayer,
                src.FirstLevel,
                dst.FirstLevel,
                0,
                firstLayer,
                0,
                firstLevel);
        }

        public void CopyTo(ITexture destination, int srcLayer, int dstLayer, int srcLevel, int dstLevel)
        {
            _gd.FlushAllCommands();

            var src = this;
            var dst = (TextureView)destination;

            if (!Valid || !dst.Valid)
            {
                return;
            }

            using var cbs = _gd.CommandBufferPool.Rent();

            var srcImage = src.GetImage().Get(cbs).Value;
            var dstImage = dst.GetImage().Get(cbs).Value;

            TextureCopy.Copy(
                _gd.Api,
                cbs.CommandBuffer,
                srcImage,
                dstImage,
                src.Info,
                dst.Info,
                src.FirstLayer,
                dst.FirstLayer,
                src.FirstLevel,
                dst.FirstLevel,
                srcLayer,
                dstLayer,
                srcLevel,
                dstLevel,
                1,
                1);
        }

        public void CopyTo(ITexture destination, Extents2D srcRegion, Extents2D dstRegion, bool linearFilter)
        {
            _gd.FlushAllCommands();

            var src = this;
            var dst = (TextureView)destination;

            using var cbs = _gd.CommandBufferPool.Rent();

            _gd.Blit.Blit(
                _gd,
                cbs,
                src,
                dst.GetIdentityImageView(),
                dst.Width,
                dst.Height,
                dst.VkFormat,
                srcRegion.X1,
                srcRegion.Y1,
                srcRegion.X2,
                srcRegion.Y2,
                dstRegion.X1,
                dstRegion.Y1,
                dstRegion.X2,
                dstRegion.Y2,
                linearFilter);
        }

        public ITexture CreateView(TextureCreateInfo info, int firstLayer, int firstLevel)
        {
            return new TextureView(_gd, _device, info, Storage, FirstLayer + firstLayer, FirstLevel + firstLevel);
        }

        public ReadOnlySpan<byte> GetData()
        {
            int size = 0;

            for (int level = 0; level < Info.Levels; level++)
            {
                size += Info.GetMipSize(level);
            }

            byte[] data = new byte[size];

            if (_gd.CommandBufferPool.OwnedByCurrentThread)
            {
                GetData(_gd.CommandBufferPool, data);
            }
            else if (_gd.BackgroundQueue.Handle != 0)
            {
                lock (_gd.BackgroundQueueLock)
                {
                    using var cbp = new CommandBufferPool(
                        _gd.Api,
                        _device,
                        _gd.BackgroundQueue,
                        _gd.QueueFamilyIndex,
                        isLight: true);

                    GetData(cbp, data);
                }
            }
            else
            {
                // TODO: Flush when the device only supports one queue.
            }

            return data;
        }

        private void GetData(CommandBufferPool cbp, byte[] data)
        {
            using var bufferHolder = _gd.BufferManager.Create(_gd, data.Length);

            using (var cbs = cbp.Rent())
            {
                var buffer = bufferHolder.GetBuffer(cbs.CommandBuffer).Get(cbs).Value;
                var image = GetImage().Get(cbs).Value;

                CopyFromOrToBuffer(cbs.CommandBuffer, buffer, image, data.Length, true, 0, 0,  Info.GetLayers(), Info.Levels, singleSlice: false);
            }

            bufferHolder.WaitForFences();
            bufferHolder.GetData(0, data);
        }

        public void SetData(ReadOnlySpan<byte> data)
        {
            SetData(data, 0, 0, Info.GetLayers(), Info.Levels, singleSlice: false);
        }

        public void SetData(ReadOnlySpan<byte> data, int layer, int level)
        {
            SetData(data, layer, level, 1, 1, singleSlice: true);
        }

        private void SetData(ReadOnlySpan<byte> data, int layer, int level, int layers, int levels, bool singleSlice)
        {
            using var bufferHolder = _gd.BufferManager.Create(_gd, data.Length);

            using var cbs = _gd.CommandBufferPool.Rent();

            bufferHolder.SetData(0, data);

            var buffer = bufferHolder.GetBuffer(cbs.CommandBuffer).Get(cbs).Value;
            var image = GetImage().Get(cbs).Value;

            CopyFromOrToBuffer(cbs.CommandBuffer, buffer, image, data.Length, false, layer, level, layers, levels, singleSlice);
        }

        private void CopyFromOrToBuffer(
            CommandBuffer commandBuffer,
            VkBuffer buffer,
            Image image,
            int size,
            bool to,
            int dstLayer,
            int dstLevel,
            int dstLayers,
            int dstLevels,
            bool singleSlice)
        {
            bool is3D = Info.Target == Target.Texture3D;
            int width = Info.Width;
            int height = Info.Height;
            int depth = is3D && !singleSlice ? Info.Depth : 1;
            int layer = is3D ? 0 : dstLayer;
            int layers = dstLayers;
            int levels = dstLevels;

            int offset = 0;

            for (int level = 0; level < levels; level++)
            {
                int mipSize = Info.GetMipSize(level);

                int endOffset = offset + mipSize;

                if ((uint)endOffset > (uint)size)
                {
                    break;
                }

                int rowLength = (Info.GetMipStride(level) / Info.BytesPerPixel) * Info.BlockWidth;

                var aspectFlags = Info.Format.ConvertAspectFlags();

                if (aspectFlags == (ImageAspectFlags.ImageAspectDepthBit | ImageAspectFlags.ImageAspectStencilBit))
                {
                    aspectFlags = ImageAspectFlags.ImageAspectDepthBit;
                }

                var sl = new ImageSubresourceLayers(
                    aspectFlags,
                    (uint)(FirstLevel + dstLevel + level),
                    (uint)(FirstLayer + layer),
                    (uint)layers);

                var extent = new Extent3D((uint)width, (uint)height, (uint)depth);

                int z = is3D ? dstLayer : 0;

                var region = new BufferImageCopy((ulong)offset, (uint)rowLength, (uint)height, sl, new Offset3D(0, 0, z), extent);

                if (to)
                {
                    _gd.Api.CmdCopyImageToBuffer(commandBuffer, image, ImageLayout.General, buffer, 1, region);
                }
                else
                {
                    _gd.Api.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.General, 1, region);
                }

                offset += mipSize;

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);

                if (Info.Target == Target.Texture3D)
                {
                    depth = Math.Max(1, depth >> 1);
                }
            }
        }

        public void SetStorage(BufferRange buffer)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Valid = false;

            _imageView.Dispose();
            _imageViewIdentity.Dispose();
            _imageView2dArray?.Dispose();
        }

        public void Release()
        {
            Dispose();
        }
    }
}
