using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Ryujinx.Graphics.Vulkan.Shaders;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class HelperShader : IDisposable
    {
        private readonly PipelineHelperShader _pipeline;
        private readonly ISampler _samplerLinear;
        private readonly ISampler _samplerNearest;
        private readonly IProgram _programColorBlit;
        private readonly IProgram _programColorBlitClearAlpha;
        private readonly IProgram _programColorClear;
        private readonly IProgram _programStrideChange;
        private readonly IProgram _programColorCopyMSToNonMSR16;
        private readonly IProgram _programColorCopyMSToNonMSR32;
        private readonly IProgram _programColorCopyMSToNonMSR8;
        private readonly IProgram _programColorCopyMSToNonMSRG32;
        private readonly IProgram _programColorCopyMSToNonMSRGBA32;
        private readonly IProgram _programColorCopyNonMSToMSR16;
        private readonly IProgram _programColorCopyNonMSToMSR32;
        private readonly IProgram _programColorCopyNonMSToMSR8;
        private readonly IProgram _programColorCopyNonMSToMSRG32;
        private readonly IProgram _programColorCopyNonMSToMSRGBA32;

        public HelperShader(VulkanRenderer gd, Device device)
        {
            _pipeline = new PipelineHelperShader(gd, device);
            _pipeline.Initialize();

            _samplerLinear = gd.CreateSampler(GAL.SamplerCreateInfo.Create(MinFilter.Linear, MagFilter.Linear));
            _samplerNearest = gd.CreateSampler(GAL.SamplerCreateInfo.Create(MinFilter.Nearest, MagFilter.Nearest));

            var vertexBindings = new ShaderBindings(
                new[] { 1 },
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>());

            var fragmentBindings = new ShaderBindings(
                Array.Empty<int>(),
                Array.Empty<int>(),
                new[] { 0 },
                Array.Empty<int>());

            _programColorBlit = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorBlitVertexShaderSource, vertexBindings, ShaderStage.Vertex, TargetLanguage.Spirv),
                new ShaderSource(ShaderBinaries.ColorBlitFragmentShaderSource, fragmentBindings, ShaderStage.Fragment, TargetLanguage.Spirv),
            });

            _programColorBlitClearAlpha = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorBlitVertexShaderSource, vertexBindings, ShaderStage.Vertex, TargetLanguage.Spirv),
                new ShaderSource(ShaderBinaries.ColorBlitClearAlphaFragmentShaderSource, fragmentBindings, ShaderStage.Fragment, TargetLanguage.Spirv),
            });

            var fragmentBindings2 = new ShaderBindings(
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>());

            _programColorClear = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorClearVertexShaderSource, vertexBindings, ShaderStage.Vertex, TargetLanguage.Spirv),
                new ShaderSource(ShaderBinaries.ColorClearFragmentShaderSource, fragmentBindings2, ShaderStage.Fragment, TargetLanguage.Spirv),
            });

            var strideChangeBindings = new ShaderBindings(
                new[] { 0 },
                new[] { 1, 2 },
                Array.Empty<int>(),
                Array.Empty<int>());

            _programStrideChange = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ChangeBufferStrideShaderSource, strideChangeBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            var colorCopyMSBindings = new ShaderBindings(
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<int>(),
                new[] { 0, 1 });

            _programColorCopyMSToNonMSR16 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyMSToNonMSR16ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyMSToNonMSR32 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyMSToNonMSR32ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyMSToNonMSR8 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyMSToNonMSR8ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyMSToNonMSRG32 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyMSToNonMSRG32ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyMSToNonMSRGBA32 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyMSToNonMSRGBA32ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyNonMSToMSR16 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyNonMSToMSR16ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyNonMSToMSR32 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyNonMSToMSR32ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyNonMSToMSR8 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyNonMSToMSR8ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyNonMSToMSRG32 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyNonMSToMSRG32ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });

            _programColorCopyNonMSToMSRGBA32 = gd.CreateProgramWithMinimalLayout(new[]
            {
                new ShaderSource(ShaderBinaries.ColorCopyNonMSToMSRGBA32ShaderSource, colorCopyMSBindings, ShaderStage.Compute, TargetLanguage.Spirv),
            });
        }

        public void Blit(
            VulkanRenderer gd,
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
            VulkanRenderer gd,
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

            _pipeline.SetTextureAndSampler(ShaderStage.Fragment, 0, src, sampler);

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = (float)srcRegion.X1 / src.Width;
            region[1] = (float)srcRegion.X2 / src.Width;
            region[2] = (float)srcRegion.Y1 / src.Height;
            region[3] = (float)srcRegion.Y2 / src.Height;

            if (dstRegion.X1 > dstRegion.X2)
            {
                (region[0], region[1]) = (region[1], region[0]);
            }

            if (dstRegion.Y1 > dstRegion.Y2)
            {
                (region[2], region[3]) = (region[3], region[2]);
            }

            var bufferHandle = gd.BufferManager.CreateWithHandle(gd, RegionBufferSize, false);

            gd.BufferManager.SetData<float>(bufferHandle, 0, region);

            Span<BufferRange> bufferRanges = stackalloc BufferRange[1];

            bufferRanges[0] = new BufferRange(bufferHandle, 0, RegionBufferSize);

            _pipeline.SetUniformBuffers(1, bufferRanges);

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];

            var rect = new Rectangle<float>(
                MathF.Min(dstRegion.X1, dstRegion.X2),
                MathF.Min(dstRegion.Y1, dstRegion.Y2),
                MathF.Abs(dstRegion.X2 - dstRegion.X1),
                MathF.Abs(dstRegion.Y2 - dstRegion.Y1));

            viewports[0] = new GAL.Viewport(
                rect,
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            scissors[0] = new Rectangle<int>(0, 0, dstWidth, dstHeight);

            _pipeline.SetProgram(clearAlpha ? _programColorBlitClearAlpha : _programColorBlit);
            _pipeline.SetRenderTarget(dst, (uint)dstWidth, (uint)dstHeight, false, dstFormat);
            _pipeline.SetRenderTargetColorMasks(new uint[] { 0xf });
            _pipeline.SetScissors(scissors);

            if (clearAlpha)
            {
                _pipeline.ClearRenderTargetColor(0, 0, 1, new ColorF(0f, 0f, 0f, 1f));
            }

            _pipeline.SetViewports(viewports, false);
            _pipeline.SetPrimitiveTopology(GAL.PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0);
            _pipeline.Finish(gd, cbs);

            gd.BufferManager.Delete(bufferHandle);
        }

        public void Clear(
            VulkanRenderer gd,
            Auto<DisposableImageView> dst,
            ReadOnlySpan<float> clearColor,
            uint componentMask,
            int dstWidth,
            int dstHeight,
            VkFormat dstFormat,
            Rectangle<int> scissor)
        {
            const int ClearColorBufferSize = 16;

            gd.FlushAllCommands();

            using var cbs = gd.CommandBufferPool.Rent();

            _pipeline.SetCommandBuffer(cbs);

            var bufferHandle = gd.BufferManager.CreateWithHandle(gd, ClearColorBufferSize, false);

            gd.BufferManager.SetData<float>(bufferHandle, 0, clearColor);

            Span<BufferRange> bufferRanges = stackalloc BufferRange[1];

            bufferRanges[0] = new BufferRange(bufferHandle, 0, ClearColorBufferSize);

            _pipeline.SetUniformBuffers(1, bufferRanges);

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];

            viewports[0] = new GAL.Viewport(
                new Rectangle<float>(0, 0, dstWidth, dstHeight),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            scissors[0] = scissor;

            _pipeline.SetProgram(_programColorClear);
            _pipeline.SetRenderTarget(dst, (uint)dstWidth, (uint)dstHeight, false, dstFormat);
            _pipeline.SetRenderTargetColorMasks(new uint[] { componentMask });
            _pipeline.SetViewports(viewports, false);
            _pipeline.SetScissors(scissors);
            _pipeline.SetPrimitiveTopology(GAL.PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0);
            _pipeline.Finish();

            gd.BufferManager.Delete(bufferHandle);
        }

        public void DrawTexture(
            VulkanRenderer gd,
            PipelineBase pipeline,
            TextureView src,
            ISampler srcSampler,
            Extents2DF srcRegion,
            Extents2DF dstRegion)
        {
            const int RegionBufferSize = 16;

            pipeline.SetTextureAndSampler(ShaderStage.Fragment, 0, src, srcSampler);

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = srcRegion.X1 / src.Width;
            region[1] = srcRegion.X2 / src.Width;
            region[2] = srcRegion.Y1 / src.Height;
            region[3] = srcRegion.Y2 / src.Height;

            if (dstRegion.X1 > dstRegion.X2)
            {
                (region[0], region[1]) = (region[1], region[0]);
            }

            if (dstRegion.Y1 > dstRegion.Y2)
            {
                (region[2], region[3]) = (region[3], region[2]);
            }

            var bufferHandle = gd.BufferManager.CreateWithHandle(gd, RegionBufferSize, false);

            gd.BufferManager.SetData<float>(bufferHandle, 0, region);

            Span<BufferRange> bufferRanges = stackalloc BufferRange[1];

            bufferRanges[0] = new BufferRange(bufferHandle, 0, RegionBufferSize);

            pipeline.SetUniformBuffers(1, bufferRanges);

            Span<GAL.Viewport> viewports = stackalloc GAL.Viewport[1];

            var rect = new Rectangle<float>(
                MathF.Min(dstRegion.X1, dstRegion.X2),
                MathF.Min(dstRegion.Y1, dstRegion.Y2),
                MathF.Abs(dstRegion.X2 - dstRegion.X1),
                MathF.Abs(dstRegion.Y2 - dstRegion.Y1));

            viewports[0] = new GAL.Viewport(
                rect,
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[1];

            pipeline.SetProgram(_programColorBlit);
            pipeline.SetViewports(viewports, false);
            pipeline.SetPrimitiveTopology(GAL.PrimitiveTopology.TriangleStrip);
            pipeline.Draw(4, 1, 0, 0);

            gd.BufferManager.Delete(bufferHandle);
        }

        public unsafe void ConvertI8ToI16(VulkanRenderer gd, CommandBufferScoped cbs, BufferHolder src, BufferHolder dst, int srcOffset, int size)
        {
            ChangeStride(gd, cbs, src, dst, srcOffset, size, 1, 2);
        }

        public unsafe void ChangeStride(VulkanRenderer gd, CommandBufferScoped cbs, BufferHolder src, BufferHolder dst, int srcOffset, int size, int stride, int newStride)
        {
            bool supportsUint8 = gd.Capabilities.SupportsShaderInt8;

            int elems = size / stride;
            int newSize = elems * newStride;

            var srcBufferAuto = src.GetBuffer();
            var dstBufferAuto = dst.GetBuffer();

            var srcBuffer = srcBufferAuto.Get(cbs, srcOffset, size).Value;
            var dstBuffer = dstBufferAuto.Get(cbs, 0, newSize).Value;

            var access = supportsUint8 ? AccessFlags.AccessShaderWriteBit : AccessFlags.AccessTransferWriteBit;
            var stage = supportsUint8 ? PipelineStageFlags.PipelineStageComputeShaderBit : PipelineStageFlags.PipelineStageTransferBit;

            BufferHolder.InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                BufferHolder.DefaultAccessFlags,
                access,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                stage,
                0,
                newSize);

            if (supportsUint8)
            {
                const int ParamsBufferSize = 16;

                Span<int> shaderParams = stackalloc int[ParamsBufferSize / sizeof(int)];

                shaderParams[0] = stride;
                shaderParams[1] = newStride;
                shaderParams[2] = size;
                shaderParams[3] = srcOffset;

                var bufferHandle = gd.BufferManager.CreateWithHandle(gd, ParamsBufferSize, false);

                gd.BufferManager.SetData<int>(bufferHandle, 0, shaderParams);

                _pipeline.SetCommandBuffer(cbs);

                Span<BufferRange> cbRanges = stackalloc BufferRange[1];

                cbRanges[0] = new BufferRange(bufferHandle, 0, ParamsBufferSize);

                _pipeline.SetUniformBuffers(0, cbRanges);

                Span<Auto<DisposableBuffer>> sbRanges = new Auto<DisposableBuffer>[2];

                sbRanges[0] = srcBufferAuto;
                sbRanges[1] = dstBufferAuto;

                _pipeline.SetStorageBuffers(1, sbRanges);

                _pipeline.SetProgram(_programStrideChange);
                _pipeline.DispatchCompute(1, 1, 1);

                gd.BufferManager.Delete(bufferHandle);

                _pipeline.Finish(gd, cbs);
            }
            else
            {
                gd.Api.CmdFillBuffer(cbs.CommandBuffer, dstBuffer, 0, Vk.WholeSize, 0);

                var bufferCopy = new BufferCopy[elems];

                for (ulong i = 0; i < (ulong)elems; i++)
                {
                    bufferCopy[i] = new BufferCopy((ulong)srcOffset + i * (ulong)stride, i * (ulong)newStride, (ulong)stride);
                }

                fixed (BufferCopy* pBufferCopy = bufferCopy)
                {
                    gd.Api.CmdCopyBuffer(cbs.CommandBuffer, srcBuffer, dstBuffer, (uint)elems, pBufferCopy);
                }
            }

            BufferHolder.InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                access,
                BufferHolder.DefaultAccessFlags,
                stage,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                0,
                newSize);
        }

        public unsafe void ConvertIndexBuffer(VulkanRenderer gd,
            CommandBufferScoped cbs,
            BufferHolder src,
            BufferHolder dst,
            IndexBufferPattern pattern,
            int indexSize,
            int srcOffset,
            int indexCount)
        {
            int convertedCount = pattern.GetConvertedCount(indexCount);
            int outputIndexSize = 4;

            // TODO: Do this with a compute shader?
            var srcBuffer = src.GetBuffer().Get(cbs, srcOffset, indexCount * indexSize).Value;
            var dstBuffer = dst.GetBuffer().Get(cbs, 0, convertedCount * outputIndexSize).Value;

            gd.Api.CmdFillBuffer(cbs.CommandBuffer, dstBuffer, 0, Vk.WholeSize, 0);

            var bufferCopy = new List<BufferCopy>();
            int outputOffset = 0;

            // Try to merge copies of adjacent indices to reduce copy count.
            int sequenceStart = 0;
            int sequenceLength = 0;

            foreach (var index in pattern.GetIndexMapping(indexCount))
            {
                if (sequenceLength > 0)
                {
                    if (index == sequenceStart + sequenceLength && indexSize == outputIndexSize)
                    {
                        sequenceLength++;
                        continue;
                    }

                    // Commit the copy so far.
                    bufferCopy.Add(new BufferCopy((ulong)(srcOffset + sequenceStart * indexSize), (ulong)outputOffset, (ulong)(indexSize * sequenceLength)));
                    outputOffset += outputIndexSize * sequenceLength;
                }

                sequenceStart = index;
                sequenceLength = 1;
            }

            if (sequenceLength > 0)
            {
                // Commit final pending copy.
                bufferCopy.Add(new BufferCopy((ulong)(srcOffset + sequenceStart * indexSize), (ulong)outputOffset, (ulong)(indexSize * sequenceLength)));
            }

            var bufferCopyArray = bufferCopy.ToArray();

            BufferHolder.InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                BufferHolder.DefaultAccessFlags,
                AccessFlags.AccessTransferWriteBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                PipelineStageFlags.PipelineStageTransferBit,
                0,
                convertedCount * outputIndexSize);

            fixed (BufferCopy* pBufferCopy = bufferCopyArray)
            {
                gd.Api.CmdCopyBuffer(cbs.CommandBuffer, srcBuffer, dstBuffer, (uint)bufferCopyArray.Length, pBufferCopy);
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
                convertedCount * outputIndexSize);
        }

        public void CopyMSToNonMS(VulkanRenderer gd, CommandBufferScoped cbs, TextureView src, TextureView dst, int srcLayer, int dstLayer, int depth)
        {
            CopyMS(gd, cbs, src, dst, srcLayer, dstLayer, depth, dst.Info.Width, dst.Info.Height, GetMSToNonMSShader(src.Info.BytesPerPixel));
        }

        public void CopyNonMSToMS(VulkanRenderer gd, CommandBufferScoped cbs, TextureView src, TextureView dst, int srcLayer, int dstLayer, int depth)
        {
            CopyMS(gd, cbs, src, dst, srcLayer, dstLayer, depth, src.Info.Width, src.Info.Height, GetNonMSToMSShader(src.Info.BytesPerPixel));
        }

        private void CopyMS(
            VulkanRenderer gd,
            CommandBufferScoped cbs,
            TextureView src,
            TextureView dst,
            int srcLayer,
            int dstLayer,
            int depth,
            int nonMSWidth,
            int nonMSHeight,
            IProgram program)
        {
            TextureView.InsertImageBarrier(
                gd.Api,
                cbs.CommandBuffer,
                src.GetImage().Get(cbs).Value,
                TextureStorage.DefaultAccessMask,
                AccessFlags.AccessShaderReadBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                PipelineStageFlags.PipelineStageComputeShaderBit,
                ImageAspectFlags.ImageAspectColorBit,
                src.FirstLayer + srcLayer,
                src.FirstLevel,
                depth,
                1);

            _pipeline.SetCommandBuffer(cbs);

            _pipeline.SetProgram(program);

            var format = GetFormat(src.Info.BytesPerPixel);

            int dispatchX = (nonMSWidth + 31) / 32;
            int dispatchY = (nonMSHeight + 31) / 32;

            if (src.Info.Target == Target.Texture2DMultisampleArray ||
                dst.Info.Target == Target.Texture2DMultisampleArray)
            {
                for (int z = 0; z < depth; z++)
                {
                    var srcView = Create2DLayerView(src, srcLayer + z);
                    var dstView = Create2DLayerView(dst, dstLayer + z);

                    _pipeline.SetImage(0, srcView, format);
                    _pipeline.SetImage(1, dstView, format);

                    _pipeline.DispatchCompute(dispatchX, dispatchY, 1);

                    srcView.Release();
                    dstView.Release();
                }
            }
            else
            {
                _pipeline.SetImage(0, src, format);
                _pipeline.SetImage(1, dst, format);

                _pipeline.DispatchCompute(dispatchX, dispatchY, 1);
            }

            _pipeline.Finish(gd, cbs);

            TextureView.InsertImageBarrier(
                gd.Api,
                cbs.CommandBuffer,
                dst.GetImage().Get(cbs).Value,
                AccessFlags.AccessShaderWriteBit,
                TextureStorage.DefaultAccessMask,
                PipelineStageFlags.PipelineStageComputeShaderBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                ImageAspectFlags.ImageAspectColorBit,
                dst.FirstLayer + dstLayer,
                dst.FirstLevel,
                depth,
                1);
        }

        private static ITexture Create2DLayerView(TextureView from, int layer)
        {
            var target = from.Info.Target switch
            {
                Target.Texture1DArray => Target.Texture1D,
                Target.Texture2DArray => Target.Texture2D,
                Target.Texture2DMultisampleArray => Target.Texture2DMultisample,
                _ => from.Info.Target
            };

            var info = new TextureCreateInfo(
                from.Info.Width,
                from.Info.Height,
                from.Info.Depth,
                1,
                from.Info.Samples,
                from.Info.BlockWidth,
                from.Info.BlockHeight,
                from.Info.BytesPerPixel,
                from.Info.Format,
                from.Info.DepthStencilMode,
                target,
                from.Info.SwizzleR,
                from.Info.SwizzleG,
                from.Info.SwizzleB,
                from.Info.SwizzleA);

            return from.CreateView(from.Info, layer, 0);
        }

        private IProgram GetMSToNonMSShader(int bytesPerPixel)
        {
            return bytesPerPixel switch
            {
                1 => _programColorCopyMSToNonMSR8,
                2 => _programColorCopyMSToNonMSR16,
                4 => _programColorCopyMSToNonMSR32,
                8 => _programColorCopyMSToNonMSRG32,
                16 => _programColorCopyMSToNonMSRGBA32,
                _ => throw new ArgumentException($"Invalid bytes per pixel {bytesPerPixel}.")
            };
        }

        private IProgram GetNonMSToMSShader(int bytesPerPixel)
        {
            return bytesPerPixel switch
            {
                1 => _programColorCopyNonMSToMSR8,
                2 => _programColorCopyNonMSToMSR16,
                4 => _programColorCopyNonMSToMSR32,
                8 => _programColorCopyNonMSToMSRG32,
                16 => _programColorCopyNonMSToMSRGBA32,
                _ => throw new ArgumentException($"Invalid bytes per pixel {bytesPerPixel}.")
            };
        }

        private static GAL.Format GetFormat(int bytesPerPixel)
        {
            return bytesPerPixel switch
            {
                1 => GAL.Format.R8Uint,
                2 => GAL.Format.R16Uint,
                4 => GAL.Format.R32Uint,
                8 => GAL.Format.R32G32Uint,
                16 => GAL.Format.R32G32B32A32Uint,
                _ => throw new ArgumentException($"Invalid bytes per pixel {bytesPerPixel}.")
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _programColorBlitClearAlpha.Dispose();
                _programColorBlit.Dispose();
                _programColorClear.Dispose();
                _programStrideChange.Dispose();
                _programColorCopyMSToNonMSR16.Dispose();
                _programColorCopyMSToNonMSR32.Dispose();
                _programColorCopyMSToNonMSR8.Dispose();
                _programColorCopyMSToNonMSRG32.Dispose();
                _programColorCopyMSToNonMSRGBA32.Dispose();
                _programColorCopyNonMSToMSR16.Dispose();
                _programColorCopyNonMSToMSR32.Dispose();
                _programColorCopyNonMSToMSR8.Dispose();
                _programColorCopyNonMSToMSRG32.Dispose();
                _programColorCopyNonMSToMSRGBA32.Dispose();
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
