using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Silk.NET.Vulkan;
using System;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class TextureBlit : IDisposable
    {
        private const string VertexShaderSource = @"#version 450 core

layout (std140, binding = 0) uniform tex_coord_in
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

        private const string FragmentShaderSource = @"#version 450 core

layout (binding = 0, set = 2) uniform sampler2D tex;

layout (location = 0) in vec2 tex_coord;
layout (location = 0) out vec4 colour;

void main()
{
    colour = texture(tex, tex_coord);
}";

        private const string FragmentShaderSourceClearAlpha = @"#version 450 core

layout (binding = 0, set = 2) uniform sampler2D tex;

layout (location = 0) in vec2 tex_coord;
layout (location = 0) out vec4 colour;

void main()
{
    colour = vec4(texture(tex, tex_coord).rgb, 1.0f);
}";

        private readonly PipelineBlit _pipeline;
        private readonly ISampler _samplerLinear;
        private readonly ISampler _samplerNearest;
        private readonly IProgram _program;
        private readonly IProgram _programClearAlpha;

        public TextureBlit(VulkanGraphicsDevice gd, Device device)
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
                new[] { 0 },
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>());

            var fragmentBindings = new ShaderBindings(
                Array.Empty<int>(),
                Array.Empty<int>(),
                new[] { 0 },
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>());

            var vertexShader = gd.CompileShader(ShaderStage.Vertex, vertexBindings, VertexShaderSource);
            var fragmentShader = gd.CompileShader(ShaderStage.Fragment, fragmentBindings, FragmentShaderSource);
            var fragmentShaderClearAlpha = gd.CompileShader(ShaderStage.Fragment, fragmentBindings, FragmentShaderSourceClearAlpha);

            _program = gd.CreateProgram(new[] { vertexShader, fragmentShader }, null);
            _programClearAlpha = gd.CreateProgram(new[] { vertexShader, fragmentShaderClearAlpha }, null);
        }

        public void BlitFast(
            VulkanGraphicsDevice gd,
            CommandBufferScoped cbs,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            int srcLevel,
            int dstLevel,
            int srcLayer,
            int dstLayer,
            int srcX0,
            int srcY0,
            int srcX1,
            int srcY1,
            int dstX0,
            int dstY0,
            int dstX1,
            int dstY1,
            bool linearFilter)
        {
            static (Offset3D, Offset3D) ExtentsToOffset3D(int width, int height, int x0, int y0, int x1, int y1)
            {
                static int Clamp(int value, int max)
                {
                    return Math.Clamp(value, 0, max);
                }

                var xy1 = new Offset3D(Clamp(x0, width), Clamp(y0, height), 0);
                var xy2 = new Offset3D(Clamp(x1, width), Clamp(y1, height), 1);

                return (xy1, xy2);
            }

            var srcSl = new ImageSubresourceLayers(srcInfo.Format.ConvertAspectFlags(), (uint)srcLevel, (uint)srcLayer, 1);
            var dstSl = new ImageSubresourceLayers(dstInfo.Format.ConvertAspectFlags(), (uint)dstLevel, (uint)dstLayer, 1);

            var srcOffs = ExtentsToOffset3D(srcInfo.Width, srcInfo.Height, srcX0, srcY0, srcX1, srcY1);
            var dstOffs = ExtentsToOffset3D(dstInfo.Width, dstInfo.Height, dstX0, dstY0, dstX1, dstY1);

            var srcOffsets = new ImageBlit.SrcOffsetsBuffer()
            {
                Element0 = srcOffs.Item1,
                Element1 = srcOffs.Item2
            };

            var dstOffsets = new ImageBlit.DstOffsetsBuffer()
            {
                Element0 = srcOffs.Item1,
                Element1 = srcOffs.Item2
            };

            var region = new ImageBlit()
            {
                SrcSubresource = srcSl,
                SrcOffsets = srcOffsets,
                DstSubresource = dstSl,
                DstOffsets = dstOffsets
            };

            var filter = linearFilter ? Filter.Linear : Filter.Nearest;

            gd.Api.CmdBlitImage(cbs.CommandBuffer, srcImage, ImageLayout.General, dstImage, ImageLayout.General, 1, region, filter);
        }

        public void Blit(
            VulkanGraphicsDevice gd,
            CommandBufferScoped cbs,
            TextureView src,
            Auto<DisposableImageView> dst,
            int dstWidth,
            int dstHeight,
            VkFormat dstFormat,
            int srcX0,
            int srcY0,
            int srcX1,
            int srcY1,
            int dstX0,
            int dstY0,
            int dstX1,
            int dstY1,
            bool linearFilter,
            bool clearAlpha = false)
        {
            _pipeline.SetCommandBuffer(cbs);

            const int RegionBufferSize = 16;

            var sampler = linearFilter ? _samplerLinear : _samplerNearest;

            _pipeline.SetTextureAndSampler(0, src, sampler);

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = (float)srcX0 / src.Width;
            region[1] = (float)srcX1 / src.Width;
            region[2] = (float)srcY0 / src.Height;
            region[3] = (float)srcY1 / src.Height;

            var bufferHandle = gd.BufferManager.CreateWithHandle(gd, RegionBufferSize);

            gd.BufferManager.SetData<float>(bufferHandle, 0, region);

            Span<BufferRange> bufferRanges = stackalloc BufferRange[1];

            bufferRanges[0] = new BufferRange(bufferHandle, 0, RegionBufferSize);

            _pipeline.SetUniformBuffers(bufferRanges);

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];

            viewports[0] = new GAL.Viewport(
                new Rectangle<float>(dstX0, dstY0, dstX1 - dstX0, dstY1 - dstY0),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            scissors[0] = new Rectangle<int>(0, 0, dstWidth, dstHeight);

            _pipeline.SetRenderTarget(dst, (uint)dstWidth, (uint)dstHeight, dstFormat);
            _pipeline.SetRenderTargetColorMasks(new uint[] { 0xf });
            _pipeline.SetViewports(0, viewports);
            _pipeline.SetScissors(scissors);
            _pipeline.SetProgram(clearAlpha ? _programClearAlpha : _program);
            _pipeline.SetPrimitiveTopology(GAL.PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0);
            _pipeline.Finish();

            gd.BufferManager.Delete(bufferHandle);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _programClearAlpha.Dispose();
                _program.Dispose();
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
