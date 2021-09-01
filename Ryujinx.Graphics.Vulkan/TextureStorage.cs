using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Numerics;
using VkFormat = Silk.NET.Vulkan.Format;

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

        private Dictionary<GAL.Format, TextureStorage> _aliasedStorages;

        public VkFormat VkFormat { get; }
        public float ScaleFactor { get; }

        public unsafe TextureStorage(
            VulkanGraphicsDevice gd,
            PhysicalDevice physicalDevice,
            Device device,
            TextureCreateInfo info,
            float scaleFactor,
            Auto<MemoryAllocation> foreignAllocation = null)
        {
            _gd = gd;
            _device = device;
            _info = info;
            ScaleFactor = scaleFactor;

            var format = _gd.FormatCapabilities.ConvertToVkFormat(info.Format);
            var levels = (uint)info.Levels;
            var layers = (uint)info.GetLayers();
            var depth = (uint)(info.Target == Target.Texture3D ? info.Depth : 1);

            VkFormat = format;

            var type = info.Target.Convert();

            var extent = new Extent3D((uint)info.Width, (uint)info.Height, depth);

            var sampleCountFlags = ConvertToSampleCountFlags((uint)info.Samples);

            var usage = DefaultUsageFlags;

            if (info.Format.IsDepthOrStencil())
            {
                usage |= ImageUsageFlags.ImageUsageDepthStencilAttachmentBit;
            }
            else if (info.Format.IsRtColorCompatible())
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

            if (foreignAllocation == null)
            {
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

                InitialTransition(ImageLayout.Undefined, ImageLayout.General);
            }
            else
            {
                var allocation = foreignAllocation.GetUnsafe();

                gd.Api.BindImageMemory(device, _image, allocation.Memory, allocation.Offset).ThrowOnError();

                _imageAuto = new Auto<DisposableImage>(new DisposableImage(_gd.Api, device, _image));

                InitialTransition(ImageLayout.Preinitialized, ImageLayout.General);
            }
        }

        public TextureStorage CreateAliasedColorForDepthStorageUnsafe(GAL.Format format)
        {
            var colorFormat = format switch
            {
                GAL.Format.S8Uint => GAL.Format.R8Unorm,
                GAL.Format.D16Unorm => GAL.Format.R16Unorm,
                GAL.Format.D24X8Unorm => GAL.Format.R8G8B8A8Unorm,
                GAL.Format.D32Float => GAL.Format.R32Float,
                GAL.Format.D24UnormS8Uint => GAL.Format.R8G8B8A8Unorm,
                GAL.Format.D32FloatS8Uint => GAL.Format.R32G32Float,
                _ => throw new ArgumentException($"\"{format}\" is not a supported depth or stencil format.")
            };

            return CreateAliasedStorageUnsafe(colorFormat);
        }

        public TextureStorage CreateAliasedStorageUnsafe(GAL.Format format)
        {
            if (_aliasedStorages == null || !_aliasedStorages.TryGetValue(format, out var storage))
            {
                _aliasedStorages ??= new Dictionary<GAL.Format, TextureStorage>();

                var info = NewCreateInfoWith(_info, format, _info.BytesPerPixel);

                storage = new TextureStorage(_gd, default, _device, info, ScaleFactor, _allocationAuto);

                _aliasedStorages.Add(format, storage);
            }

            return storage;
        }

        public static TextureCreateInfo NewCreateInfoWith(TextureCreateInfo info, GAL.Format format, int bytesPerPixel)
        {
            return new TextureCreateInfo(
                info.Width,
                info.Height,
                info.Depth,
                info.Levels,
                info.Samples,
                info.BlockWidth,
                info.BlockHeight,
                bytesPerPixel,
                format,
                info.DepthStencilMode,
                info.Target,
                info.SwizzleR,
                info.SwizzleG,
                info.SwizzleB,
                info.SwizzleA);
        }

        public Auto<DisposableImage> GetImage()
        {
            return _imageAuto;
        }

        public Image GetImageForViewCreation()
        {
            return _image;
        }

        private unsafe void InitialTransition(ImageLayout srcLayout, ImageLayout dstLayout)
        {
            using var cbs = _gd.CommandBufferPool.Rent();

            var aspectFlags = _info.Format.ConvertAspectFlags();

            var subresourceRange = new ImageSubresourceRange(aspectFlags, 0, (uint)_info.Levels, 0, (uint)_info.GetLayers());

            var barrier = new ImageMemoryBarrier()
            {
                SType = StructureType.ImageMemoryBarrier,
                SrcAccessMask = 0,
                DstAccessMask = DefaultAccessMask,
                OldLayout = srcLayout,
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
            if (_aliasedStorages != null)
            {
                foreach (var storage in _aliasedStorages.Values)
                {
                    storage.Dispose();
                }

                _aliasedStorages.Clear();
            }

            _imageAuto.Dispose();
            _allocationAuto?.Dispose();
        }
    }
}
