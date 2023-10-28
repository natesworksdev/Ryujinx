using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Numerics;
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

        // Note that each entry must be aligned to 16 bytes, this is a constant buffer layout restriction.
        private const int IdMapElements = 4;

        public const uint MinimumTexturesCount = 256;
        public const uint MinimumSamplersCount = 256;

        private readonly Dictionary<int, int> _textureIdMap;
        private readonly Dictionary<int, int> _samplerIdMap;
        private readonly ulong[] _textureBlockBitmap;
        private readonly ulong[] _samplerBlockBitmap;

        private ITexture[] _textureRefs;
        private float[] _textureScales;
        private Auto<DisposableSampler>[] _samplerRefs;
        private bool _textureScalesDirty;

        public uint TexturesCount => CalculateTexturesCount();
        public uint SamplersCount => CalculateSamplersCount();

        private readonly int[] _idMap;
        private bool _idMapDataDirty;

        private BufferHolder _idMapBuffer;
        private BufferHolder _textureScalesBuffer;

        private bool _dirty;
        private bool _hasDescriptors;

        private PipelineLayout _pipelineLayout;
        private DescriptorSetCollection _bindlessTextures;
        private DescriptorSetCollection _bindlessSamplers;
        private DescriptorSetCollection _bindlessImages;
        private DescriptorSetCollection _bindlessBufferTextures;
        private DescriptorSetCollection _bindlessBufferImages;

        public BindlessManager()
        {
            _textureIdMap = new Dictionary<int, int>();
            _samplerIdMap = new Dictionary<int, int>();
            _textureBlockBitmap = new ulong[((TextureCapacity >> TextureIdBlockShift) + 63) / 64];
            _samplerBlockBitmap = new ulong[((SamplerCapacity >> TextureIdBlockShift) + 63) / 64];

            _textureRefs = Array.Empty<TextureView>();
            _textureScales = Array.Empty<float>();
            _samplerRefs = Array.Empty<Auto<DisposableSampler>>();

            // This is actually a structure with 2 elements,
            // texture index (X) and sampler index (Y).
            _idMap = new int[(TextureCapacity >> TextureIdBlockShift) * IdMapElements];
        }

        private uint CalculateTexturesCount()
        {
            return Math.Max(MinimumTexturesCount, (uint)BitUtils.Pow2RoundUp(_textureRefs.Length));
        }

        private uint CalculateSamplersCount()
        {
            return Math.Max(MinimumSamplersCount, (uint)BitUtils.Pow2RoundUp(_samplerRefs.Length));
        }

        public void SetBindlessTexture(int textureId, ITexture texture, float scale)
        {
            int textureIndex = GetTextureBlockId(textureId);

            _textureRefs[textureIndex] = texture;

            if (_textureRefs.Length != _textureScales.Length)
            {
                Array.Resize(ref _textureScales, _textureRefs.Length);
            }

            if (_textureScales[textureIndex] != scale)
            {
                _textureScales[textureIndex] = scale;
                _textureScalesDirty = true;
            }

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

                _idMap[blockIndex * IdMapElements + idMapOffset] = mappedIndex << TextureIdBlockShift;
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

            var plce = program.GetPipelineLayoutCacheEntry(gd, TexturesCount, SamplersCount);

            plce.UpdateCommandBufferIndex(cbs.CommandBufferIndex);

            var btDsc = plce.GetNewDescriptorSetCollection(PipelineBase.BindlessTexturesSetIndex, out _).Get(cbs);
            var bbtDsc = plce.GetNewDescriptorSetCollection(PipelineBase.BindlessBufferTextureSetIndex, out _).Get(cbs);
            var bsDsc = plce.GetNewDescriptorSetCollection(PipelineBase.BindlessSamplersSetIndex, out _).Get(cbs);
            var biDsc = plce.GetNewDescriptorSetCollection(PipelineBase.BindlessImagesSetIndex, out _).Get(cbs);
            var bbiDsc = plce.GetNewDescriptorSetCollection(PipelineBase.BindlessBufferImageSetIndex, out _).Get(cbs);

            int idMapBufferSizeInBytes = _idMap.Length * sizeof(int);

            _idMapBuffer ??= gd.BufferManager.Create(gd, idMapBufferSizeInBytes);

            if (_idMapDataDirty)
            {
                _idMapBuffer.SetDataUnchecked(0, MemoryMarshal.Cast<int, byte>(_idMap));
                _idMapDataDirty = false;
            }

            int textureScalesBufferSizeInBytes = _textureScales.Length * sizeof(float);

            if (_textureScalesDirty)
            {
                if (_textureScalesBuffer == null || _textureScalesBuffer.Size != textureScalesBufferSizeInBytes)
                {
                    _textureScalesBuffer?.Dispose();
                    _textureScalesBuffer = gd.BufferManager.Create(gd, textureScalesBufferSizeInBytes);
                }

                _textureScalesBuffer.SetDataUnchecked(0, MemoryMarshal.Cast<float, byte>(_textureScales));
                _textureScalesDirty = false;
            }

            Span<DescriptorBufferInfo> uniformBuffer = stackalloc DescriptorBufferInfo[1];

            uniformBuffer[0] = new DescriptorBufferInfo()
            {
                Offset = 0,
                Range = (ulong)idMapBufferSizeInBytes,
                Buffer = _idMapBuffer.GetBuffer().Get(cbs, 0, idMapBufferSizeInBytes).Value
            };

            btDsc.UpdateBuffers(0, 0, uniformBuffer, DescriptorType.UniformBuffer);

            if (_textureScalesBuffer != null)
            {
                Span<DescriptorBufferInfo> storageBuffer = stackalloc DescriptorBufferInfo[1];

                storageBuffer[0] = new DescriptorBufferInfo()
                {
                    Offset = 0,
                    Range = (ulong)textureScalesBufferSizeInBytes,
                    Buffer = _textureScalesBuffer.GetBuffer().Get(cbs, 0, textureScalesBufferSizeInBytes).Value
                };

                btDsc.UpdateBuffers(0, 1, storageBuffer, DescriptorType.StorageBuffer);
            }

            for (int i = 0; i < _textureRefs.Length; i++)
            {
                var texture = _textureRefs[i];
                if (texture is TextureView view)
                {
                    var td = new DescriptorImageInfo()
                    {
                        ImageLayout = ImageLayout.General,
                        ImageView = view.GetImageView().Get(cbs).Value
                    };

                    btDsc.UpdateImage(0, 2, i, td, DescriptorType.SampledImage);

                    if (view.Info.Format.IsImageCompatible())
                    {
                        td = new DescriptorImageInfo()
                        {
                            ImageLayout = ImageLayout.General,
                            ImageView = view.GetIdentityImageView().Get(cbs).Value
                        };

                        biDsc.UpdateImage(0, 0, i, td, DescriptorType.StorageImage);
                    }
                }
                else if (texture is TextureBuffer buffer)
                {
                    bool isImageCompatible = buffer.Format.IsImageCompatible();
                    var bufferView = buffer.GetBufferView(cbs, isImageCompatible);

                    bbtDsc.UpdateBufferImage(0, 0, i, bufferView, DescriptorType.UniformTexelBuffer);

                    if (isImageCompatible)
                    {
                        bbiDsc.UpdateBufferImage(0, 0, i, bufferView, DescriptorType.StorageTexelBuffer);
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

            _pipelineLayout = plce.PipelineLayout;
            _bindlessTextures = btDsc;
            _bindlessSamplers = bsDsc;
            _bindlessImages = biDsc;
            _bindlessBufferTextures = bbtDsc;
            _bindlessBufferImages = bbiDsc;

            _hasDescriptors = true;

            Bind(gd, program, cbs, pbp, plce.PipelineLayout, btDsc, bsDsc, biDsc, bbtDsc, bbiDsc);
        }

        private void Rebind(VulkanRenderer gd, ShaderCollection program, CommandBufferScoped cbs, PipelineBindPoint pbp)
        {
            if (_hasDescriptors)
            {
                Bind(
                    gd,
                    program,
                    cbs,
                    pbp,
                    _pipelineLayout,
                    _bindlessTextures,
                    _bindlessSamplers,
                    _bindlessImages,
                    _bindlessBufferTextures,
                    _bindlessBufferImages);
            }
        }

        private static void Bind(
            VulkanRenderer gd,
            ShaderCollection program,
            CommandBufferScoped cbs,
            PipelineBindPoint pbp,
            PipelineLayout pipelineLayout,
            DescriptorSetCollection bindlessTextures,
            DescriptorSetCollection bindlessSamplers,
            DescriptorSetCollection bindlessImages,
            DescriptorSetCollection bindlessBufferTextures,
            DescriptorSetCollection bindlessBufferImages)
        {
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, pipelineLayout, PipelineBase.BindlessTexturesSetIndex, 1, bindlessTextures.GetSets(), 0, ReadOnlySpan<uint>.Empty);
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, pipelineLayout, PipelineBase.BindlessBufferTextureSetIndex, 1, bindlessBufferTextures.GetSets(), 0, ReadOnlySpan<uint>.Empty);
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, pipelineLayout, PipelineBase.BindlessSamplersSetIndex, 1, bindlessSamplers.GetSets(), 0, ReadOnlySpan<uint>.Empty);
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, pipelineLayout, PipelineBase.BindlessImagesSetIndex, 1, bindlessImages.GetSets(), 0, ReadOnlySpan<uint>.Empty);
            gd.Api.CmdBindDescriptorSets(cbs.CommandBuffer, pbp, pipelineLayout, PipelineBase.BindlessBufferImageSetIndex, 1, bindlessBufferImages.GetSets(), 0, ReadOnlySpan<uint>.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _idMapBuffer?.Dispose();
                _textureScalesBuffer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
