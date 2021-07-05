using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    class TextureStorage : IDisposable
    {
        private const ImageUsageFlags DefaultUsageFlags =
            ImageUsageFlags.ImageUsageSampledBit |
            ImageUsageFlags.ImageUsageTransferSrcBit |
            ImageUsageFlags.ImageUsageTransferDstBit;

        public const AccessFlags DefaultAccessMask =
            AccessFlags.AccessShaderReadBit |
            AccessFlags.AccessShaderWriteBit |
            AccessFlags.AccessColorAttachmentReadBit |
            AccessFlags.AccessColorAttachmentWriteBit |
            AccessFlags.AccessDepthStencilAttachmentReadBit |
            AccessFlags.AccessDepthStencilAttachmentWriteBit |
            AccessFlags.AccessTransferReadBit |
            AccessFlags.AccessTransferWriteBit;

        private readonly VulkanGraphicsDevice _gd;

        private readonly Device _device;

        private readonly TextureCreateInfo _info;

        private readonly Image _image;
        private readonly Auto<DisposableImage> _imageAuto;
        private readonly Auto<MemoryAllocation> _allocationAuto;

        public float ScaleFactor { get; }

        public unsafe TextureStorage(
            VulkanGraphicsDevice gd,
            PhysicalDevice physicalDevice,
            Device device,
            TextureCreateInfo info,
            float scaleFactor)
        {
            _gd = gd;
            _device = device;
            _info = info;
            ScaleFactor = scaleFactor;

            var format = FormatTable.GetFormat(info.Format);
            var levels = (uint)info.Levels;
            var layers = (uint)info.GetLayers();
            var depth = (uint)(info.Target == Target.Texture3D ? info.Depth : 1);

            var type = info.Target.Convert();

            var extent = new Extent3D((uint)info.Width, (uint)info.Height, depth);

            var sampleCountFlags = ConvertToSampleCountFlags((uint)info.Samples);

            var usage = DefaultUsageFlags;

            if (info.Format.IsDepthOrStencil())
            {
                usage |= ImageUsageFlags.ImageUsageDepthStencilAttachmentBit;
            }
            else if (info.BlockWidth == 1 && info.BlockHeight == 1 && info.Format != GAL.Format.R5G5B5A1Unorm)
            {
                usage |= ImageUsageFlags.ImageUsageColorAttachmentBit;
            }

            if (info.Format.IsImageCompatible())
            {
                usage |= ImageUsageFlags.ImageUsageStorageBit;
            }

            var flags = ImageCreateFlags.ImageCreateMutableFormatBit;

            if (info.BlockWidth != 1 || info.BlockHeight != 1)
            {
                flags |= ImageCreateFlags.ImageCreateBlockTexelViewCompatibleBit;
            }

            bool cubeCompatible = info.Width == info.Height && layers >= 6;

            if (type == ImageType.ImageType2D && cubeCompatible)
            {
                flags |= ImageCreateFlags.ImageCreateCubeCompatibleBit;
            }

            if (type == ImageType.ImageType3D)
            {
                flags |= ImageCreateFlags.ImageCreate2DArrayCompatibleBit;
            }

            // System.Console.WriteLine("create image " + type + " " + format + " " + levels + " " + layers + " " + usage + " " + flags);

            var imageCreateInfo = new ImageCreateInfo()
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = type,
                Format = format,
                Extent = extent,
                MipLevels = levels,
                ArrayLayers = layers,
                Samples = sampleCountFlags,
                Tiling = ImageTiling.Optimal,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = flags
            };

            gd.Api.CreateImage(device, imageCreateInfo, null, out _image).ThrowOnError();

            gd.Api.GetImageMemoryRequirements(device, _image, out var requirements);
            var allocation = gd.MemoryAllocator.AllocateDeviceMemory(physicalDevice, requirements);

            if (allocation.Memory.Handle == 0UL)
            {
                gd.Api.DestroyImage(device, _image, null);
                throw new Exception("Image initialization failed.");
            }

            gd.Api.BindImageMemory(device, _image, allocation.Memory, allocation.Offset).ThrowOnError();

            _allocationAuto = new Auto<MemoryAllocation>(allocation);
            _imageAuto = new Auto<DisposableImage>(new DisposableImage(_gd.Api, device, _image), null, _allocationAuto);

            InitialTransition(ImageLayout.General);
        }

        public Auto<DisposableImage> GetImage()
        {
            return _imageAuto;
        }

        public Image GetImageForViewCreation()
        {
            return _image;
        }

        public unsafe void InitialTransition(CommandBufferScoped cbs, ImageLayout dstLayout)
        {
            var aspectFlags = _info.Format.ConvertAspectFlags();

            var subresourceRange = new ImageSubresourceRange(aspectFlags, 0, (uint)_info.Levels, 0, (uint)_info.GetLayers());

            var barrier = new ImageMemoryBarrier()
            {
                SType = StructureType.ImageMemoryBarrier,
                SrcAccessMask = 0,
                DstAccessMask = DefaultAccessMask,
                OldLayout = ImageLayout.Undefined,
                NewLayout = dstLayout,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = _imageAuto.Get(cbs).Value,
                SubresourceRange = subresourceRange
            };

            _gd.Api.CmdPipelineBarrier(
                cbs.CommandBuffer,
                PipelineStageFlags.PipelineStageTopOfPipeBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                0,
                0,
                null,
                0,
                null,
                1,
                barrier);
        }

        private unsafe void InitialTransition(ImageLayout dstLayout)
        {
            using var cbs = _gd.CommandBufferPool.Rent();

            var aspectFlags = _info.Format.ConvertAspectFlags();

            var subresourceRange = new ImageSubresourceRange(aspectFlags, 0, (uint)_info.Levels, 0, (uint)_info.GetLayers());

            var barrier = new ImageMemoryBarrier()
            {
                SType = StructureType.ImageMemoryBarrier,
                SrcAccessMask = 0,
                DstAccessMask = DefaultAccessMask,
                OldLayout = ImageLayout.Undefined,
                NewLayout = dstLayout,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = _imageAuto.Get(cbs).Value,
                SubresourceRange = subresourceRange
            };

            _gd.Api.CmdPipelineBarrier(
                cbs.CommandBuffer,
                PipelineStageFlags.PipelineStageTopOfPipeBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                0,
                0,
                null,
                0,
                null,
                1,
                barrier);
        }

        private static SampleCountFlags ConvertToSampleCountFlags(uint samples)
        {
            return SampleCountFlags.SampleCount1Bit;
            if (samples == 0 || samples > (uint)SampleCountFlags.SampleCount64Bit)
            {
                return SampleCountFlags.SampleCount1Bit;
            }

            // Round up to the nearest power of two.
            return (SampleCountFlags)(1u << (31 - BitOperations.LeadingZeroCount(samples)));
        }

        public TextureView CreateView(TextureCreateInfo info, int firstLayer, int firstLevel)
        {
            return new TextureView(_gd, _device, info, this, firstLayer, firstLevel);
        }

        public void Dispose()
        {
            _imageAuto.Dispose();
            _allocationAuto?.Dispose();
        }
    }
}
