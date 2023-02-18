using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan
{
    class BindlessManager
    {
        private const int TextureIdBits = 20;
        private const int SamplerIdBits = 12;
        private const int TextureCapacity = 1 << TextureIdBits;
        private const int SamplerCapacity = 1 << SamplerIdBits;

        private const int TextureIdBlockShift = 8;
        private const int TextureIdBlockMask = 0xfff;

        private readonly Dictionary<int, int> _textureIdMap;
        private readonly Dictionary<int, int> _samplerIdMap;
        private readonly ulong[] _textureBlockBitmap;
        private readonly ulong[] _samplerBlockBitmap;

        private TextureView[] _textureRefs;
        private Auto<DisposableSampler>[] _samplerRefs;

        private readonly int[] _idMap;
        private bool _idMapDataDirty;

        private BufferHolder _idMapBuffer;

        private bool _dirty;
        private bool _hasDescriptors;

        private DescriptorSetCollection _bindlessTextures;
        private DescriptorSetCollection _bindlessSamplers;
        private DescriptorSetCollection _bindlessImages;

        public BindlessManager()
        {
            _textureIdMap = new Dictionary<int, int>();
            _samplerIdMap = new Dictionary<int, int>();
            _textureBlockBitmap = new ulong[((TextureCapacity >> TextureIdBlockShift) + 63) / 64];
            _samplerBlockBitmap = new ulong[((SamplerCapacity >> TextureIdBlockShift) + 63) / 64];

            _textureRefs = Array.Empty<TextureView>();
            _samplerRefs = Array.Empty<Auto<DisposableSampler>>();

            // This is actually a structure with 4 elements,
            // texture index (X), sampler index (Y), scale (Z) and W is currently unused.
            _idMap = new int[(TextureCapacity >> TextureIdBlockShift) * 4];
        }

        public void SetBindlessTexture(int textureId, TextureView texture)
        {
            int textureIndex = GetTextureBlockId(textureId);

            _textureRefs[textureIndex] = texture;
            _dirty = true;
        }

        public void SetBindlessSampler(int samplerId, Auto<DisposableSampler> sampler)
        {
            int samplerIndex = GetSamplerBlockId(samplerId);

            _samplerRefs[samplerIndex] = sampler;
            _dirty = true;
        }

        private int GetTextureBlockId(int textureId)
        {
            return GetBlockId(textureId, 0, _textureIdMap, _textureBlockBitmap, ref _textureRefs);
        }

        private int GetSamplerBlockId(int samplerId)
        {
            return GetBlockId(samplerId, 1, _samplerIdMap, _samplerBlockBitmap, ref _samplerRefs);
        }

        private int GetBlockId<T>(int id, int idMapOffset, Dictionary<int, int> idMap, ulong[] bitmap, ref T[] resourceRefs)
        {
            int blockIndex = (id >> TextureIdBlockShift) & TextureIdBlockMask;

            if (!idMap.TryGetValue(blockIndex, out int mappedIndex))
            {
                mappedIndex = AllocateNewBlock(bitmap);

                int minLength = (mappedIndex + 1) << TextureIdBlockShift;

                if (minLength > resourceRefs.Length)
                {
                    Array.Resize(ref resourceRefs, minLength);
                }

                _idMap[blockIndex * 4 + idMapOffset] = mappedIndex << TextureIdBlockShift;
                _idMapDataDirty = true;

                idMap.Add(blockIndex, mappedIndex);
            }

            return (mappedIndex << TextureIdBlockShift) | (id & ~(TextureIdBlockMask << TextureIdBlockShift));
        }

        private static int AllocateNewBlock(ulong[] bitmap)
        {
            for (int index = 0; index < bitmap.Length; index++)
            {
                ref ulong v = ref bitmap[index];

                if (v == ulong.MaxValue)
                {
                    continue;
                }

                int firstFreeBit = BitOperations.TrailingZeroCount(~v);
                v |= 1UL << firstFreeBit;
                return index * 64 + firstFreeBit;
            }

            throw new InvalidOperationException("No free space left on the texture or sampler table.");
        }

        public void UpdateAndBind(
            VulkanRenderer gd,
            ShaderCollection program,
            CommandBufferScoped cbs,
            PipelineBindPoint pbp,
            SamplerHolder dummySampler)
        {
            if (!_dirty)
            {
                Rebind(gd, program, cbs, pbp);
                return;
            }

            _dirty = false;

            var btDsc = program.GetNewDescriptorSetCollection(gd, cbs.CommandBufferIndex, PipelineBase.BindlessTexturesSetIndex, out _).Get(cbs);
            var bsDsc = program.GetNewDescriptorSetCollection(gd, cbs.CommandBufferIndex, PipelineBase.BindlessSamplersSetIndex, out _).Get(cbs);
            var biDsc = program.GetNewDescriptorSetCollection(gd, cbs.CommandBufferIndex, PipelineBase.BindlessImagesSetIndex, out _).Get(cbs);

            int bufferSizeInBytes = _idMap.Length * sizeof(int);

            if (_idMapBuffer == null)
            {
                _idMapBuffer = gd.BufferManager.Create(gd, bufferSizeInBytes);
            }

            if (_idMapDataDirty)
            {
                _idMapBuffer.SetDataUnchecked(0, MemoryMarshal.Cast<int, byte>(_idMap));
                _idMapDataDirty = false;
            }

            Span<DescriptorBufferInfo> uniformBuffer = stackalloc DescriptorBufferInfo[1];

            uniformBuffer[0] = new DescriptorBufferInfo()
            {
                Offset = 0,
                Range = (ulong)bufferSizeInBytes,
                Buffer = _idMapBuffer.GetBuffer().Get(cbs, 0, bufferSizeInBytes).Value
            };

            btDsc.UpdateBuffers(0, 0, uniformBuffer, DescriptorType.UniformBuffer);

            for (int i = 0; i < _textureRefs.Length; i++)
            {
                var texture = _textureRefs[i];
                if (texture != null)
                {
                    var td = new DescriptorImageInfo()
                    {
                        ImageLayout = ImageLayout.General,
                        ImageView = texture.GetImageView().Get(cbs).Value
                    };

                    btDsc.UpdateImage(0, 1, i, td, DescriptorType.SampledImage);

                    if (texture.Info.Format.IsImageCompatible())
                    {
                        td = new DescriptorImageInfo()
                        {
                            ImageLayout = ImageLayout.General,
                            ImageView = texture.GetIdentityImageView().Get(cbs).Value
                        };

                        biDsc.UpdateImage(0, 0, i, td, DescriptorType.StorageImage);
                    }
                }
            }

            for (int i = 0; i < _samplerRefs.Length; i++)
            {
                var sampler = _samplerRefs[i];
                if (sampler != null)
                {
                    var sd = new DescriptorImageInfo()
                    {
                        Sampler = sampler.Get(cbs).Value
                    };

                    if (sd.Sampler.Handle == 0)
                    {
                        sd.Sampler = dummySampler.GetSampler().Get(cbs).Value;
                    }

                    bsDsc.UpdateImage(0, 0, i, sd, DescriptorType.Sampler);
                }
            }

            _bindlessTextures = btDsc;
            _bindlessSamplers = bsDsc;
            _bindlessImages = biDsc;

            _hasDescriptors = true;

            Bind(gd, program, cbs, pbp, btDsc, bsDsc, biDsc);
        }

        private void Rebind(VulkanRenderer gd, ShaderCollection program, CommandBufferScoped cbs, PipelineBindPoint pbp)
        {
            if (_hasDescriptors)
            {
                Bind(gd, program, cbs, pbp, _bindlessTextures, _bindlessSamplers, _bindlessImages);
            }
        }

        private void Bind(
            VulkanRenderer gd,
            ShaderCollection program,
            CommandBufferScoped cbs,
            PipelineBindPoint pbp,
            DescriptorSetCollection bindlessTextures,
            DescriptorSetCollection bindlessSamplers,
            DescriptorSetCollection bindlessImages)
        {
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, program.PipelineLayout, PipelineBase.BindlessTexturesSetIndex, 1, bindlessTextures.GetSets(), 0, ReadOnlySpan<uint>.Empty);
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, program.PipelineLayout, PipelineBase.BindlessSamplersSetIndex, 1, bindlessSamplers.GetSets(), 0, ReadOnlySpan<uint>.Empty);
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, program.PipelineLayout, PipelineBase.BindlessImagesSetIndex, 1, bindlessImages.GetSets(), 0, ReadOnlySpan<uint>.Empty);
        }
    }
}