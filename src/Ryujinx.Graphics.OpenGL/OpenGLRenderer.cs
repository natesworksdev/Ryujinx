using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using Ryujinx.Graphics.OpenGL.Queries;
using Ryujinx.Graphics.Shader.Translation;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.ARB;
using System;
using System.Runtime.InteropServices;
using Sampler = Ryujinx.Graphics.OpenGL.Image.Sampler;

namespace Ryujinx.Graphics.OpenGL
{
    public sealed class OpenGLRenderer : IRenderer
    {
        public readonly GL Api;
        private readonly Pipeline _pipeline;

        public IPipeline Pipeline => _pipeline;

        private readonly Counters _counters;

        private readonly Window _window;

        public IWindow Window => _window;

        private readonly TextureCopy _textureCopy;
        private readonly TextureCopy _backgroundTextureCopy;
        internal TextureCopy TextureCopy => BackgroundContextWorker.InBackground ? _backgroundTextureCopy : _textureCopy;
        internal TextureCopyIncompatible TextureCopyIncompatible { get; }
        internal TextureCopyMS TextureCopyMS { get; }

        private readonly Sync _sync;

        public event EventHandler<ScreenCaptureImageInfo> ScreenCaptured;

        internal PersistentBuffers PersistentBuffers { get; }

        internal ResourcePool ResourcePool { get; }

        internal int BufferCount { get; private set; }

        internal HardwareCapabilities Capabilities;

        public string GpuVendor { get; private set; }
        public string GpuRenderer { get; private set; }
        public string GpuVersion { get; private set; }

        public bool PreferThreading => true;

        public OpenGLRenderer(GL api)
        {
            Api = api;
            _pipeline = new Pipeline(this);
            _counters = new Counters(Api);
            _window = new Window(this);
            _textureCopy = new TextureCopy(this);
            _backgroundTextureCopy = new TextureCopy(this);
            TextureCopyIncompatible = new TextureCopyIncompatible(this);
            TextureCopyMS = new TextureCopyMS(this);
            _sync = new Sync(this);
            PersistentBuffers = new PersistentBuffers(Api);
            ResourcePool = new ResourcePool();
        }

        public BufferHandle CreateBuffer(int size, BufferAccess access)
        {
            BufferCount++;

            if (access.HasFlag(BufferAccess.FlushPersistent))
            {
                BufferHandle handle = Buffer.CreatePersistent(Api, size);

                PersistentBuffers.Map(handle, size);

                return handle;
            }
            else
            {
                return Buffer.Create(Api, size);
            }
        }

        public BufferHandle CreateBuffer(int size, GAL.BufferAccess access, BufferHandle storageHint)
        {
            return CreateBuffer(size, access);
        }

        public BufferHandle CreateBuffer(nint pointer, int size)
        {
            throw new NotSupportedException();
        }

        public BufferHandle CreateBufferSparse(ReadOnlySpan<BufferRange> storageBuffers)
        {
            throw new NotSupportedException();
        }

        public IImageArray CreateImageArray(int size, bool isBuffer)
        {
            return new ImageArray(Api, size);
        }

        public IProgram CreateProgram(ShaderSource[] shaders, ShaderInfo info)
        {
            return new Program(this, shaders, info.FragmentOutputMap);
        }

        public ISampler CreateSampler(SamplerCreateInfo info)
        {
            return new Sampler(this, info);
        }

        public ITexture CreateTexture(TextureCreateInfo info)
        {
            if (info.Target == Target.TextureBuffer)
            {
                return new TextureBuffer(this, info);
            }
            else
            {
                return ResourcePool.GetTextureOrNull(info) ?? new TextureStorage(this, info).CreateDefaultView();
            }
        }

        public ITextureArray CreateTextureArray(int size, bool isBuffer)
        {
            return new TextureArray(Api, size);
        }

        public void DeleteBuffer(BufferHandle buffer)
        {
            PersistentBuffers.Unmap(buffer);

            Buffer.Delete(Api, buffer);
        }

        public HardwareInfo GetHardwareInfo()
        {
            return new HardwareInfo(GpuVendor, GpuRenderer, GpuVendor); // OpenGL does not provide a driver name, vendor name is closest analogue.
        }

        public PinnedSpan<byte> GetBufferData(BufferHandle buffer, int offset, int size)
        {
            return Buffer.GetData(this, buffer, offset, size);
        }

