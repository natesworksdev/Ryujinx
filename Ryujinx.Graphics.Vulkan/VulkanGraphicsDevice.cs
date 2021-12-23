using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Ryujinx.Graphics.Vulkan.Queries;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan
{
    public sealed class VulkanGraphicsDevice : IRenderer
    {
        private Instance _instance;
        private SurfaceKHR _surface;
        private PhysicalDevice _physicalDevice;
        private Device _device;
        private Window _window;

        internal FormatCapabilities FormatCapabilities { get; private set; }
        internal HardwareCapabilities Capabilities { get; private set; }

        internal Vk Api { get; private set; }
        internal KhrSurface SurfaceApi { get; private set; }
        internal KhrSwapchain SwapchainApi { get; private set; }
        internal ExtConditionalRendering ConditionalRenderingApi { get; private set; }
        internal ExtExtendedDynamicState ExtendedDynamicStateApi { get; private set; }
        internal ExtTransformFeedback TransformFeedbackApi { get; private set; }
        internal KhrDrawIndirectCount DrawIndirectCountApi { get; private set; }
        internal ExtDebugReport DebugReportApi { get; private set; }

        internal bool SupportsIndexTypeUint8 { get; private set; }
        internal bool SupportsCustomBorderColor { get; private set; }
        internal bool SupportsIndirectParameters { get; private set; }
        internal bool SupportsFragmentShaderInterlock { get; private set; }

        internal uint QueueFamilyIndex { get; private set; }

        internal Queue Queue { get; private set; }
        internal Queue BackgroundQueue { get; private set; }
        internal object BackgroundQueueLock { get; private set; }
        internal object QueueLock { get; private set; }

        internal MemoryAllocator MemoryAllocator { get; private set; }
        internal CommandBufferPool CommandBufferPool { get; private set; }
        internal DescriptorSetManager DescriptorSetManager { get; private set; }
        internal PipelineLayoutCache PipelineLayoutCache { get; private set; }
        internal BackgroundResources BackgroundResources { get; private set; }

        internal BufferManager BufferManager { get; private set; }

        internal HashSet<ShaderCollection> Shaders { get; }
        internal HashSet<ITexture> Textures { get; }
        internal HashSet<SamplerHolder> Samplers { get; }

        private Counters _counters;
        private SyncManager _syncManager;

        private PipelineFull _pipeline;
        private DebugReportCallbackEXT _debugReportCallback;

        internal HelperShader HelperShader { get; private set; }
        internal PipelineFull PipelineInternal => _pipeline;

        public IPipeline Pipeline => _pipeline;

        public IWindow Window => _window;

        private Func<Instance, Vk, SurfaceKHR> GetSurface;
        private Func<string[]> GetRequiredExtensions;

        internal Vendor Vendor { get; private set; }
        internal bool IsIntelWindows { get; private set; }
        public string GpuVendor { get; private set; }
        public string GpuRenderer { get; private set; }
        public string GpuVersion { get; private set; }

        public bool PreferThreading => true;

        public event EventHandler<ScreenCaptureImageInfo> ScreenCaptured;

        public VulkanGraphicsDevice(Func<Instance, Vk, SurfaceKHR> surfaceFunc, Func<string[]> requiredExtensionsFunc)
        {
            GetSurface = surfaceFunc;
            GetRequiredExtensions = requiredExtensionsFunc;
            Shaders = new HashSet<ShaderCollection>();
            Textures = new HashSet<ITexture>();
            Samplers = new HashSet<SamplerHolder>();
        }

        private void SetupContext(GraphicsDebugLevel logLevel)
        {
            var api = Vk.GetApi();

            Api = api;

            _instance = VulkanInitialization.CreateInstance(api, logLevel, GetRequiredExtensions(), out ExtDebugReport debugReport, out _debugReportCallback);

            DebugReportApi = debugReport;

            if (api.TryGetInstanceExtension(_instance, out KhrSurface surfaceApi))
            {
                SurfaceApi = surfaceApi;
            }

            _surface = GetSurface(_instance, api);
            _physicalDevice = VulkanInitialization.FindSuitablePhysicalDevice(api, _instance, _surface);

            FormatCapabilities = new FormatCapabilities(api, _physicalDevice);

            var queueFamilyIndex = VulkanInitialization.FindSuitableQueueFamily(api, _physicalDevice, _surface, out uint maxQueueCount);
            var supportedExtensions = VulkanInitialization.GetSupportedExtensions(api, _physicalDevice);

            _device = VulkanInitialization.CreateDevice(api, _physicalDevice, queueFamilyIndex, supportedExtensions, maxQueueCount);

            Capabilities = new HardwareCapabilities(
                supportedExtensions.Contains(ExtConditionalRendering.ExtensionName),
                supportedExtensions.Contains(ExtExtendedDynamicState.ExtensionName));

            SupportsIndexTypeUint8 = supportedExtensions.Contains("VK_EXT_index_type_uint8");
            SupportsCustomBorderColor = supportedExtensions.Contains("VK_EXT_custom_border_color");
            SupportsIndirectParameters = supportedExtensions.Contains(KhrDrawIndirectCount.ExtensionName);
            SupportsFragmentShaderInterlock = supportedExtensions.Contains("VK_EXT_fragment_shader_interlock");

            if (api.TryGetDeviceExtension(_instance, _device, out KhrSwapchain swapchainApi))
            {
                SwapchainApi = swapchainApi;
            }

            if (api.TryGetDeviceExtension(_instance, _device, out ExtConditionalRendering conditionalRenderingApi))
            {
                ConditionalRenderingApi = conditionalRenderingApi;
            }

            if (api.TryGetDeviceExtension(_instance, _device, out ExtExtendedDynamicState extendedDynamicStateApi))
            {
                ExtendedDynamicStateApi = extendedDynamicStateApi;
            }

            if (api.TryGetDeviceExtension(_instance, _device, out ExtTransformFeedback transformFeedbackApi))
            {
                TransformFeedbackApi = transformFeedbackApi;
            }

            if (api.TryGetDeviceExtension(_instance, _device, out KhrDrawIndirectCount drawIndirectCountApi))
            {
                DrawIndirectCountApi = drawIndirectCountApi;
            }

            api.GetDeviceQueue(_device, queueFamilyIndex, 0, out var queue);
            Queue = queue;
            QueueLock = new object();

            if (maxQueueCount >= 2)
            {
                api.GetDeviceQueue(_device, queueFamilyIndex, 1, out var backgroundQueue);
                BackgroundQueue = backgroundQueue;
                BackgroundQueueLock = new object();
            }

            Api.GetPhysicalDeviceProperties(_physicalDevice, out var properties);

            MemoryAllocator = new MemoryAllocator(api, _device, properties.Limits.MaxMemoryAllocationCount);

            CommandBufferPool = VulkanInitialization.CreateCommandBufferPool(api, _device, queue, QueueLock, queueFamilyIndex);

            DescriptorSetManager = new DescriptorSetManager(_device);

            PipelineLayoutCache = new PipelineLayoutCache();

            BackgroundResources = new BackgroundResources(this, _device);

            BufferManager = new BufferManager(this, _physicalDevice, _device);

            _syncManager = new SyncManager(this, _device);
            _pipeline = new PipelineFull(this, _device);

            HelperShader = new HelperShader(this, _device);

            _counters = new Counters(this, _device, _pipeline);

            _window = new Window(this, _surface, _physicalDevice, _device);
        }

        public IShader CompileShader(ShaderStage stage, ShaderBindings bindings, string code)
        {
            return new Shader(Api, _device, stage, bindings, code);
        }

        public IShader CompileShader(ShaderStage stage, ShaderBindings bindings, byte[] code)
        {
            return new Shader(Api, _device, stage, bindings, code);
        }

        public BufferHandle CreateBuffer(int size)
        {
            return BufferManager.CreateWithHandle(this, size, false);
        }

        public IProgram CreateProgram(IShader[] shaders, TransformFeedbackDescriptor[] transformFeedbackDescriptors)
        {
            return new ShaderCollection(this, _device, shaders, transformFeedbackDescriptors);
        }

        public ISampler CreateSampler(GAL.SamplerCreateInfo info)
        {
            return new SamplerHolder(this, _device, info);
        }

        public ITexture CreateTexture(TextureCreateInfo info, float scale)
        {
            if (info.Target == Target.TextureBuffer)
            {
                return new TextureBuffer(this, info, scale);
            }

            // This should be disposed when all views are destroyed.
            using var storage = CreateTextureStorage(info, scale);
            return storage.CreateView(info, 0, 0);
        }

        internal TextureStorage CreateTextureStorage(TextureCreateInfo info, float scale)
        {
            return new TextureStorage(this, _physicalDevice, _device, info, scale);
        }

        public void DeleteBuffer(BufferHandle buffer)
        {
            BufferManager.Delete(buffer);
        }

        internal void FlushAllCommands()
        {
            // System.Console.WriteLine("flush commands " + caller);
            _pipeline?.FlushCommandsImpl();
        }

        public ReadOnlySpan<byte> GetBufferData(BufferHandle buffer, int offset, int size)
        {
            return BufferManager.GetData(buffer, offset, size);
        }

        public Capabilities GetCapabilities()
        {
            Api.GetPhysicalDeviceFeatures(_physicalDevice, out var features);
            Api.GetPhysicalDeviceProperties(_physicalDevice, out var properties);

            var limits = properties.Limits;

            return new Capabilities(
                TargetApi.Vulkan,
                IsIntelWindows,
                false,
                features.TextureCompressionAstcLdr,
                false,
                SupportsFragmentShaderInterlock,
                false,
                features.ShaderStorageImageReadWithoutFormat,
                true,
                false,
                false,
                false,
                false,
                SupportsIndirectParameters,
                (int)limits.MaxComputeSharedMemorySize,
                (int)limits.MaxSamplerAnisotropy,
                (int)limits.MinStorageBufferOffsetAlignment);
        }

        public HardwareInfo GetHardwareInfo()
        {
            return new HardwareInfo(GpuVendor, GpuRenderer);
        }

        private static string ParseStandardVulkanVersion(uint version)
        {
            return $"{version >> 22}.{(version >> 12) & 0x3FF}.{version & 0xFFF}";
        }

        private static string ParseDriverVersion(ref PhysicalDeviceProperties properties)
        {
            uint driverVersionRaw = properties.DriverVersion;

            // NVIDIA differ from the standard here and use a different format.
            if (properties.VendorID == 0x10DE)
            {
                return $"{(driverVersionRaw >> 22) & 0x3FF}.{(driverVersionRaw >> 14) & 0xFF}.{(driverVersionRaw >> 6) & 0xFF}.{driverVersionRaw & 0x3F}";
            }
            else
            {
                return ParseStandardVulkanVersion(driverVersionRaw);
            }
        }

        private unsafe void PrintGpuInformation()
        {
            Api.GetPhysicalDeviceProperties(_physicalDevice, out var properties);

            string vendorName = properties.VendorID switch
            {
                0x1002 => "AMD",
                0x1010 => "ImgTec",
                0x10DE => "NVIDIA",
                0x13B5 => "ARM",
                0x1AE0 => "Google",
                0x5143 => "Qualcomm",
                0x8086 => "Intel",
                0x10001 => "Vivante",
                0x10002 => "VeriSilicon",
                0x10003 => "Kazan",
                0x10004 => "Codeplay Software Ltd.",
                0x10005 => "Mesa",
                0x10006 => "PoCL",
                _ => $"0x{properties.VendorID:X}"
            };

            Vendor = properties.VendorID switch
            {
                0x1002 => Vendor.Amd,
                0x10DE => Vendor.Nvidia,
                0x8086 => Vendor.Intel,
                _ => Vendor.Unknown
            };

            IsIntelWindows = Vendor == Vendor.Intel && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            GpuVendor = vendorName;
            GpuRenderer = Marshal.PtrToStringAnsi((IntPtr)properties.DeviceName);
            GpuVersion = $"Vulkan v{ParseStandardVulkanVersion(properties.ApiVersion)}, Driver v{ParseDriverVersion(ref properties)}";

            Logger.Notice.Print(LogClass.Gpu, $"{GpuVendor} {GpuRenderer} ({GpuVersion})");
        }

        public void Initialize(GraphicsDebugLevel logLevel)
        {
            SetupContext(logLevel);

            PrintGpuInformation();
        }

        public void PreFrame()
        {
            _syncManager.Cleanup();
        }

        public ICounterEvent ReportCounter(CounterType type, EventHandler<ulong> resultHandler, bool hostReserved)
        {
            return _counters.QueueReport(type, resultHandler, hostReserved);
        }

        public void ResetCounter(CounterType type)
        {
            _counters.QueueReset(type);
        }

        public void SetBufferData(BufferHandle buffer, int offset, ReadOnlySpan<byte> data)
        {
            BufferManager.SetData(buffer, offset, data, _pipeline.CurrentCommandBuffer, _pipeline.EndRenderPass);
        }

        public void UpdateCounters()
        {
            _counters.Update();
        }

        public unsafe void Dispose()
        {
            CommandBufferPool.Dispose();
            BackgroundResources.Dispose();
            _counters.Dispose();
            _window.Dispose();
            HelperShader.Dispose();
            _pipeline.Dispose();
            BufferManager.Dispose();
            DescriptorSetManager.Dispose();
            PipelineLayoutCache.Dispose();

            SurfaceApi.DestroySurface(_instance, _surface, null);

            MemoryAllocator.Dispose();

            if (_debugReportCallback.Handle != 0)
            {
                DebugReportApi.DestroyDebugReportCallback(_instance, _debugReportCallback, null);
            }

            foreach (var shader in Shaders)
            {
                shader.Dispose();
            }

            foreach (var texture in Textures)
            {
                texture.Release();
            }

            foreach (var sampler in Samplers)
            {
                sampler.Dispose();
            }

            Api.DestroyDevice(_device, null);

            // Last step destroy the instance
            Api.DestroyInstance(_instance, null);
        }

        public void BackgroundContextAction(Action action, bool alwaysBackground = false)
        {
            action();
        }

        public void CreateSync(ulong id)
        {
            _syncManager.Create(id);
        }

        public IProgram LoadProgramBinary(byte[] programBinary)
        {
            throw new NotImplementedException();
        }

        public void WaitSync(ulong id)
        {
            _syncManager.Wait(id);
        }

        public void Screenshot()
        {
            _window.ScreenCaptureRequested = true;
        }

        public void OnScreenCaptured(ScreenCaptureImageInfo bitmap)
        {
            ScreenCaptured?.Invoke(this, bitmap);
        }
    }
}
