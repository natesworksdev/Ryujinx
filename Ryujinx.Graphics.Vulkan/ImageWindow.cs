using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using VkFormat = Silk.NET.Vulkan.Format;
using ResetEvent = System.Threading.ManualResetEventSlim;

namespace Ryujinx.Graphics.Vulkan
{
    class ImageWindow : WindowBase, IWindow, IDisposable
    {
        internal const VkFormat Format = VkFormat.R8G8B8A8Unorm;

        private const int ImageCount = 5;
        private const int SurfaceWidth = 1280;
        private const int SurfaceHeight = 720;

        private readonly VulkanRenderer _gd;
        private readonly PhysicalDevice _physicalDevice;
        private readonly Device _device;
        private readonly Instance _instance;

        private Auto<DisposableImage>[] _images;
        private Auto<DisposableImageView>[] _imageViews;
        private Auto<DisposableMemory>[] _imageMemory;
        private ResetEvent[] _imageInUseEvents;
        private ImageState[] _states;
        private PresentImageInfo[] _presentedImages;
        private unsafe void*[] _memoryMaps;

        private ulong[] _imageSizes;
        private ulong[] _imageOffsets;

        private Semaphore _imageAvailableSemaphore;
        private Semaphore _renderFinishedSemaphore;

        private int _width = SurfaceWidth;
        private int _height = SurfaceHeight;
        private bool _recreateImages;
        private bool _isSameGpu;
        private int _nextImage;
        private Auto<DisposableImage> _stagingImage;
        private Auto<DisposableImageView> _stagingImageView;
        private ulong _stagingImageSizes;
        private Auto<DisposableMemory> _stagingMemory;

        internal new bool ScreenCaptureRequested { get; set; }

        public unsafe ImageWindow(VulkanRenderer gd, Instance instance, PhysicalDevice physicalDevice, Device device, bool sameGpu)
        {
            _gd = gd;
            _physicalDevice = physicalDevice;
            _device = device;
            _isSameGpu = sameGpu;
            _instance = instance;

            _images = new Auto<DisposableImage>[ImageCount];
            _imageMemory = new Auto<DisposableMemory>[ImageCount];
            _imageSizes = new ulong[ImageCount];
            _imageOffsets = new ulong[ImageCount];
            _imageInUseEvents = new ResetEvent[ImageCount];
            _states = new ImageState[ImageCount];
            _presentedImages = new PresentImageInfo[ImageCount];
            _memoryMaps = new void*[ImageCount];

            CreateImages();

            var semaphoreCreateInfo = new SemaphoreCreateInfo()
            {
                SType = StructureType.SemaphoreCreateInfo
            };

            gd.Api.CreateSemaphore(device, semaphoreCreateInfo, null, out _imageAvailableSemaphore).ThrowOnError();
            gd.Api.CreateSemaphore(device, semaphoreCreateInfo, null, out _renderFinishedSemaphore).ThrowOnError();
        }

        private void RecreateImages()
        {
            for (int i = 0; i < ImageCount; i++)
            {
                _states[i].IsValid = false;
                _imageInUseEvents[i].Wait();
                _imageViews[i]?.Dispose();
                if (!_isSameGpu)
                {
                    _gd.Api.UnmapMemory(_device, _imageMemory[i].GetUnsafe().Memory);
                }
                _imageMemory[i]?.Dispose();
                _images[i]?.Dispose();
                _stagingImageView?.Dispose();
                _stagingMemory?.Dispose();
                _stagingImage?.Dispose();
                _presentedImages = null;
            }

            CreateImages();
        }

        private void CreateImages()
        {
            _imageViews = new Auto<DisposableImageView>[ImageCount];
            _presentedImages = new PresentImageInfo[ImageCount];
            unsafe
            {
                var cbs = _gd.CommandBufferPool.Rent();
                ExternalMemoryHandleTypeFlags flags = _isSameGpu ? default : ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeHostAllocationBitExt;

                if (_isSameGpu)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        flags |= ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueWin32Bit;
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        flags |= ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueFDBit;
                    }
                }

