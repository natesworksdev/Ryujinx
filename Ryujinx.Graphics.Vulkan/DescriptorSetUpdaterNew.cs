using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Vulkan
{
    class DescriptorSetUpdaterNew
    {
        private readonly VulkanGraphicsDevice _gd;

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

        private uint[] _uniformBufferSizes;
        private uint[] _storageBufferSizes;

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

        public DescriptorSetUpdaterNew(VulkanGraphicsDevice gd)
        {
            _gd = gd;

            _uniformBuffers = Array.Empty<DescriptorBufferInfo>();
            _storageBuffers = Array.Empty<DescriptorBufferInfo>();
            _textures = Array.Empty<DescriptorImageInfo>();
            _images = Array.Empty<DescriptorImageInfo>();
            _bufferTextures = Array.Empty<BufferView>();
            _bufferImages = Array.Empty<BufferView>();
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

        public void SetStorageBuffers(CommandBuffer commandBuffer, ReadOnlySpan<BufferRange> buffers)
        {
            _storageBuffers = new DescriptorBufferInfo[buffers.Length];
            _storageBufferRefs = new Auto<DisposableBuffer>[buffers.Length];
            _storageBufferSizes = new uint[buffers.Length];

            for (int i = 0; i < buffers.Length; i++)
            {
                var buffer = buffers[i];

                _storageBufferRefs[i] = _gd.BufferManager.GetBuffer(commandBuffer, buffer.Handle, false, out int size);

                _storageBuffers[i] = new DescriptorBufferInfo()
                {
                    Offset = (ulong)buffer.Offset,
                    Range = (ulong)buffer.Size
                };

                _storageBufferSizes[i] = (uint)size;
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

        public void SetUniformBuffers(CommandBuffer commandBuffer, ReadOnlySpan<BufferRange> buffers)
        {
            _uniformBuffers = new DescriptorBufferInfo[buffers.Length];
            _uniformBufferRefs = new Auto<DisposableBuffer>[buffers.Length];
            _uniformBufferSizes = new uint[buffers.Length];

            for (int i = 0; i < buffers.Length; i++)
            {
                var buffer = buffers[i];

                _uniformBufferRefs[i] = _gd.BufferManager.GetBuffer(commandBuffer, buffer.Handle, false, out int size);

                _uniformBuffers[i] = new DescriptorBufferInfo()
                {
                    Offset = (ulong)buffer.Offset,
                    Range = (ulong)buffer.Size
                };

                _uniformBufferSizes[i] = (uint)size;
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
                int count = _program.Bindings[PipelineBase.UniformSetIndex].Length;

                var key = new BufferHandleSet();

                Span<uint> dynamicOffsets = stackalloc uint[count];

                int maxBinding = -1;

                for (int bindingIndex = 0; bindingIndex < count; bindingIndex++)
                {
                    int binding = _program.Bindings[PipelineBase.UniformSetIndex][bindingIndex];

                    var info = _uniformBuffers[binding];

                    key.Handles[binding] = _uniformBufferRefs[binding]?.Get(cbs, (int)info.Offset, (int)info.Range).Value.Handle ?? 0UL;
                    key.Sizes[binding] = _uniformBufferSizes[binding];

                    dynamicOffsets[bindingIndex] = (uint)_uniformBuffers[binding].Offset;

                    if (maxBinding < binding)
                    {
                        maxBinding = binding;
                    }
                }

                key.Count = maxBinding + 1;

                var sets = _program.DescriptorSetCache.GetUniformBuffer(cbs, ref key).GetSets();

                _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, PipelineBase.UniformSetIndex, 1, sets, (uint)count, dynamicOffsets);
            }

            if (_dirty.HasFlag(DirtyFlags.Storage))
            {
                int count = _program.Bindings[PipelineBase.StorageSetIndex].Length;

                var key = new BufferHandleSet();

                Span<uint> dynamicOffsets = stackalloc uint[count];

                int maxBinding = -1;

                for (int bindingIndex = 0; bindingIndex < count; bindingIndex++)
                {
                    int binding = _program.Bindings[PipelineBase.StorageSetIndex][bindingIndex];

                    var info = _storageBuffers[binding];

                    key.Handles[binding] = _storageBufferRefs[binding]?.Get(cbs, (int)info.Offset, (int)info.Range).Value.Handle ?? 0UL;
                    key.Sizes[binding] = _storageBufferSizes[binding];

                    dynamicOffsets[bindingIndex] = (uint)_storageBuffers[binding].Offset;

                    if (maxBinding < binding)
                    {
                        maxBinding = binding;
                    }
                }

                key.Count = maxBinding + 1;

                var sets = _program.DescriptorSetCache.GetStorageBuffer(cbs, ref key).GetSets();

                _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, PipelineBase.StorageSetIndex, 1, sets, (uint)count, dynamicOffsets);
            }

            if (_dirty.HasFlag(DirtyFlags.Texture))
            {
                int count = _program.Bindings[PipelineBase.TextureSetIndex].Length;

                var key = new CombinedImageHandleSet();

                int maxBinding = -1;

                for (int bindingIndex = 0; bindingIndex < count; bindingIndex++)
                {
                    int binding = _program.Bindings[PipelineBase.TextureSetIndex][bindingIndex];

                    key.ImageHandles[binding] = _textureRefs[binding]?.Get(cbs).Value.Handle ?? 0UL;
                    key.SamplerHandles[binding] = _samplerRefs[binding]?.Get(cbs).Value.Handle ?? 0UL;

                    if (maxBinding < binding)
                    {
                        maxBinding = binding;
                    }
                }

                key.Count = maxBinding + 1;

                var sets = _program.DescriptorSetCache.GetTexture(cbs, ref key).GetSets();

                _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, PipelineBase.TextureSetIndex, 1, sets, 0, Span<uint>.Empty);
            }

            if (_dirty.HasFlag(DirtyFlags.Image))
            {
                int count = _program.Bindings[PipelineBase.ImageSetIndex].Length;

                var key = new HandleSet();

                int maxBinding = -1;

                for (int bindingIndex = 0; bindingIndex < count; bindingIndex++)
                {
                    int binding = _program.Bindings[PipelineBase.ImageSetIndex][bindingIndex];

                    key.Handles[binding] = _imageRefs[binding]?.Get(cbs).Value.Handle ?? 0UL;

                    if (maxBinding < binding)
                    {
                        maxBinding = binding;
                    }
                }

                key.Count = maxBinding + 1;

                var sets = _program.DescriptorSetCache.GetImage(cbs, ref key).GetSets();

                _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, PipelineBase.ImageSetIndex, 1, sets, 0, Span<uint>.Empty);
            }

            _dirty = DirtyFlags.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateBuffer(CommandBufferScoped cbs, ref DescriptorBufferInfo info, Auto<DisposableBuffer> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            info.Buffer = buffer.Get(cbs, (int)info.Offset, (int)info.Range).Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAndBind(CommandBufferScoped cbs, int setIndex, DirtyFlags flag, PipelineBindPoint pbp)
        {
            int dynamicCount = setIndex == PipelineBase.UniformSetIndex ||
                               setIndex == PipelineBase.StorageSetIndex ? _program.Bindings[setIndex].Length : 0;

            Span<uint> dynamicOffsets = stackalloc uint[dynamicCount];

            var dsc = _program.GetNewDescriptorSetCollection(_gd, cbs.CommandBufferIndex, setIndex).Get(cbs);

            int maxBinding = -1;

            for (int bindingIndex = 0; bindingIndex < _program.Bindings[setIndex].Length; bindingIndex++)
            {
                int binding = _program.Bindings[setIndex][bindingIndex];

                if (setIndex == PipelineBase.UniformSetIndex)
                {
                    UpdateBuffer(cbs, ref _uniformBuffers[binding], _uniformBufferRefs[binding]);
                    dynamicOffsets[bindingIndex] = (uint)_uniformBuffers[binding].Offset;
                }
                else if (setIndex == PipelineBase.StorageSetIndex)
                {
                    UpdateBuffer(cbs, ref _storageBuffers[binding], _storageBufferRefs[binding]);
                    dynamicOffsets[bindingIndex] = (uint)_storageBuffers[binding].Offset;
                }
                else if (setIndex == PipelineBase.TextureSetIndex)
                {
                    _textures[binding].ImageView = _textureRefs[binding]?.Get(cbs).Value ?? default;
                    _textures[binding].Sampler = _samplerRefs[binding]?.Get(cbs).Value ?? default;
                }
                else if (setIndex == PipelineBase.ImageSetIndex)
                {
                    _images[binding].ImageView = _imageRefs[binding]?.Get(cbs).Value ?? default;
                }
                else if (setIndex == PipelineBase.BufferTextureSetIndex)
                {

                }
                else if (setIndex == PipelineBase.BufferImageSetIndex)
                {

                }

                if (maxBinding < binding)
                {
                    maxBinding = binding;
                }
            }

            int count = maxBinding + 1;

            if (setIndex == PipelineBase.UniformSetIndex)
            {
                ReadOnlySpan<DescriptorBufferInfo> uniformBuffers = _uniformBuffers;
                dsc.UpdateBuffers(0, 0, uniformBuffers.Slice(0, count), DescriptorType.UniformBufferDynamic);
            }
            else if (setIndex == PipelineBase.StorageSetIndex)
            {
                ReadOnlySpan<DescriptorBufferInfo> storageBuffers = _storageBuffers;
                dsc.UpdateBuffers(0, 0, storageBuffers.Slice(0, count), DescriptorType.StorageBufferDynamic);
            }
            else if (setIndex == PipelineBase.TextureSetIndex)
            {
                ReadOnlySpan<DescriptorImageInfo> textures = _textures;
                dsc.UpdateImages(0, 0, textures.Slice(0, count), DescriptorType.CombinedImageSampler);
            }
            else if (setIndex == PipelineBase.ImageSetIndex)
            {
                ReadOnlySpan<DescriptorImageInfo> images = _images;
                dsc.UpdateImages(0, 0, images.Slice(0, count), DescriptorType.StorageImage);
            }
            else if (setIndex == PipelineBase.BufferTextureSetIndex)
            {
                ReadOnlySpan<BufferView> bufferTextures = _bufferTextures;
                dsc.UpdateBufferImages(0, 0, bufferTextures.Slice(0, count), DescriptorType.UniformTexelBuffer);
            }
            else if (setIndex == PipelineBase.BufferImageSetIndex)
            {
                ReadOnlySpan<BufferView> bufferImages = _bufferImages;
                dsc.UpdateBufferImages(0, 0, bufferImages.Slice(0, count), DescriptorType.StorageTexelBuffer);
            }



            var sets = dsc.GetSets();

            _gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, _program.PipelineLayout, (uint)setIndex, 1, sets, (uint)dynamicCount, dynamicOffsets);
        }

        public void SignalCommandBufferChange()
        {
            _dirty = DirtyFlags.All;
        }
    }
}