        public Capabilities GetCapabilities()
        {
            bool intelWindows = Capabilities.GpuVendor == OpenGL.GpuVendor.IntelWindows;
            bool intelUnix = Capabilities.GpuVendor == OpenGL.GpuVendor.IntelUnix;
            bool amdWindows = Capabilities.GpuVendor == OpenGL.GpuVendor.AmdWindows;

            return new Capabilities(
                api: TargetApi.OpenGL,
                vendorName: GpuVendor,
                hasFrontFacingBug: intelWindows,
                hasVectorIndexingBug: amdWindows,
                needsFragmentOutputSpecialization: false,
                reduceShaderPrecision: false,
                supportsAstcCompression: Capabilities.SupportsAstcCompression,
                supportsBc123Compression: Capabilities.SupportsTextureCompressionS3tc,
                supportsBc45Compression: Capabilities.SupportsTextureCompressionRgtc,
                supportsBc67Compression: true, // Should check BPTC extension, but for some reason NVIDIA is not exposing the extension.
                supportsEtc2Compression: true,
                supports3DTextureCompression: false,
                supportsBgraFormat: false,
                supportsR4G4Format: false,
                supportsR4G4B4A4Format: true,
                supportsSnormBufferTextureFormat: false,
                supports5BitComponentFormat: true,
                supportsSparseBuffer: false,
                supportsBlendEquationAdvanced: Capabilities.SupportsBlendEquationAdvanced,
                supportsFragmentShaderInterlock: Capabilities.SupportsFragmentShaderInterlock,
                supportsFragmentShaderOrderingIntel: Capabilities.SupportsFragmentShaderOrdering,
                supportsGeometryShader: true,
                supportsGeometryShaderPassthrough: Capabilities.SupportsGeometryShaderPassthrough,
                supportsTransformFeedback: true,
                supportsImageLoadFormatted: Capabilities.SupportsImageLoadFormatted,
                supportsLayerVertexTessellation: Capabilities.SupportsShaderViewportLayerArray,
                supportsMismatchingViewFormat: Capabilities.SupportsMismatchingViewFormat,
                supportsCubemapView: true,
                supportsNonConstantTextureOffset: Capabilities.SupportsNonConstantTextureOffset,
                supportsScaledVertexFormats: true,
                supportsSeparateSampler: false,
                supportsShaderBallot: Capabilities.SupportsShaderBallot,
                supportsShaderBarrierDivergence: !(intelWindows || intelUnix),
                supportsShaderFloat64: true,
                supportsTextureGatherOffsets: true,
                supportsTextureShadowLod: Capabilities.SupportsTextureShadowLod,
                supportsVertexStoreAndAtomics: true,
                supportsViewportIndexVertexTessellation: Capabilities.SupportsShaderViewportLayerArray,
                supportsViewportMask: Capabilities.SupportsViewportArray2,
                supportsViewportSwizzle: Capabilities.SupportsViewportSwizzle,
                supportsIndirectParameters: Capabilities.SupportsIndirectParameters,
                supportsDepthClipControl: true,
                maximumUniformBuffersPerStage: 13, // TODO: Avoid hardcoding those limits here and get from driver?
                maximumStorageBuffersPerStage: 16,
                maximumTexturesPerStage: 32,
                maximumImagesPerStage: 8,
                maximumComputeSharedMemorySize: Capabilities.MaximumComputeSharedMemorySize,
                maximumSupportedAnisotropy: Capabilities.MaximumSupportedAnisotropy,
                shaderSubgroupSize: Constants.MaxSubgroupSize,
                storageBufferOffsetAlignment: Capabilities.StorageBufferOffsetAlignment,
                textureBufferOffsetAlignment: Capabilities.TextureBufferOffsetAlignment,
                gatherBiasPrecision: intelWindows || amdWindows ? 8 : 0); // Precision is 8 for these vendors on Vulkan.
        }

        public void SetBufferData(BufferHandle buffer, int offset, ReadOnlySpan<byte> data)
        {
            Buffer.SetData(Api, buffer, offset, data);
        }

        public void UpdateCounters()
        {
            _counters.Update();
        }

        public void PreFrame()
        {
            _sync.Cleanup();
            ResourcePool.Tick();
        }

        public ICounterEvent ReportCounter(CounterType type, EventHandler<ulong> resultHandler, float divisor, bool hostReserved)
        {
            return _counters.QueueReport(type, resultHandler, divisor, _pipeline.DrawCount, hostReserved);
        }

        public void Initialize(GraphicsDebugLevel glLogLevel)
        {
            Debugger.Initialize(Api, glLogLevel);

            LoadFeatures();

            PrintGpuInformation();

            if (Capabilities.SupportsParallelShaderCompile)
            {
                Api.TryGetExtension(out ArbParallelShaderCompile arbParallelShaderCompile);

                arbParallelShaderCompile.MaxShaderCompilerThreads((uint)Math.Min(Environment.ProcessorCount, 8));
            }

            _counters.Initialize();

            // This is required to disable [0, 1] clamping for SNorm outputs on compatibility profiles.
            // This call is expected to fail if we're running with a core profile,
            // as this clamp target was deprecated, but that's fine as a core profile
            // should already have the desired behaviour when outputs are not clamped.
            Api.ClampColor(ClampColorTargetARB.FragmentColorArb, ClampColorModeARB.False);
        }