                var externalImageCreateInfo = new ExternalMemoryImageCreateInfo()
                {
                    SType = StructureType.ExternalMemoryImageCreateInfo,
                    HandleTypes = flags,
                };

                var exportMemoryAllocateInfo = new ExportMemoryAllocateInfo()
                {
                    SType = StructureType.ExportMemoryAllocateInfo,
                    HandleTypes = flags
                };

                var imageCreateInfo = new ImageCreateInfo
                {
                    SType = StructureType.ImageCreateInfo,
                    ImageType = ImageType.ImageType2D,
                    Format = Format,
                    Extent =
                            new Extent3D((uint?)_width,
                                (uint?)_height, 1),
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.SampleCount1Bit,
                    Tiling = _isSameGpu ? ImageTiling.Optimal : ImageTiling.Linear,
                    Usage = _isSameGpu ? ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit
                    : ImageUsageFlags.ImageUsageSampledBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined,
                    Flags = ImageCreateFlags.ImageCreateMutableFormatBit,
                    PNext = &externalImageCreateInfo
                };

                for (int i = 0; i < _images.Length; i++)
                {
                    _gd.Api.CreateImage(_device, imageCreateInfo, null, out var image).ThrowOnError();
                    _images[i] = new Auto<DisposableImage>(new DisposableImage(_gd.Api, _device, image));

                    _gd.Api.GetImageMemoryRequirements(_device, image,
                        out var memoryRequirements);

                    var memoryAllocateInfo = new MemoryAllocateInfo
                    {
                        SType = StructureType.MemoryAllocateInfo,
                        AllocationSize = memoryRequirements.Size,
                        MemoryTypeIndex = (uint)MemoryAllocator.FindSuitableMemoryTypeIndex(_gd.Api,
                            _physicalDevice,
                            memoryRequirements.MemoryTypeBits, _isSameGpu ? MemoryPropertyFlags.MemoryPropertyDeviceLocalBit :
                             MemoryPropertyFlags.MemoryPropertyHostCachedBit | MemoryPropertyFlags.MemoryPropertyHostVisibleBit
                             | MemoryPropertyFlags.MemoryPropertyHostCoherentBit),
                        PNext = &exportMemoryAllocateInfo
                    };

                    _gd.Api.AllocateMemory(_device, memoryAllocateInfo, null, out var memory);

                    _imageSizes[i] = memoryAllocateInfo.AllocationSize;
                    _imageOffsets[i] = 0;

                    _imageMemory[i] = new Auto<DisposableMemory>(new DisposableMemory(_gd.Api, _device, memory));

                    _gd.Api.BindImageMemory(_device, image, memory, 0);

                    _imageViews[i] = CreateImageView(_gd.Api, _device, image, Format);

                    Transition(
                        _gd.Api,
                        cbs.CommandBuffer,
                        image,
                        0,
                        0,
                        ImageLayout.Undefined,
                        _isSameGpu ? ImageLayout.ColorAttachmentOptimal : ImageLayout.TransferDstOptimal);

                    _imageInUseEvents[i] = new ResetEvent(true);
                    _states[i] = new ImageState();

                    if (!_isSameGpu)
                    {
                        void* map = null;
                        _gd.Api.MapMemory(_device, memory, 0, memoryAllocateInfo.AllocationSize, 0, (void**)(&map)).ThrowOnError();

                        _memoryMaps[i] = map;
                    }
                }

