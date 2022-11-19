using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan.Effects
{
    internal partial class FsrUpscaler : IScaler
    {
        private readonly VulkanRenderer _renderer;
        private PipelineHelperShader _scalingPipeline;
        private PipelineHelperShader _sharpeningPipeline;
        private ISampler _sampler;
        private ShaderCollection _scalingProgram;
        private ShaderCollection _sharpeningProgram;
        private float _sharpeningLevel = 1;
        private Device _device;
        private TextureView _intermediaryTexture;

        public float Level
        {
            get => _sharpeningLevel;
            set
            {
                _sharpeningLevel = MathF.Max(0.01f, value);
            }
        }

        public FsrUpscaler(VulkanRenderer renderer, Device device)
        {
            _device = device;
            _renderer = renderer;

            Initialize();
        }

        public void Dispose()
        {
            _scalingPipeline.Dispose();
            _scalingProgram.Dispose();
            _sharpeningPipeline.Dispose();
            _sharpeningProgram.Dispose();
            _sampler.Dispose();
            _intermediaryTexture?.Dispose();
        }

        public void Initialize()
        {
            _scalingPipeline = new PipelineHelperShader(_renderer, _device);
            _sharpeningPipeline = new PipelineHelperShader(_renderer, _device);

            _scalingPipeline.Initialize();
            _sharpeningPipeline.Initialize();

            var scalingShader = EmbeddedResources.Read("Ryujinx.Graphics.Vulkan/Shaders/fsr_scaling.spirv");
            var sharpeningShader = EmbeddedResources.Read("Ryujinx.Graphics.Vulkan/Shaders/fsr_sharpening.spirv");

            var computeBindings = new ShaderBindings(
                new[] { 2 },
                Array.Empty<int>(),
                new[] { 1 },
                new[] { 0 });

            var sharpeningBindings = new ShaderBindings(
                new[] { 2, 3, 4 },
                Array.Empty<int>(),
                new[] { 1 },
                new[] { 0 });

            _sampler = _renderer.CreateSampler(GAL.SamplerCreateInfo.Create(MinFilter.Linear, MagFilter.Linear));

            _scalingProgram = _renderer.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(scalingShader, computeBindings, ShaderStage.Compute, TargetLanguage.Spirv)
            });

            _sharpeningProgram = _renderer.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(sharpeningShader, sharpeningBindings, ShaderStage.Compute, TargetLanguage.Spirv)
            });
        }

        public void Run(
            TextureView view,
            CommandBufferScoped cbs,
            Auto<DisposableImageView> destinationTexture,
            Silk.NET.Vulkan.Format format,
            int width,
            int height,
            int srcX0,
            int srcX1,
            int srcY0,
            int srcY1,
            int dstX0,
            int dstX1,
            int dstY0,
            int dstY1)
        {
            if (_intermediaryTexture == null || _intermediaryTexture.Info.Width != width || _intermediaryTexture.Info.Height != height)
            {
                var originalInfo = view.Info;
                var info = new TextureCreateInfo(
                    width,
                    height,
                    originalInfo.Depth,
                    originalInfo.Levels,
                    originalInfo.Samples,
                    originalInfo.BlockWidth,
                    originalInfo.BlockHeight,
                    originalInfo.BytesPerPixel,
                    originalInfo.Format,
                    originalInfo.DepthStencilMode,
                    originalInfo.Target,
                    originalInfo.SwizzleR,
                    originalInfo.SwizzleG,
                    originalInfo.SwizzleB,
                    originalInfo.SwizzleA);
                _intermediaryTexture?.Dispose();
                _intermediaryTexture = _renderer.CreateTexture(info, view.ScaleFactor) as TextureView;
            }

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];
            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            viewports[0] = new GAL.Viewport(
                new Rectangle<float>(0, 0, view.Width, view.Height),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            scissors[0] = new Rectangle<int>(0, 0, view.Width, view.Height);

            _scalingPipeline.SetCommandBuffer(cbs);
            _scalingPipeline.SetProgram(_scalingProgram);
            _scalingPipeline.SetTextureAndSampler(ShaderStage.Compute, 1, view, _sampler);

            float srcWidth = Math.Abs(srcX1 - srcX0);
            float srcHeight = Math.Abs(srcY1 - srcY0);
            float scaleX = srcWidth / view.Width;
            float scaleY = srcHeight / view.Height;

            ReadOnlySpan<float> dimensionsBuffer = stackalloc float[]
            {
                srcX0,
                srcX1,
                srcY0,
                srcY1,
                dstX0,
                dstX1,
                dstY0,
                dstY1,
                scaleX,
                scaleY
            };

            int rangeSize = dimensionsBuffer.Length * sizeof(float);
            var bufferHandle = _renderer.BufferManager.CreateWithHandle(_renderer, rangeSize, false);
            _renderer.BufferManager.SetData(bufferHandle, 0, dimensionsBuffer);

            ReadOnlySpan<float> sharpeningBuffer = stackalloc float[] { Level };
            var sharpeningBufferHandle = _renderer.BufferManager.CreateWithHandle(_renderer, sizeof(float), false);
            _renderer.BufferManager.SetData(sharpeningBufferHandle, 0, sharpeningBuffer);

            int threadGroupWorkRegionDim = 16;
            int dispatchX = (width + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchY = (height + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;

            var bufferRanges = new BufferRange(bufferHandle, 0, rangeSize);
            _scalingPipeline.SetUniformBuffers(stackalloc[] { new BufferAssignment(2, bufferRanges) });
            _scalingPipeline.SetScissors(scissors);
            _scalingPipeline.SetViewports(viewports, false);
            _scalingPipeline.SetImage(0, _intermediaryTexture, GAL.Format.R8G8B8A8Unorm);
            _scalingPipeline.DispatchCompute(dispatchX, dispatchY, 1);
            _scalingPipeline.ComputeBarrier();

            viewports[0] = new GAL.Viewport(
                new Rectangle<float>(0, 0, width, height),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            scissors[0] = new Rectangle<int>(0, 0, width, height);

            // Sharpening pass
            _sharpeningPipeline.SetCommandBuffer(cbs);
            _sharpeningPipeline.SetProgram(_sharpeningProgram);
            _sharpeningPipeline.SetTextureAndSampler(ShaderStage.Compute, 1, _intermediaryTexture, _sampler);
            _sharpeningPipeline.SetUniformBuffers(stackalloc[] { new BufferAssignment(2, bufferRanges) });
            var sharpeningRange = new BufferRange(sharpeningBufferHandle, 0, sizeof(float));
            _sharpeningPipeline.SetUniformBuffers(stackalloc[] { new BufferAssignment(4, sharpeningRange) });
            _sharpeningPipeline.SetScissors(scissors);
            _sharpeningPipeline.SetViewports(viewports, false);
            _sharpeningPipeline.SetImage(0, destinationTexture);
            _sharpeningPipeline.DispatchCompute(dispatchX, dispatchY, 1);
            _sharpeningPipeline.ComputeBarrier();

            _renderer.BufferManager.Delete(bufferHandle);
            _renderer.BufferManager.Delete(sharpeningBufferHandle);
        }
    }
}