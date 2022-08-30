using Silk.NET.Core;
using System;

namespace Ryujinx.Graphics.Vulkan.MoltenVK
{
    enum MVKConfigLogLevel : int
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    enum MVKConfigTraceVulkanCalls : int
    {
        None = 0,
        Enter = 1,
        EnterExit = 2,
        Duration = 3
    }

    enum MVKConfigAutoGPUCaptureScope : int
    {
        None = 0,
        Device = 1,
        Frame = 2
    }

    [Flags]
    enum MVKConfigAdvertiseExtensions : int
    {
        All = 0x00000001,
        MoltenVK = 0x00000002,
        WSI = 0x00000004,
        Portability = 0x00000008
    }

    struct MVKConfiguration
    {
        public Bool32 DebugMode;
        public Bool32 ShaderConversionFlipVertexY;
        public Bool32 SynchronousQueueSubmits;
        public Bool32 PrefillMetalCommandBuffers;
        public uint MaxActiveMetalCommandBuffersPerQueue;
        public Bool32 SupportLargeQueryPools;
        public Bool32 PresentWithCommandBuffer;
        public Bool32 SwapchainMagFilterUseNearest;
        public ulong MetalCompileTimeout;
        public Bool32 PerformanceTracking;
        public uint PerformanceLoggingFrameCount;
        public Bool32 DisplayWatermark;
        public Bool32 SpecializedQueueFamilies;
        public Bool32 SwitchSystemGPU;
        public Bool32 FullImageViewSwizzle;
        public uint DefaultGPUCaptureScopeQueueFamilyIndex;
        public uint DefaultGPUCaptureScopeQueueIndex;
        public Bool32 FastMathEnabled;
        public MVKConfigLogLevel LogLevel;
        public MVKConfigTraceVulkanCalls TraceVulkanCalls;
        public Bool32 ForceLowPowerGPU;
        public Bool32 SemaphoreUseMTLFence;
        public Bool32 SemaphoreUseMTLEvent;
        public MVKConfigAutoGPUCaptureScope AutoGPUCaptureScope;
        public IntPtr AutoGPUCaptureOutputFilepath;
        public Bool32 Texture1DAs2D;
        public Bool32 PreallocateDescriptors;
        public Bool32 UseCommandPooling;
        public Bool32 UseMTLHeap;
        public Bool32 LogActivityPerformanceInline;
        public uint ApiVersionToAdvertise;
        public MVKConfigAdvertiseExtensions AdvertiseExtensions;
        public Bool32 ResumeLostDevice;
        public Bool32 UseMetalArgumentBuffers;

    }
}