                if (!_isSameGpu)
                {
                    var stagingImageCreateInfo = new ImageCreateInfo
                    {
                        SType = StructureType.ImageCreateInfo,
                        ImageType = ImageType.ImageType2D,
                        Format = Format,
                        Extent =
                                new Extent3D((uint?)_width,
                                    (uint?)_height, 1),
                        MipLevels = 1,
                        ArrayLayers = 1,
                        Samples = SampleCountFlags.SampleCount1Bit,
                        Tiling = ImageTiling.Optimal,
                        Usage = ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit,
                        SharingMode = SharingMode.Exclusive,
                        InitialLayout = ImageLayout.Undefined,
                        Flags = ImageCreateFlags.ImageCreateMutableFormatBit,
                    };
                    _gd.Api.CreateImage(_device, stagingImageCreateInfo, null, out var image).ThrowOnError();
                    _stagingImage = new Auto<DisposableImage>(new DisposableImage(_gd.Api, _device, image));

                    _gd.Api.GetImageMemoryRequirements(_device, image,
                        out var memoryRequirements);

                    var memoryAllocateInfo = new MemoryAllocateInfo
                    {
                        SType = StructureType.MemoryAllocateInfo,
                        AllocationSize = memoryRequirements.Size,
                        MemoryTypeIndex = (uint)MemoryAllocator.FindSuitableMemoryTypeIndex(_gd.Api,
                            _physicalDevice,
                            memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit),
                    };

                    _gd.Api.AllocateMemory(_device, memoryAllocateInfo, null, out var memory);

                    _stagingImageSizes = memoryAllocateInfo.AllocationSize;

                    _stagingMemory = new Auto<DisposableMemory>(new DisposableMemory(_gd.Api, _device, memory));

                    _gd.Api.BindImageMemory(_device, image, memory, 0);

                    _stagingImageView = CreateImageView(_gd.Api, _device, image, Format);

                    Transition(
                        _gd.Api,
                        cbs.CommandBuffer,
                        image,
                        0,
                        0,
                        ImageLayout.Undefined,
                        ImageLayout.TransferSrcOptimal);
                }

