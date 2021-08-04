using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Silk.NET.Vulkan;
using System;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class HelperShader : IDisposable
    {
        private const string VertexShaderSource = @"#version 450 core

layout (std140, binding = 1) uniform tex_coord_in
{
    vec4 tex_coord_in_data;
};

layout (location = 0) out vec2 tex_coord;

void main()
{
    int low = gl_VertexIndex & 1;
	int high = gl_VertexIndex >> 1;
	tex_coord.x = tex_coord_in_data[low];
	tex_coord.y = tex_coord_in_data[2 + high];
    gl_Position.x = (float(low) - 0.5f) * 2.0f;
    gl_Position.y = (float(high) - 0.5f) * 2.0f;
    gl_Position.z = 0.0f;
    gl_Position.w = 1.0f;
}";

        private const string ColorBlitFragmentShaderSource = @"#version 450 core

layout (binding = 32, set = 2) uniform sampler2D tex;

layout (location = 0) in vec2 tex_coord;
layout (location = 0) out vec4 colour;

void main()
{
    colour = texture(tex, tex_coord);
}";

        private const string ClearAlphaFragmentShaderSource = @"#version 450 core

layout (binding = 32, set = 2) uniform sampler2D tex;

layout (location = 0) in vec2 tex_coord;
layout (location = 0) out vec4 colour;

void main()
{
    colour = vec4(texture(tex, tex_coord).rgb, 1.0f);
}";

        private readonly PipelineBlit _pipeline;
        private readonly ISampler _samplerLinear;
        private readonly ISampler _samplerNearest;
        private readonly IProgram _programColorBlit;
        private readonly IProgram _programClearAlpha;

        public HelperShader(VulkanGraphicsDevice gd, Device device)
        {
            _pipeline = new PipelineBlit(gd, device);

            static GAL.SamplerCreateInfo GetSamplerCreateInfo(MinFilter minFilter, MagFilter magFilter)
            {
                return new GAL.SamplerCreateInfo(
                    minFilter,
                    magFilter,
                    false,
                    AddressMode.ClampToEdge,
                    AddressMode.ClampToEdge,
                    AddressMode.ClampToEdge,
                    CompareMode.None,
                    GAL.CompareOp.Always,
                    new ColorF(0f, 0f, 0f, 0f),
                    0f,
                    0f,
                    0f,
                    1f);
            }

            _samplerLinear = gd.CreateSampler(GetSamplerCreateInfo(MinFilter.Linear, MagFilter.Linear));
            _samplerNearest = gd.CreateSampler(GetSamplerCreateInfo(MinFilter.Nearest, MagFilter.Nearest));

            var vertexBindings = new ShaderBindings(
                new[] { 1 },
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>());

            var fragmentBindings = new ShaderBindings(
                Array.Empty<int>(),
                Array.Empty<int>(),
                new[] { 32 },
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>());

            var vertexShader = gd.CompileShader(ShaderStage.Vertex, vertexBindings, VertexShaderSource);
            var fragmentShaderColorBlit = gd.CompileShader(ShaderStage.Fragment, fragmentBindings, ColorBlitFragmentShaderSource);
            var fragmentShaderClearAlpha = gd.CompileShader(ShaderStage.Fragment, fragmentBindings, ClearAlphaFragmentShaderSource);

            _programColorBlit = gd.CreateProgram(new[] { vertexShader, fragmentShaderColorBlit }, null);
            _programClearAlpha = gd.CreateProgram(new[] { vertexShader, fragmentShaderClearAlpha }, null);
        }

        public void Blit(
            VulkanGraphicsDevice gd,
            TextureView src,
            Auto<DisposableImageView> dst,
            int dstWidth,
            int dstHeight,
            VkFormat dstFormat,
            Extents2D srcRegion,
            Extents2D dstRegion,
            bool linearFilter,
            bool clearAlpha = false)
        {
            gd.FlushAllCommands();

            using var cbs = gd.CommandBufferPool.Rent();

            Blit(gd, cbs, src, dst, dstWidth, dstHeight, dstFormat, srcRegion, dstRegion, linearFilter, clearAlpha);
        }

        public void Blit(
            VulkanGraphicsDevice gd,
            CommandBufferScoped cbs,
            TextureView src,
            Auto<DisposableImageView> dst,
            int dstWidth,
            int dstHeight,
            VkFormat dstFormat,
            Extents2D srcRegion,
            Extents2D dstRegion,
            bool linearFilter,
            bool clearAlpha = false)
        {
            _pipeline.SetCommandBuffer(cbs);

            const int RegionBufferSize = 16;

            var sampler = linearFilter ? _samplerLinear : _samplerNearest;

            _pipeline.SetTextureAndSampler(32, src, sampler);

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = (float)srcRegion.X1 / src.Width;
            region[1] = (float)srcRegion.X2 / src.Width;
            region[2] = (float)srcRegion.Y1 / src.Height;
            region[3] = (float)srcRegion.Y2 / src.Height;

            var bufferHandle = gd.BufferManager.CreateWithHandle(gd, RegionBufferSize);

            gd.BufferManager.SetData<float>(bufferHandle, 0, region);

            Span<BufferRange> bufferRanges = stackalloc BufferRange[1];

            bufferRanges[0] = new BufferRange(bufferHandle, 0, RegionBufferSize);

            _pipeline.SetUniformBuffers(1, bufferRanges);

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];

            viewports[0] = new GAL.Viewport(
                new Rectangle<float>(dstRegion.X1, dstRegion.Y1, dstRegion.X2 - dstRegion.X1, dstRegion.Y2 - dstRegion.Y1),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            scissors[0] = new Rectangle<int>(0, 0, dstWidth, dstHeight);

            _pipeline.SetProgram(clearAlpha ? _programClearAlpha : _programColorBlit);
            _pipeline.SetRenderTarget(dst, (uint)dstWidth, (uint)dstHeight, false, dstFormat);
            _pipeline.SetRenderTargetColorMasks(new uint[] { 0xf });

            if (clearAlpha)
            {
                _pipeline.ClearRenderTargetColor(0, 0xf, new ColorF(0f, 0f, 0f, 1f));
            }

            _pipeline.SetViewports(0, viewports);
            _pipeline.SetScissors(scissors);
            _pipeline.SetPrimitiveTopology(GAL.PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0);
            _pipeline.Finish();

            gd.BufferManager.Delete(bufferHandle);
        }

        public unsafe void ConvertI8ToI16(VulkanGraphicsDevice gd, CommandBufferScoped cbs, BufferHolder src, BufferHolder dst, int srcOffset, int size)
        {
            // TODO: Do this with a compute shader?
            var srcBuffer = src.GetBuffer().Get(cbs, srcOffset, size).Value;
            var dstBuffer = dst.GetBuffer().Get(cbs, 0, size * 2).Value;

            gd.Api.CmdFillBuffer(cbs.CommandBuffer, dstBuffer, 0, Vk.WholeSize, 0);

            var bufferCopy = new BufferCopy[size];

            for (ulong i = 0; i < (ulong)size; i++)
            {
                bufferCopy[i] = new BufferCopy((ulong)srcOffset + i, i * 2, 1);
            }

            BufferHolder.InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                BufferHolder.DefaultAccessFlags,
                AccessFlags.AccessTransferWriteBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                PipelineStageFlags.PipelineStageTransferBit,
                0,
                size * 2);

            fixed (BufferCopy* pBufferCopy = bufferCopy)
            {
                gd.Api.CmdCopyBuffer(cbs.CommandBuffer, srcBuffer, dstBuffer, (uint)size, pBufferCopy);
            }

            BufferHolder.InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                AccessFlags.AccessTransferWriteBit,
                BufferHolder.DefaultAccessFlags,
                PipelineStageFlags.PipelineStageTransferBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                0,
                size * 2);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _programClearAlpha.Dispose();
                _programColorBlit.Dispose();
                _samplerNearest.Dispose();
                _samplerLinear.Dispose();
                _pipeline.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
