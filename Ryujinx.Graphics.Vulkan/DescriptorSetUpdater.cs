using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Vulkan
{
    class DescriptorSetUpdater
    {
        private readonly VulkanGraphicsDevice _gd;
        private readonly PipelineBase _pipeline;

        private ShaderCollection _program;

        private Auto<DisposableBuffer>[] _uniformBufferRefs;
        private Auto<DisposableBuffer>[] _storageBufferRefs;
        private Auto<DisposableImageView>[] _textureRefs;
        private Auto<DisposableSampler>[] _samplerRefs;
        private Auto<DisposableImageView>[] _imageRefs;
        private TextureBuffer[] _bufferTextureRefs;
        private TextureBuffer[] _bufferImageRefs;

        private DescriptorBufferInfo[] _uniformBuffers;
        private DescriptorBufferInfo[] _storageBuffers;
        private DescriptorImageInfo[] _textures;
        private DescriptorImageInfo[] _images;
        private BufferView[] _bufferTextures;
        private BufferView[] _bufferImages;

        [Flags]
        private enum DirtyFlags
        {
            None = 0,
            Uniform = 1 << 0,
            Storage = 1 << 1,
            Texture = 1 << 2,
            Image = 1 << 3,
            BufferTexture = 1 << 4,
            BufferImage = 1 << 5,
            All = Uniform | Storage | Texture | Image | BufferTexture | BufferImage
        }

        private DirtyFlags _dirty;

        private readonly TextureView _dummyTexture;
        private readonly SamplerHolder _dummySampler;

        public DescriptorSetUpdater(VulkanGraphicsDevice gd, PipelineBase pipeline)
        {
            _gd = gd;
            _pipeline = pipeline;

            _uniformBuffers = Array.Empty<DescriptorBufferInfo>();
            _storageBuffers = Array.Empty<DescriptorBufferInfo>();
            _textures = new DescriptorImageInfo[32 * 5];
            _textureRefs = new Auto<DisposableImageView>[32 * 5];
            _samplerRefs = new Auto<DisposableSampler>[32 * 5];
            _images = Array.Empty<DescriptorImageInfo>();
            _bufferTextures = Array.Empty<BufferView>();
            _bufferImages = Array.Empty<BufferView>();

            _dummyTexture = (TextureView)gd.CreateTexture(new GAL.TextureCreateInfo(
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                4,
                GAL.Format.R8G8B8A8Unorm,
                DepthStencilMode.Depth,
                Target.Texture2D,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha), 1f);

            _dummySampler = (SamplerHolder)gd.CreateSampler(new GAL.SamplerCreateInfo(
                MinFilter.Nearest,
                MagFilter.Nearest,
                false,
                AddressMode.Repeat,
                AddressMode.Repeat,
                AddressMode.Repeat,
                CompareMode.None,
                GAL.CompareOp.Always,
                new ColorF(0, 0, 0, 0),
                0,
                0,
                0,
                1f));
        }

        public void SetProgram(ShaderCollection program)
        {
            _program = program;
            _dirty = DirtyFlags.All;
        }

        public void SetImage(int binding, ITexture image, GAL.Format imageFormat)
        {
            if (image == null)
            {
                return;
            }

            if (image is TextureBuffer imageBuffer)
            {
                if (_bufferImages.Length <= binding)
                {
                    Array.Resize(ref _bufferImages, binding + 1);
                    Array.Resize(ref _bufferImageRefs, binding + 1);
                }

                _bufferImageRefs[binding] = imageBuffer;

                SignalDirty(DirtyFlags.BufferImage);
            }
            else
            {
                if (_images.Length <= binding)
                {
                    Array.Resize(ref _images, binding + 1);
                    Array.Resize(ref _imageRefs, binding + 1);
                }

                if (image != null)
                {
                    _imageRefs[binding] = ((TextureView)image).GetIdentityImageView();
                    _images[binding] = new DescriptorImageInfo()
                    {
                        ImageLayout = ImageLayout.General
                    };
                }

                SignalDirty(DirtyFlags.Image);
            }
        }

        public void SetStorageBuffers(CommandBuffer commandBuffer, int first, ReadOnlySpan<BufferRange> buffers)
        {
            Array.Resize(ref _storageBuffers, first + buffers.Length);
            Array.Resize(ref _storageBufferRefs, first + buffers.Length);

            for (int i = 0; i < buffers.Length; i++)
            {
                var buffer = buffers[i];

                _storageBufferRefs[first + i] = _gd.BufferManager.GetBuffer(commandBuffer, buffer.Handle, false);

                _storageBuffers[first + i] = new DescriptorBufferInfo()
                {
                    Offset = (ulong)buffer.Offset,
                    Range = (ulong)buffer.Size
                };
            }

            SignalDirty(DirtyFlags.Storage);
        }

        public void SetTextureAndSampler(int binding, ITexture texture, ISampler sampler)
        {
            if (texture == null)
            {
                return;
            }

            if (texture is TextureBuffer textureBuffer)
            {
                if (_bufferTextures.Length <= binding)
                {
                    Array.Resize(ref _bufferTextures, binding + 1);
                    Array.Resize(ref _bufferTextureRefs, binding + 1);
                }

                _bufferTextureRefs[binding] = textureBuffer;

                SignalDirty(DirtyFlags.BufferTexture);
            }
            else
            {
                if (sampler == null)
                {
                    return;
                }

                if (_textures.Length <= binding)
                {
                    Array.Resize(ref _textures, binding + 1);
                    Array.Resize(ref _textureRefs, binding + 1);
                    Array.Resize(ref _samplerRefs, binding + 1);
                }

                _textureRefs[binding] = ((TextureView)texture).GetImageView();
                _samplerRefs[binding] = ((SamplerHolder)sampler).GetSampler();

                _textures[binding] = new DescriptorImageInfo()
                {
                    ImageLayout = ImageLayout.General
                };

                SignalDirty(DirtyFlags.Texture);
            }
        }

        public void SetUniformBuffers(CommandBuffer commandBuffer, int first, ReadOnlySpan<BufferRange> buffers)
        {
            Array.Resize(ref _uniformBuffers, first + buffers.Length);
            Array.Resize(ref _uniformBufferRefs, first + buffers.Length);

            for (int i = 0; i < buffers.Length; i++)
            {
                var buffer = buffers[i];

                _uniformBufferRefs[first + i] = _gd.BufferManager.GetBuffer(commandBuffer, buffer.Handle, false);

                _uniformBuffers[first + i] = new DescriptorBufferInfo()
                {
                    Offset = (ulong)buffer.Offset,
                    Range = (ulong)buffer.Size
                };
            }

            SignalDirty(DirtyFlags.Uniform);
        }

        private void SignalDirty(DirtyFlags flag)
        {
            _dirty |= flag;
        }

        public void UpdateAndBindDescriptorSets(CommandBufferScoped cbs, PipelineBindPoint pbp)
        {
            if ((_dirty & DirtyFlags.All) == 0)
            {
                return;
            }

            // System.Console.WriteLine("modified " + _dirty + " " + _modified + " on program " + _program.GetHashCode().ToString("X"));

            if (_dirty.HasFlag(DirtyFlags.Uniform))
            {
                UpdateAndBind(cbs, PipelineBase.UniformSetIndex, DirtyFlags.Uniform, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.Storage))
            {
                UpdateAndBind(cbs, PipelineBase.StorageSetIndex, DirtyFlags.Storage, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.Texture))
            {
                UpdateAndBind(cbs, PipelineBase.TextureSetIndex, DirtyFlags.Texture, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.Image))
            {
                UpdateAndBind(cbs, PipelineBase.ImageSetIndex, DirtyFlags.Image, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.BufferTexture))
            {
                UpdateAndBind(cbs, PipelineBase.BufferTextureSetIndex, DirtyFlags.BufferTexture, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.BufferImage))
            {
                UpdateAndBind(cbs, PipelineBase.BufferImageSetIndex, DirtyFlags.BufferImage, pbp);
            }

            _dirty = DirtyFlags.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateBuffer(CommandBufferScoped cbs, ref DescriptorBufferInfo info, Auto<DisposableBuffer> buffer)
        {
            info.Buffer = buffer?.Get(cbs, (int)info.Offset, (int)info.Range).Value ?? default;

            // The spec requires that buffers with null handle have offset as 0 and range as VK_WHOLE_SIZE.
            if (info.Buffer.Handle == 0)
            {
                info.Offset = 0;
                info.Range = Vk.WholeSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAndBind(CommandBufferScoped cbs, int setIndex, DirtyFlags flag, PipelineBindPoint pbp)
        {
            int stagesCount = _program.Bindings[setIndex].Length;
            if (stagesCount == 0 && setIndex != PipelineBase.UniformSetIndex)
            {
                return;
            }

            var dsc = _program.GetNewDescriptorSetCollection(_gd, cbs.CommandBufferIndex, setIndex, out var isNew).Get(cbs);

            if (isNew)
            {
                Initialize(cbs, setIndex, dsc);
            }

            if (setIndex == PipelineBase.UniformSetIndex)
            {
                Span<DescriptorBufferInfo> uniformBuffer = stackalloc DescriptorBufferInfo[1];

                uniformBuffer[0] = new DescriptorBufferInfo()
                {
                    Offset = 0,
                    Range = SupportBuffer.RequiredSize,
                    Buffer = _pipeline.RenderScaleBuffer.GetBuffer().Get(cbs, 0, SupportBuffer.RequiredSize).Value
                };

                dsc.UpdateBuffers(0, 0, uniformBuffer, DescriptorType.UniformBuffer);
            }

            for (int stageIndex = 0; stageIndex < stagesCount; stageIndex++)
            {
                var stageBindings = _program.Bindings[setIndex][stageIndex];
                int bindingsCount = stageBindings.Length;
                int count;

                for (int bindingIndex = 0; bindingIndex < bindingsCount; bindingIndex += count)
                {
                    int binding = stageBindings[bindingIndex];
                    count = 1;

                    while (bindingIndex + count < bindingsCount && stageBindings[bindingIndex + count] == binding + count)
                    {
                        count++;
                    }

                    if (setIndex == PipelineBase.UniformSetIndex)
                    {
                        count = Math.Min(count, _uniformBuffers.Length - binding);

                        if (count <= 0)
                        {
                            break;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            UpdateBuffer(cbs, ref _uniformBuffers[binding + i], _uniformBufferRefs[binding + i]);
                        }

                        ReadOnlySpan<DescriptorBufferInfo> uniformBuffers = _uniformBuffers;
                        dsc.UpdateBuffers(0, binding, uniformBuffers.Slice(binding, count), DescriptorType.UniformBuffer);
                    }
                    else if (setIndex == PipelineBase.StorageSetIndex)
                    {
                        count = Math.Min(count, _storageBuffers.Length - binding);

                        if (count <= 0)
                        {
                            break;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            UpdateBuffer(cbs, ref _storageBuffers[binding + i], _storageBufferRefs[binding + i]);
                        }

                        ReadOnlySpan<DescriptorBufferInfo> storageBuffers = _storageBuffers;
                        dsc.UpdateStorageBuffers(0, binding, storageBuffers.Slice(binding, count));
                    }
                    else if (setIndex == PipelineBase.TextureSetIndex)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ref var texture = ref _textures[binding + i];

                            texture.ImageView = _textureRefs[binding + i]?.Get(cbs).Value ?? default;
                            texture.Sampler = _samplerRefs[binding + i]?.Get(cbs).Value ?? default;
                            texture.ImageLayout = ImageLayout.General;

                            if (texture.ImageView.Handle == 0 || texture.Sampler.Handle == 0)
                            {
                                texture.ImageView = _dummyTexture.GetImageView().Get(cbs).Value;
                                texture.Sampler = _dummySampler.GetSampler().Get(cbs).Value;
                            }
                        }

                        ReadOnlySpan<DescriptorImageInfo> textures = _textures;
                        dsc.UpdateImages(0, binding, textures.Slice(binding, count), DescriptorType.CombinedImageSampler);
                    }
                    else if (setIndex == PipelineBase.ImageSetIndex)
                    {
                        count = Math.Min(count, _images.Length - binding);

                        if (count <= 0)
                        {
                            break;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            _images[binding + i].ImageView = _imageRefs[binding + i]?.Get(cbs).Value ?? default;
                        }

                        ReadOnlySpan<DescriptorImageInfo> images = _images;
                        dsc.UpdateImages(0, binding, images.Slice(binding, count), DescriptorType.StorageImage);
                    }
                    else if (setIndex == PipelineBase.BufferTextureSetIndex)
                    {
                        count = Math.Min(count, _bufferTextures.Length - binding);

                        if (count <= 0)
                        {
                            break;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            _bufferTextures[binding + i] = _bufferTextureRefs[binding + i]?.GetBufferView(cbs) ?? default;
                        }

                        ReadOnlySpan<BufferView> bufferTextures = _bufferTextures;
                        dsc.UpdateBufferImages(0, binding, bufferTextures.Slice(binding, count), DescriptorType.UniformTexelBuffer);
                    }
                    else if (setIndex == PipelineBase.BufferImageSetIndex)
                    {
                        count = Math.Min(count, _bufferImages.Length - binding);

                        if (count <= 0)
                        {
                            break;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            _bufferImages[binding + i] = _bufferImageRefs[binding + i]?.GetBufferView(cbs) ?? default;
                        }

                        ReadOnlySpan<BufferView> bufferImages = _bufferImages;
                        dsc.UpdateBufferImages(0, binding, bufferImages.Slice(binding, count), DescriptorType.StorageTexelBuffer);
                    }
                }
            }

            var sets = dsc.GetSets();

            _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, (uint)setIndex, 1, sets, 0, ReadOnlySpan<uint>.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize(CommandBufferScoped cbs, int setIndex, DescriptorSetCollection dsc)
        {
            uint stages = _program.Stages;

            while (stages != 0)
            {
                int stage = BitOperations.TrailingZeroCount(stages);
                stages &= ~(1u << stage);

                if (setIndex == PipelineBase.UniformSetIndex)
                {
                    dsc.InitializeBuffers(0, 1 + stage * Constants.MaxUniformBuffersPerStage, Constants.MaxUniformBuffersPerStage, DescriptorType.UniformBuffer);
                }
                else if (setIndex == PipelineBase.StorageSetIndex)
                {
                    dsc.InitializeBuffers(0, stage * Constants.MaxStorageBuffersPerStage, Constants.MaxStorageBuffersPerStage, DescriptorType.StorageBuffer);
                }
            }
        }

        public void SignalCommandBufferChange()
        {
            _dirty = DirtyFlags.All;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dummyTexture.Dispose();
                _dummySampler.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