                _gd.CommandBufferPool.Return(cbs);
            }
        }

        internal static unsafe Auto<DisposableImageView> CreateImageView(Vk api, Device device, Image image, VkFormat format)
        {
            var componentMapping = new ComponentMapping(
                ComponentSwizzle.R,
                ComponentSwizzle.G,
                ComponentSwizzle.B,
                ComponentSwizzle.A);

            var aspectFlags = ImageAspectFlags.ImageAspectColorBit;

            var subresourceRange = new ImageSubresourceRange(aspectFlags, 0, 1, 0, 1);

            var imageCreateInfo = new ImageViewCreateInfo()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = image,
                ViewType = ImageViewType.ImageViewType2D,
                Format = format,
                Components = componentMapping,
                SubresourceRange = subresourceRange
            };

            api.CreateImageView(device, imageCreateInfo, null, out var imageView).ThrowOnError();
            return new Auto<DisposableImageView>(new DisposableImageView(api, device, imageView));
        }

        public override unsafe void Present(ITexture texture, ImageCrop crop, Action<object> swapBuffersCallback)
        {
            if (_recreateImages)
            {
                RecreateImages();
                _recreateImages = false;
            }

            var image = _images[_nextImage];

            _gd.FlushAllCommands();

            var cbs = _gd.CommandBufferPool.Rent();

            Transition(
                _gd.Api,
                cbs.CommandBuffer,
                _isSameGpu ? image.GetUnsafe().Value : _stagingImage.GetUnsafe().Value,
                0,
                AccessFlags.AccessTransferWriteBit,
                ImageLayout.TransferSrcOptimal,
                ImageLayout.General);

            var view = (TextureView)texture;

            int srcX0, srcX1, srcY0, srcY1;
            float scale = view.ScaleFactor;

            if (crop.Left == 0 && crop.Right == 0)
            {
                srcX0 = 0;
                srcX1 = (int)(view.Width / scale);
            }
            else
            {
                srcX0 = crop.Left;
                srcX1 = crop.Right;
            }

            if (crop.Top == 0 && crop.Bottom == 0)
            {
                srcY0 = 0;
                srcY1 = (int)(view.Height / scale);
            }
            else
            {
                srcY0 = crop.Top;
                srcY1 = crop.Bottom;
            }

            if (scale != 1f)
            {
                srcX0 = (int)(srcX0 * scale);
                srcY0 = (int)(srcY0 * scale);
                srcX1 = (int)Math.Ceiling(srcX1 * scale);
                srcY1 = (int)Math.Ceiling(srcY1 * scale);
            }

            if (ScreenCaptureRequested)
            {
                CaptureFrame(view, srcX0, srcY0, srcX1 - srcX0, srcY1 - srcY0, view.Info.Format.IsBgr(), crop.FlipX, crop.FlipY);

                ScreenCaptureRequested = false;
            }

            float ratioX = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _height * crop.AspectRatioX / (_width * crop.AspectRatioY));
            float ratioY = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _width * crop.AspectRatioY / (_height * crop.AspectRatioX));

            int dstWidth = (int)(_width * ratioX);
            int dstHeight = (int)(_height * ratioY);

            int dstPaddingX = (_width - dstWidth) / 2;
            int dstPaddingY = (_height - dstHeight) / 2;

            int dstX0 = crop.FlipX ? _width - dstPaddingX : dstPaddingX;
            int dstX1 = crop.FlipX ? dstPaddingX : _width - dstPaddingX;

            int dstY0 = crop.FlipY ? dstPaddingY : _height - dstPaddingY;
            int dstY1 = crop.FlipY ? _height - dstPaddingY : dstPaddingY;

            _gd.HelperShader.Blit(
                _gd,
                cbs,
                view,
                _isSameGpu ? _imageViews[_nextImage] : _stagingImageView,
                _width,
                _height,
                Format,
                new Extents2D(srcX0, srcY0, srcX1, srcY1),
                new Extents2D(dstX0, dstY1, dstX1, dstY0),
                true,
                true);

            if (!_isSameGpu)
            {
                var region = new ImageCopy()
                {
                    SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit,
                    0, 0, 1),
                    DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit,
                    0, 0, 1),
                    SrcOffset = new Offset3D(0, 0),
                    Extent = new Extent3D((uint)_width, (uint)_height, 1),
                    DstOffset = new Offset3D(0, 0),
                };

                //_imageInUseEvents[_nextImage].Wait();

                _gd.Api.CmdCopyImage(cbs.CommandBuffer, _stagingImage.GetUnsafe().Value,
                    ImageLayout.General, _images[_nextImage].GetUnsafe().Value, ImageLayout.TransferDstOptimal, 1, region);

                var mappedRegions = new MappedMemoryRange()
                {
                    Size = _imageSizes[_nextImage],
                    SType = StructureType.MappedMemoryRange,
                    Memory = _imageMemory[_nextImage].GetUnsafe().Memory,
                    Offset = 0
                };
            }

            Transition(
                _gd.Api,
                cbs.CommandBuffer,
                _isSameGpu ? image.GetUnsafe().Value : _stagingImage.GetUnsafe().Value,
                0,
                0,
                ImageLayout.General,
                ImageLayout.TransferSrcOptimal);

            _gd.CommandBufferPool.Return(
                cbs,
                null,
                new[] { PipelineStageFlags.PipelineStageAllCommandsBit },
                null);

            if (!_isSameGpu)
            {
                cbs.GetFence().Wait();
            }

            _imageInUseEvents[_nextImage].Reset();

            var j = _nextImage;

            PresentImageInfo info = _presentedImages[_nextImage];
            if (info == null)
            {
                info = new PresentImageInfo(
                                        image.GetUnsafe().Value,
                                        _imageMemory[_nextImage].GetUnsafe().Memory,
                                        _device,
                                        _instance,
                                        _physicalDevice,
                                        _imageSizes[_nextImage],
                                        _imageOffsets[_nextImage],
                                        _renderFinishedSemaphore,
                                        _imageAvailableSemaphore,
                                        new Extent2D((uint)_width, (uint)_height),
                                        _states[_nextImage],
                                        _isSameGpu,
                                        _memoryMaps[_nextImage],
                                        cbs.GetFence().GetUnsafe(),
                                        () => { _imageInUseEvents[j].Set(); });

                _presentedImages[_nextImage] = info;
            }

            swapBuffersCallback(info);

            _nextImage = (_nextImage + 1) % ImageCount;
        }

        internal static unsafe void Transition(
            Vk api,
            CommandBuffer commandBuffer,
            Image image,
            AccessFlags srcAccess,
            AccessFlags dstAccess,
            ImageLayout srcLayout,
            ImageLayout dstLayout)
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ImageAspectColorBit, 0, 1, 0, 1);

            var barrier = new ImageMemoryBarrier()
            {
                SType = StructureType.ImageMemoryBarrier,
                SrcAccessMask = srcAccess,
                DstAccessMask = dstAccess,
                OldLayout = srcLayout,
                NewLayout = dstLayout,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = image,
                SubresourceRange = subresourceRange
            };

            api.CmdPipelineBarrier(
                commandBuffer,
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

        private void CaptureFrame(TextureView texture, int x, int y, int width, int height, bool isBgra, bool flipX, bool flipY)
        {
            byte[] bitmap = texture.GetData(x, y, width, height);

            _gd.OnScreenCaptured(new ScreenCaptureImageInfo(width, height, isBgra, bitmap, flipX, flipY));
        }

        public override void SetSize(int width, int height)
        {
            if (_width != width || _height != height)
            {
                _recreateImages = true;
            }

            _width = width;
            _height = height;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                unsafe
                {
                    _gd.Api.DestroySemaphore(_device, _renderFinishedSemaphore, null);
                    _gd.Api.DestroySemaphore(_device, _imageAvailableSemaphore, null);
                    for (int i = 0; i < ImageCount; i++)
                    {
                        _states[i].IsValid = false;
                        _imageInUseEvents[i].Set();
                        _imageInUseEvents[i].Dispose();
                        _imageViews[i]?.Dispose();
                        if (!_isSameGpu)
                        {
                            _gd.Api.UnmapMemory(_device, _imageMemory[i].GetUnsafe().Memory);
                        }
                        _imageMemory[i]?.Dispose();
                        _images[i]?.Dispose();
                        _stagingImageView?.Dispose();
                        _stagingMemory?.Dispose();
                        _stagingImage?.Dispose();
                    }
                }
            }
        }

        public override void Dispose()
        {
            Dispose(true);
        }
    }

    public class ImageState
    {
        private bool _isValid = true;

        public bool IsValid
        {
            get => _isValid; internal set
            {
                _isValid = value;

                StateChanged?.Invoke(this, _isValid);
            }
        }
        public event EventHandler<bool> StateChanged;
    }

    public class PresentImageInfo
    {
        private Auto<DisposableMemory> _stagingMemory = null;
        private Auto<DisposableImage> _stagingImage = null;
        private Auto<DisposableImageView> _stagingImageView = null;
        private Auto<DisposableMemory> _externalMemory = null;
        private Auto<DisposableImage> _externalImage = null;
        private bool _isSameGpu;

        public Image Image { get; }
        public DeviceMemory Memory { get; }
        public Device Device { get; }
        public Instance Instance { get; }
        public PhysicalDevice PhysicalDevice { get; }
        public ulong MemorySize { get; set; }
        public ulong MemoryOffset { get; set; }
        public Semaphore ReadySemaphore { get; }
        public Semaphore AvailableSemaphore { get; }
        public Extent2D Extent { get; }
        public Action CompletionAction { get; }
        public ImageState State { get; internal set; }
        public unsafe void* Pointer { get; }
        public Fence Fence { get; }

        public unsafe PresentImageInfo(
            Image image,
            DeviceMemory memory,
            Device device,
            Instance instance,
            PhysicalDevice physicalDevice,
            ulong memorySize,
            ulong memoryOffset,
            Semaphore readySemaphore,
            Semaphore availableSemaphore,
            Extent2D extent2D,
            ImageState state,
            bool isSameGpu,
            void* pointer,
            Fence fence,
            Action completionAction)
        {
            Image = image;
            Memory = memory;
            Device = device;
            Instance = instance;
            PhysicalDevice = physicalDevice;
            MemorySize = memorySize;
            MemoryOffset = memoryOffset;
            ReadySemaphore = readySemaphore;
            AvailableSemaphore = availableSemaphore;
            Extent = extent2D;
            CompletionAction = completionAction;
            State = state;
            _isSameGpu = isSameGpu;
            Pointer = pointer;
            Fence = fence;

            state.StateChanged += StateChanged;
        }

        private void StateChanged(object sender, bool e)
        {
            if(!e) {
                if(_stagingImage != null) {
                    _stagingMemory.Dispose();
                    _stagingImageView.Dispose();
                    _stagingImage.Dispose();
                }

                if(_externalImage != null) {
                    _externalImage.Dispose();
                    _externalMemory.Dispose();
                }
            }
        }

        public unsafe void GetImage(Device device, PhysicalDevice physicalDevice, CommandBuffer commandBuffer, out Image image, out DeviceMemory memory)
        {
            var api = Vk.GetApi();

            memory = default;

            if (_externalImage != null)
            {
                if (_isSameGpu)
                {
                    image = _externalImage.GetUnsafe().Value;
                    memory = _externalMemory.GetUnsafe().Memory;
                }
                else
                {
                    var region = new ImageCopy()
                    {
                        SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit,
                        0, 0, 1),
                        DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit,
                        0, 0, 1),
                        SrcOffset = new Offset3D(0, 0),
                        Extent = new Extent3D(Extent.Width, Extent.Height, 1),
                        DstOffset = new Offset3D(0, 0)
                    };

                    api.CmdCopyImage(commandBuffer, _externalImage.GetUnsafe().Value,
                        ImageLayout.TransferSrcOptimal, _stagingImage.GetUnsafe().Value, ImageLayout.TransferDstOptimal, 1, region);

                    image = _stagingImage.GetUnsafe().Value;
                    memory = _stagingMemory.GetUnsafe().Memory;
                }

                return;
            }

            ExternalMemoryHandleTypeFlags flags = _isSameGpu ? default : ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeHostAllocationBitExt;

            if (_isSameGpu)
            {
                if (OperatingSystem.IsWindows())
                {
                    flags |= ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueWin32Bit;
                }
                else if (OperatingSystem.IsLinux())
                {
                    flags |= ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueFDBit;
                }
            }

            var externalImageCreateInfo = new ExternalMemoryImageCreateInfo()
            {
                SType = StructureType.ExternalMemoryImageCreateInfo,
                HandleTypes = flags,
            };

            var imageCreateInfo = new ImageCreateInfo
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.ImageType2D,
                Format = ImageWindow.Format,
                Extent =
                            new Extent3D(Extent.Width, Extent.Height, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCountFlags.SampleCount1Bit,
                Tiling = _isSameGpu ? ImageTiling.Optimal : ImageTiling.Linear,
                Usage = _isSameGpu ? ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit :
                  ImageUsageFlags.ImageUsageSampledBit |  ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = ImageCreateFlags.ImageCreateMutableFormatBit,
                PNext = &externalImageCreateInfo
            };

            api.CreateImage(device, imageCreateInfo, null, out image).ThrowOnError();
            api.GetImageMemoryRequirements(device, image,
                                out var memoryRequirements);

            var memoryAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)MemoryAllocator.FindSuitableMemoryTypeIndex(api,
                    physicalDevice,
                    memoryRequirements.MemoryTypeBits, _isSameGpu ? MemoryPropertyFlags.MemoryPropertyDeviceLocalBit :
                             MemoryPropertyFlags.MemoryPropertyHostCachedBit | MemoryPropertyFlags.MemoryPropertyHostVisibleBit
                             | MemoryPropertyFlags.MemoryPropertyHostCoherentBit)
            };

            ImportMemoryHostPointerInfoEXT importmemoryInfo;
            if (_isSameGpu)
            {
                if (OperatingSystem.IsWindows())
                {
                    nint handle = 0;
                    if (api.TryGetDeviceExtension<KhrExternalMemoryWin32>(Instance, Device, out var win32Export))
                    {
                        var getInfo = new MemoryGetWin32HandleInfoKHR()
                        {
                            SType = StructureType.MemoryGetWin32HandleInfoKhr,
                            HandleType = ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueWin32Bit,
                            Memory = Memory
                        };
                        win32Export.GetMemoryWin32Handle(Device, getInfo, out handle).ThrowOnError();
                    }

                    if (handle != 0)
                    {
                        var getInfo = new ImportMemoryWin32HandleInfoKHR
                        {
                            SType = StructureType.ImportMemoryWin32HandleInfoKhr,
                            HandleType = ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueWin32Bit,
                            Handle = handle
                        };

                        memoryAllocateInfo.PNext = &getInfo;

                        api.AllocateMemory(device, memoryAllocateInfo, null,
                            out memory).ThrowOnError();
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    int handle = 0;
                    if (api.TryGetDeviceExtension<KhrExternalMemoryFd>(Instance, Device, out var fdExport))
                    {
                        var getInfo = new MemoryGetFdInfoKHR()
                        {
                            SType = StructureType.MemoryGetFDInfoKhr,
                            HandleType = ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueFDBit,
                            Memory = Memory
                        };
                        fdExport.GetMemoryF(Device, &getInfo, out handle).ThrowOnError();
                    }

                    if (handle != 0)
                    {
                        var getInfo = new ImportMemoryFdInfoKHR
                        {
                            SType = StructureType.ImportMemoryFDInfoKhr,
                            HandleType = ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeOpaqueFDBit,
                            Fd = handle
                        };

                        memoryAllocateInfo.PNext = &getInfo;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            else
            {
                importmemoryInfo = new ImportMemoryHostPointerInfoEXT()
                {
                    SType = StructureType.ImportMemoryHostPointerInfoExt,
                    HandleType = ExternalMemoryHandleTypeFlags.ExternalMemoryHandleTypeHostAllocationBitExt,
                    PHostPointer = Pointer
                };

                memoryAllocateInfo.PNext = &importmemoryInfo;
            }

            api.AllocateMemory(device, memoryAllocateInfo, null,
                out memory).ThrowOnError();

            api.BindImageMemory(device, image, memory, 0).ThrowOnError();

            _externalImage = new Auto<DisposableImage>(new DisposableImage(api, device, image));
            _externalMemory = new Auto<DisposableMemory>(new DisposableMemory(api, device, memory));

            if (!_isSameGpu)
            {
                ImageWindow.Transition(
                    api,
                    commandBuffer,
                    image,
                    0,
                    0,
                    ImageLayout.Undefined,
                    ImageLayout.TransferSrcOptimal);

                var stagingImageCreateInfo = new ImageCreateInfo
                {
                    SType = StructureType.ImageCreateInfo,
                    ImageType = ImageType.ImageType2D,
                    Format = VkFormat.R8G8B8A8Unorm,
                    Extent =
                        new Extent3D(Extent.Width, Extent.Height, 1),
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.SampleCount1Bit,
                    Tiling = ImageTiling.Optimal,
                    Usage = ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined,
                    Flags = ImageCreateFlags.ImageCreateMutableFormatBit,
                };
                api.CreateImage(device, stagingImageCreateInfo, null, out var stagingimage).ThrowOnError();
                _stagingImage = new Auto<DisposableImage>(new DisposableImage(api, device, stagingimage));

                api.GetImageMemoryRequirements(device, stagingimage,
                    out memoryRequirements);

                memoryAllocateInfo = new MemoryAllocateInfo
                {
                    SType = StructureType.MemoryAllocateInfo,
                    AllocationSize = memoryRequirements.Size,
                    MemoryTypeIndex = (uint)MemoryAllocator.FindSuitableMemoryTypeIndex(api,
                        physicalDevice,
                        memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit),
                };

                api.AllocateMemory(device, memoryAllocateInfo, null, out memory);

                _stagingMemory = new Auto<DisposableMemory>(new DisposableMemory(api, device, memory));

                api.BindImageMemory(device, stagingimage, memory, 0);

                _stagingImageView = ImageWindow.CreateImageView(api, device, image, VkFormat.R8G8B8A8Unorm);

                ImageWindow.Transition(
                    api,
                    commandBuffer,
                    stagingimage,
                    0,
                    0,
                    ImageLayout.Undefined,
                    ImageLayout.TransferDstOptimal);

                var region = new ImageCopy()
                {
                    SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit,
                        0, 0, 1),
                    DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit,
                        0, 0, 1),
                    SrcOffset = new Offset3D(0, 0),
                    Extent = stagingImageCreateInfo.Extent,
                    DstOffset = new Offset3D(0, 0)
                };

                api.CmdCopyImage(commandBuffer, image,
                    ImageLayout.TransferSrcOptimal, stagingimage, ImageLayout.TransferDstOptimal, 1, region);

                image = stagingimage;
            }
        }
    }
}