        private void LoadFeatures()
        {
            Capabilities = new HardwareCapabilities(
                HardwareCapabilities.HasExtension(Api, "GL_NV_alpha_to_coverage_dither_control"),
                HardwareCapabilities.HasExtension(Api, "GL_KHR_texture_compression_astc_ldr"),
                HardwareCapabilities.HasExtension(Api, "GL_NV_blend_equation_advanced"),
                HardwareCapabilities.HasExtension(Api, "GL_NV_draw_texture"),
                HardwareCapabilities.HasExtension(Api, "GL_ARB_fragment_shader_interlock"),
                HardwareCapabilities.HasExtension(Api, "GL_INTEL_fragment_shader_ordering"),
                HardwareCapabilities.HasExtension(Api, "GL_NV_geometry_shader_passthrough"),
                HardwareCapabilities.HasExtension(Api, "GL_EXT_shader_image_load_formatted"),
                HardwareCapabilities.HasExtension(Api, "GL_ARB_indirect_parameters"),
                HardwareCapabilities.HasExtension(Api, "GL_ARB_parallel_shader_compile"),
                HardwareCapabilities.HasExtension(Api, "GL_EXT_polygon_offset_clamp"),
                HardwareCapabilities.SupportsQuadsCheck(Api),
                HardwareCapabilities.HasExtension(Api, "GL_ARB_seamless_cubemap_per_texture"),
                HardwareCapabilities.HasExtension(Api, "GL_ARB_shader_ballot"),
                HardwareCapabilities.HasExtension(Api, "GL_ARB_shader_viewport_layer_array"),
                HardwareCapabilities.HasExtension(Api, "GL_NV_viewport_array2"),
                HardwareCapabilities.HasExtension(Api, "GL_EXT_texture_compression_bptc"),
                HardwareCapabilities.HasExtension(Api, "GL_EXT_texture_compression_rgtc"),
                HardwareCapabilities.HasExtension(Api, "GL_EXT_texture_compression_s3tc"),
                HardwareCapabilities.HasExtension(Api, "GL_EXT_texture_shadow_lod"),
                HardwareCapabilities.HasExtension(Api, "GL_NV_viewport_swizzle"),
                Api.GetInteger(GLEnum.MaxComputeSharedMemorySize),
                Api.GetInteger(GLEnum.ShaderStorageBufferOffsetAlignment),
                Api.GetInteger(GLEnum.TextureBufferOffsetAlignment),
                Api.GetFloat(GLEnum.MaxTextureMaxAnisotropy),
                HardwareCapabilities.GetGpuVendor(Api));
        }

        private unsafe void PrintGpuInformation()
        {
            GpuVendor = Marshal.PtrToStringAnsi((IntPtr)Api.GetString(StringName.Vendor));
            GpuRenderer = Marshal.PtrToStringAnsi((IntPtr)Api.GetString(StringName.Renderer));
            GpuVersion = Marshal.PtrToStringAnsi((IntPtr)Api.GetString(StringName.Version));

            Logger.Notice.Print(LogClass.Gpu, $"{GpuVendor} {GpuRenderer} ({GpuVersion})");
        }

        public void ResetCounter(CounterType type)
        {
            _counters.QueueReset(type);
        }

        public void BackgroundContextAction(Action action, bool alwaysBackground = false)
        {
            // alwaysBackground is ignored, since we cannot switch from the current context.

            if (_window.BackgroundContext.HasContext())
            {
                action(); // We have a context already - use that (assuming it is the main one).
            }
            else
            {
                _window.BackgroundContext.Invoke(action);
            }
        }

        public void InitializeBackgroundContext(IOpenGLContext baseContext)
        {
            _window.InitializeBackgroundContext(baseContext);
        }

        public void Dispose()
        {
            _textureCopy.Dispose();
            _backgroundTextureCopy.Dispose();
            TextureCopyMS.Dispose();
            PersistentBuffers.Dispose();
            ResourcePool.Dispose();
            _pipeline.Dispose();
            _window.Dispose();
            _counters.Dispose();
            _sync.Dispose();
        }

        public IProgram LoadProgramBinary(byte[] programBinary, bool hasFragmentShader, ShaderInfo info)
        {
            return new Program(this, programBinary, hasFragmentShader, info.FragmentOutputMap);
        }

        public void CreateSync(ulong id, bool strict)
        {
            _sync.Create(id);
        }

        public void WaitSync(ulong id)
        {
            _sync.Wait(id);
        }

        public ulong GetCurrentSync()
        {
            return _sync.GetCurrent();
        }

        public void SetInterruptAction(Action<Action> interruptAction)
        {
            // Currently no need for an interrupt action.
        }

        public void Screenshot()
        {
            _window.ScreenCaptureRequested = true;
        }

        public void OnScreenCaptured(ScreenCaptureImageInfo bitmap)
        {
            ScreenCaptured?.Invoke(this, bitmap);
        }

        public bool PrepareHostMapping(nint address, ulong size)
        {
            return false;
        }
    }
}
