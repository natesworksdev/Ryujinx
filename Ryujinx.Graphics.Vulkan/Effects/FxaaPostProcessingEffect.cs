using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Silk.NET.Vulkan;
using System;
using System.IO;

namespace Ryujinx.Graphics.Vulkan.Effects
{
    internal partial class FxaaPostProcessingEffect : IPostProcessingEffect
    {
        private readonly VulkanRenderer _renderer;
        private ISampler _samplerLinear;
        private ShaderCollection _shaderProgram;

        private PipelineHelperShader _pipeline;
        private TextureView _texture;

        public FxaaPostProcessingEffect(VulkanRenderer renderer, Device device)
        {
            _renderer = renderer;
            _pipeline = new PipelineHelperShader(renderer, device);

            Initialize();
        }

        public void Dispose()
        {
            _shaderProgram.Dispose();
            _pipeline.Dispose();
            _samplerLinear.Dispose();
            _texture?.Dispose();
        }

        private void Initialize()
        {
            _pipeline.Initialize();

            var shader = EmbeddedResources.Read("Ryujinx.Graphics.Vulkan/Shaders/fxaa.spirv");

            var computeBindings = new ShaderBindings(
                new[] { 2 },
                Array.Empty<int>(),
                new[] { 1 },
                new[] { 0 });

            _samplerLinear = _renderer.CreateSampler(GAL.SamplerCreateInfo.Create(MinFilter.Linear, MagFilter.Linear));

            _shaderProgram = _renderer.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(shader, computeBindings, ShaderStage.Compute, TargetLanguage.Spirv)
            });
        }

        public TextureView Run(TextureView view, CommandBufferScoped cbs, int width, int height)
        {
            if (_texture == null || _texture.Width != view.Width || _texture.Height != view.Height)
            {
                _texture?.Dispose();
                _texture = _renderer.CreateTexture(view.Info, view.ScaleFactor) as TextureView;
            }

            _pipeline.SetCommandBuffer(cbs);
            _pipeline.SetProgram(_shaderProgram);
            _pipeline.SetTextureAndSampler(ShaderStage.Compute, 1, view, _samplerLinear);

            ReadOnlySpan<float> resolutionBuffer = stackalloc float[] { view.Width, view.Height };
            int rangeSize = resolutionBuffer.Length * sizeof(float);
            var bufferHandle = _renderer.BufferManager.CreateWithHandle(_renderer, rangeSize, false);

            _renderer.BufferManager.SetData(bufferHandle, 0, resolutionBuffer);

            var bufferRanges = new BufferRange(bufferHandle, 0, rangeSize);
            _pipeline.SetUniformBuffers(stackalloc[] { new BufferAssignment(2, bufferRanges) });

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];

            viewports[0] = new GAL.Viewport(
                new Rectangle<float>(0, 0, view.Width, view.Height),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            var dispatchX = BitUtils.DivRoundUp(view.Width, IPostProcessingEffect.LocalGroupSize);
            var dispatchY = BitUtils.DivRoundUp(view.Height, IPostProcessingEffect.LocalGroupSize);

            scissors[0] = new Rectangle<int>(0, 0, view.Width, view.Height);
            _pipeline.SetScissors(scissors);
            _pipeline.SetViewports(viewports, false);

            _pipeline.SetImage(0, _texture, GAL.Format.R8G8B8A8Unorm);
            _pipeline.DispatchCompute(dispatchX, dispatchY, 1);

            _renderer.BufferManager.Delete(bufferHandle);
            _pipeline.ComputeBarrier();

            return _texture;
        }
    }
}