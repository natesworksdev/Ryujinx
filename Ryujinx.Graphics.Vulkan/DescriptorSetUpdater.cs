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
        private GAL.Format[] _bufferImageFormats;

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
            All = Uniform | Storage | Texture | Image
        }

        private DirtyFlags _dirty;

        private readonly BufferHolder _dummyBuffer;
        private readonly TextureView _dummyTexture;
        private readonly SamplerHolder _dummySampler;

        public DescriptorSetUpdater(VulkanGraphicsDevice gd, PipelineBase pipeline)
        {
            _gd = gd;
            _pipeline = pipeline;

            _uniformBufferRefs = new Auto<DisposableBuffer>[Constants.MaxUniformBufferBindings];
            _storageBufferRefs = new Auto<DisposableBuffer>[Constants.MaxStorageBufferBindings];
            _textureRefs = new Auto<DisposableImageView>[Constants.MaxTextureBindings];
            _samplerRefs = new Auto<DisposableSampler>[Constants.MaxTextureBindings];
            _imageRefs = new Auto<DisposableImageView>[Constants.MaxImageBindings];
            _bufferTextureRefs = new TextureBuffer[Constants.MaxTextureBindings];
            _bufferImageRefs = new TextureBuffer[Constants.MaxImageBindings];
            _bufferImageFormats = new GAL.Format[Constants.MaxImageBindings];

            _uniformBuffers = new DescriptorBufferInfo[Constants.MaxUniformBufferBindings];
            _storageBuffers = new DescriptorBufferInfo[Constants.MaxStorageBufferBindings];
            _textures = new DescriptorImageInfo[Constants.MaxTextureBindings];
            _images = new DescriptorImageInfo[Constants.MaxImageBindings];
            _bufferTextures = new BufferView[Constants.MaxTextureBindings];
            _bufferImages = new BufferView[Constants.MaxImageBindings];

            if (gd.Capabilities.SupportsNullDescriptors)
            {
                // If null descriptors are supported, we can pass null as the handle.
                _dummyBuffer = null;
            }
            else
            {
                // If null descriptors are not supported, we need to pass the handle of a dummy buffer on unused bindings.
                _dummyBuffer = gd.BufferManager.Create(gd, 0x10000, forConditionalRendering: false, deviceLocal: true);
            }

            _dummyTexture = gd.CreateTextureView(new GAL.TextureCreateInfo(
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
                _bufferImageRefs[binding] = imageBuffer;
                _bufferImageFormats[binding] = imageFormat;
            }
            else if (image is TextureView view)
            {
                _imageRefs[binding] = view.GetView(imageFormat).GetIdentityImageView();
                _images[binding] = new DescriptorImageInfo()
                {
                    ImageLayout = ImageLayout.General
                };
            }

            SignalDirty(DirtyFlags.Image);
        }

        public void SetStorageBuffers(CommandBuffer commandBuffer, int first, ReadOnlySpan<BufferRange> buffers)
        {
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

        public void SetTextureAndSampler(CommandBufferScoped cbs, ShaderStage stage, int binding, ITexture texture, ISampler sampler)
        {
            if (texture == null)
            {
                return;
            }

            if (texture is TextureBuffer textureBuffer)
            {
                _bufferTextureRefs[binding] = textureBuffer;
            }
            else
            {
                TextureView view = (TextureView)texture;

                view.Storage.InsertBarrier(cbs, AccessFlags.AccessShaderReadBit, stage.ConvertToPipelineStageFlags());

                _textureRefs[binding] = view.GetImageView();
                _samplerRefs[binding] = ((SamplerHolder)sampler)?.GetSampler();

                _textures[binding] = new DescriptorImageInfo()
                {
                    ImageLayout = ImageLayout.General
                };
            }

            SignalDirty(DirtyFlags.Texture);
        }

        public void SetUniformBuffers(CommandBuffer commandBuffer, int first, ReadOnlySpan<BufferRange> buffers)
        {
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
                if (_program.UsePushDescriptors)
                {
                    UpdateAndBindUniformBufferPd(cbs, pbp);
                }
                else
                {
                    UpdateAndBind(cbs, PipelineBase.UniformSetIndex, pbp);
                }
            }

            if (_dirty.HasFlag(DirtyFlags.Storage))
            {
                UpdateAndBind(cbs, PipelineBase.StorageSetIndex, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.Texture))
            {
                UpdateAndBind(cbs, PipelineBase.TextureSetIndex, pbp);
            }

            if (_dirty.HasFlag(DirtyFlags.Image))
            {
                UpdateAndBind(cbs, PipelineBase.ImageSetIndex, pbp);
            }

            _dirty = DirtyFlags.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateBuffer(
            CommandBufferScoped cbs,
            ref DescriptorBufferInfo info,
            Auto<DisposableBuffer> buffer,
            Auto<DisposableBuffer> dummyBuffer)
        {
            info.Buffer = buffer?.Get(cbs, (int)info.Offset, (int)info.Range).Value ?? default;

            // The spec requires that buffers with null handle have offset as 0 and range as VK_WHOLE_SIZE.
            if (info.Buffer.Handle == 0)
            {
                info.Buffer = dummyBuffer?.Get(cbs).Value ?? default;
                info.Offset = 0;
                info.Range = Vk.WholeSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAndBind(CommandBufferScoped cbs, int setIndex, PipelineBindPoint pbp)
        {
            var program = _program;
            int stagesCount = program.Bindings[setIndex].Length;
            if (stagesCount == 0 && setIndex != PipelineBase.UniformSetIndex)
            {
                return;
            }

            var dummyBuffer = _dummyBuffer?.GetBuffer();

            var dsc = program.GetNewDescriptorSetCollection(_gd, cbs.CommandBufferIndex, setIndex, out var isNew).Get(cbs);

            if (!program.HasMinimalLayout)
            {
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
                        Range = (ulong)SupportBuffer.RequiredSize,
                        Buffer = _gd.BufferManager.GetBuffer(cbs.CommandBuffer, _pipeline.SupportBufferUpdater.Handle, false).Get(cbs, 0, SupportBuffer.RequiredSize).Value
                    };

                    dsc.UpdateBuffers(0, 0, uniformBuffer, DescriptorType.UniformBuffer);
                }
            }

            for (int stageIndex = 0; stageIndex < stagesCount; stageIndex++)
            {
                var stageBindings = program.Bindings[setIndex][stageIndex];
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
                        for (int i = 0; i < count; i++)
                        {
                            UpdateBuffer(cbs, ref _uniformBuffers[binding + i], _uniformBufferRefs[binding + i], dummyBuffer);
                        }

                        ReadOnlySpan<DescriptorBufferInfo> uniformBuffers = _uniformBuffers;
                        dsc.UpdateBuffers(0, binding, uniformBuffers.Slice(binding, count), DescriptorType.UniformBuffer);
                    }
                    else if (setIndex == PipelineBase.StorageSetIndex)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            UpdateBuffer(cbs, ref _storageBuffers[binding + i], _storageBufferRefs[binding + i], dummyBuffer);
                        }

                        ReadOnlySpan<DescriptorBufferInfo> storageBuffers = _storageBuffers;
                        dsc.UpdateStorageBuffers(0, binding, storageBuffers.Slice(binding, count));
                    }
                    else if (setIndex == PipelineBase.TextureSetIndex)
                    {
                        if (((uint)binding % (Constants.MaxTexturesPerStage * 2)) < Constants.MaxTexturesPerStage || program.HasMinimalLayout)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                ref var texture = ref _textures[binding + i];

                                texture.ImageView = _textureRefs[binding + i]?.Get(cbs).Value ?? default;
                                texture.Sampler = _samplerRefs[binding + i]?.Get(cbs).Value ?? default;
                                texture.ImageLayout = ImageLayout.General;

                                if (texture.ImageView.Handle == 0)
                                {
                                    texture.ImageView = _dummyTexture.GetImageView().Get(cbs).Value;
                                }

                                if (texture.Sampler.Handle == 0)
                                {
                                    texture.Sampler = _dummySampler.GetSampler().Get(cbs).Value;
                                }
                            }

                            ReadOnlySpan<DescriptorImageInfo> textures = _textures;
                            dsc.UpdateImages(0, binding, textures.Slice(binding, count), DescriptorType.CombinedImageSampler);
                        }
                        else
                        {
                            for (int i = 0; i < count; i++)
                            {
                                _bufferTextures[binding + i] = _bufferTextureRefs[binding + i]?.GetBufferView(cbs) ?? default;
                            }

                            ReadOnlySpan<BufferView> bufferTextures = _bufferTextures;
                            dsc.UpdateBufferImages(0, binding, bufferTextures.Slice(binding, count), DescriptorType.UniformTexelBuffer);
                        }
                    }
                    else if (setIndex == PipelineBase.ImageSetIndex)
                    {
                        if (((uint)binding % (Constants.MaxImagesPerStage * 2)) < Constants.MaxImagesPerStage || program.HasMinimalLayout)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                _images[binding + i].ImageView = _imageRefs[binding + i]?.Get(cbs).Value ?? default;
                            }

                            ReadOnlySpan<DescriptorImageInfo> images = _images;
                            dsc.UpdateImages(0, binding, images.Slice(binding, count), DescriptorType.StorageImage);
                        }
                        else
                        {
                            for (int i = 0; i < count; i++)
                            {
                                _bufferImages[binding + i] = _bufferImageRefs[binding + i]?.GetBufferView(cbs, _bufferImageFormats[binding + i]) ?? default;
                            }

                            ReadOnlySpan<BufferView> bufferImages = _bufferImages;
                            dsc.UpdateBufferImages(0, binding, bufferImages.Slice(binding, count), DescriptorType.StorageTexelBuffer);
                        }
                    }
                }
            }

            var sets = dsc.GetSets();

            _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, (uint)setIndex, 1, sets, 0, ReadOnlySpan<uint>.Empty);
        }

        private unsafe void UpdateBuffers(
            CommandBufferScoped cbs,
            PipelineBindPoint pbp,
            int baseBinding,
            ReadOnlySpan<DescriptorBufferInfo> bufferInfo,
            DescriptorType type)
        {
            if (bufferInfo.Length == 0)
            {
                return;
            }

            fixed (DescriptorBufferInfo* pBufferInfo = bufferInfo)
            {
                var writeDescriptorSet = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstBinding = (uint)baseBinding,
                    DescriptorType = type,
                    DescriptorCount = (uint)bufferInfo.Length,
                    PBufferInfo = pBufferInfo
                };

                _gd.PushDescriptorApi.CmdPushDescriptorSet(cbs.CommandBuffer, pbp, _program.PipelineLayout, 0, 1, &writeDescriptorSet);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAndBindUniformBufferPd(CommandBufferScoped cbs, PipelineBindPoint pbp)
        {
            var dummyBuffer = _dummyBuffer?.GetBuffer();
            int stagesCount = _program.Bindings[PipelineBase.UniformSetIndex].Length;

            Span<DescriptorBufferInfo> uniformBuffer = stackalloc DescriptorBufferInfo[1];

            uniformBuffer[0] = new DescriptorBufferInfo()
            {
                Offset = 0,
                Range = (ulong)SupportBuffer.RequiredSize,
                Buffer = _gd.BufferManager.GetBuffer(cbs.CommandBuffer, _pipeline.SupportBufferUpdater.Handle, false).Get(cbs, 0, SupportBuffer.RequiredSize).Value
            };

            UpdateBuffers(cbs, pbp, 0, uniformBuffer, DescriptorType.UniformBuffer);

            for (int stageIndex = 0; stageIndex < stagesCount; stageIndex++)
            {
                var stageBindings = _program.Bindings[PipelineBase.UniformSetIndex][stageIndex];
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

                    for (int i = 0; i < count; i++)
                    {
                        UpdateBuffer(cbs, ref _uniformBuffers[binding + i], _uniformBufferRefs[binding + i], dummyBuffer);
                    }

                    ReadOnlySpan<DescriptorBufferInfo> uniformBuffers = _uniformBuffers;
                    UpdateBuffers(cbs, pbp, binding, uniformBuffers.Slice(binding, count), DescriptorType.UniformBuffer);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize(CommandBufferScoped cbs, int setIndex, DescriptorSetCollection dsc)
        {
            var dummyBuffer = _dummyBuffer?.GetBuffer().Get(cbs).Value ?? default;

            uint stages = _program.Stages;

            while (stages != 0)
            {
                int stage = BitOperations.TrailingZeroCount(stages);
                stages &= ~(1u << stage);

                if (setIndex == PipelineBase.UniformSetIndex)
                {
                    dsc.InitializeBuffers(
                        0,
                        1 + stage * Constants.MaxUniformBuffersPerStage,
                        Constants.MaxUniformBuffersPerStage,
                        DescriptorType.UniformBuffer,
                        dummyBuffer);
                }
                else if (setIndex == PipelineBase.StorageSetIndex)
                {
                    dsc.InitializeBuffers(
                        0,
                        stage * Constants.MaxStorageBuffersPerStage,
                        Constants.MaxStorageBuffersPerStage,
                        DescriptorType.StorageBuffer,
                        dummyBuffer);
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
