using Ryujinx.Common.Memory;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace Ryujinx.Graphics.Vulkan
{
    struct HandleSet : IEquatable<HandleSet>
    {
        public Array16<ulong> Handles;
        public int Count;

        public override bool Equals(object obj)
        {
            return obj is HandleSet other && Equals(other);
        }

        public bool Equals(HandleSet other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (Handles[i] != other.Handles[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            for (int i = 0; i < Count; i++)
            {
                hashCode.Add(Handles[i]);
            }

            return hashCode.ToHashCode();
        }
    }

    struct BufferHandleSet : IEquatable<BufferHandleSet>
    {
        public Array16<ulong> Handles;
        public Array16<uint> Sizes;
        public int Count;

        public override bool Equals(object obj)
        {
            return obj is BufferHandleSet other && Equals(other);
        }

        public bool Equals(BufferHandleSet other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (Handles[i] != other.Handles[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            for (int i = 0; i < Count; i++)
            {
                hashCode.Add(Handles[i]);
            }

            return hashCode.ToHashCode();
        }
    }

    struct CombinedImageHandleSet : IEquatable<CombinedImageHandleSet>
    {
        public Array32<ulong> ImageHandles;
        public Array32<ulong> SamplerHandles;
        public int Count;

        public override bool Equals(object obj)
        {
            return obj is CombinedImageHandleSet other && Equals(other);
        }

        public bool Equals(CombinedImageHandleSet other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (ImageHandles[i] != other.ImageHandles[i] || SamplerHandles[i] != other.SamplerHandles[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            for (int i = 0; i < Count; i++)
            {
                hashCode.Add(ImageHandles[i]);
                hashCode.Add(SamplerHandles[i]);
            }

            return hashCode.ToHashCode();
        }
    }

    class DescriptorSetCache
    {
        private readonly VulkanGraphicsDevice _gd;
        private readonly DescriptorSetLayout[] _descriptorSetLayouts;
        private readonly Dictionary<BufferHandleSet, DescriptorSetCollection> _uCache;
        private readonly Dictionary<BufferHandleSet, DescriptorSetCollection> _sCache;
        private readonly Dictionary<CombinedImageHandleSet, DescriptorSetCollection> _tCache;
        private readonly Dictionary<HandleSet, DescriptorSetCollection> _iCache;
        private readonly Dictionary<HandleSet, DescriptorSetCollection> _bTCache;
        private readonly Dictionary<HandleSet, DescriptorSetCollection> _bICache;

        public DescriptorSetCache(VulkanGraphicsDevice gd, DescriptorSetLayout[] descriptorSetLayouts)
        {
            _gd = gd;
            _descriptorSetLayouts = descriptorSetLayouts;
            _uCache = new Dictionary<BufferHandleSet, DescriptorSetCollection>();
            _sCache = new Dictionary<BufferHandleSet, DescriptorSetCollection>();
            _tCache = new Dictionary<CombinedImageHandleSet, DescriptorSetCollection>();
            _iCache = new Dictionary<HandleSet, DescriptorSetCollection>();
            _bTCache = new Dictionary<HandleSet, DescriptorSetCollection>();
            _bICache = new Dictionary<HandleSet, DescriptorSetCollection>();
        }

        public DescriptorSetCollection GetUniformBuffer(CommandBufferScoped cbs, ref BufferHandleSet key)
        {
            return GetOrCreate(cbs, ref key, _uCache, CreateUniformBuffer);
        }

        public DescriptorSetCollection GetStorageBuffer(CommandBufferScoped cbs, ref BufferHandleSet key)
        {
            return GetOrCreate(cbs, ref key, _sCache, CreateStorageBuffer);
        }

        public DescriptorSetCollection GetTexture(CommandBufferScoped cbs, ref CombinedImageHandleSet key)
        {
            return GetOrCreate(cbs, ref key, _tCache, CreateImage);
        }

        public DescriptorSetCollection GetImage(CommandBufferScoped cbs, ref HandleSet key)
        {
            return GetOrCreate(cbs, ref key, _iCache, CreateImage);
        }

        public DescriptorSetCollection GetBufferTexture(CommandBufferScoped cbs, ref HandleSet key)
        {
            return GetOrCreate(cbs, ref key, _bTCache, CreateBufferImage);
        }

        public DescriptorSetCollection GetBufferImage(CommandBufferScoped cbs, ref HandleSet key)
        {
            return GetOrCreate(cbs, ref key, _bICache, CreateBufferImage);
        }

        private delegate DescriptorSetCollection CreateCallback<T>(CommandBufferScoped cbs, ref T key);

        private static DescriptorSetCollection GetOrCreate<T>(
            CommandBufferScoped cbs,
            ref T key,
            Dictionary<T, DescriptorSetCollection> hashTable,
            CreateCallback<T> createCallback)
        {
            if (!hashTable.TryGetValue(key, out var ds))
            {
                ds = createCallback(cbs, ref key);
                hashTable.Add(key, ds);
            }

            return ds;
        }

        private DescriptorSetCollection CreateUniformBuffer(CommandBufferScoped cbs, ref BufferHandleSet key)
        {
            return CreateBuffer(cbs, ref key, PipelineBase.UniformSetIndex, DescriptorType.UniformBufferDynamic);
        }

        private DescriptorSetCollection CreateStorageBuffer(CommandBufferScoped cbs, ref BufferHandleSet key)
        {
            return CreateBuffer(cbs, ref key, PipelineBase.StorageSetIndex, DescriptorType.StorageBufferDynamic);
        }

        private DescriptorSetCollection CreateBuffer(CommandBufferScoped cbs, ref BufferHandleSet key, int setIndex, DescriptorType type)
        {
            var dsc = _gd.DescriptorSetManager.AllocateDescriptorSet(_gd.Api, _descriptorSetLayouts[setIndex]).Get(cbs);

            Span<DescriptorBufferInfo> bufferInfos = stackalloc DescriptorBufferInfo[key.Count];

            for (int i = 0; i < key.Count; i++)
            {
                uint size = key.Sizes[i];

                if (type == DescriptorType.UniformBufferDynamic)
                {
                    size = Math.Min(size, 0x10000);
                }

                bufferInfos[i] = new DescriptorBufferInfo()
                {
                    Buffer = new VkBuffer(key.Handles[i]),
                    Offset = 0,
                    Range = size
                };
            }

            dsc.UpdateBuffers(0, 0, bufferInfos, type);

            return dsc;
        }

        private DescriptorSetCollection CreateImage(CommandBufferScoped cbs, ref CombinedImageHandleSet key)
        {
            var dsc = _gd.DescriptorSetManager.AllocateDescriptorSet(_gd.Api, _descriptorSetLayouts[PipelineBase.TextureSetIndex]).Get(cbs);

            Span<DescriptorImageInfo> imageInfos = stackalloc DescriptorImageInfo[key.Count];

            for (int i = 0; i < key.Count; i++)
            {
                imageInfos[i] = new DescriptorImageInfo()
                {
                    ImageLayout = ImageLayout.General,
                    ImageView = new ImageView(key.ImageHandles[i]),
                    Sampler = new Sampler(key.SamplerHandles[i])
                };
            }

            dsc.UpdateImages(0, 0, imageInfos, DescriptorType.CombinedImageSampler);

            return dsc;
        }

        private DescriptorSetCollection CreateImage(CommandBufferScoped cbs, ref HandleSet key)
        {
            var dsc = _gd.DescriptorSetManager.AllocateDescriptorSet(_gd.Api, _descriptorSetLayouts[PipelineBase.ImageSetIndex]).Get(cbs);

            Span<DescriptorImageInfo> imageInfos = stackalloc DescriptorImageInfo[key.Count];

            for (int i = 0; i < key.Count; i++)
            {
                imageInfos[i] = new DescriptorImageInfo()
                {
                    ImageLayout = ImageLayout.General,
                    ImageView = new ImageView(key.Handles[i])
                };
            }

            dsc.UpdateImages(0, 0, imageInfos, DescriptorType.StorageImage);

            return dsc;
        }

        private DescriptorSetCollection CreateBufferTexture(CommandBufferScoped cbs, ref HandleSet key)
        {
            return CreateBufferImage(cbs, ref key, PipelineBase.BufferTextureSetIndex, DescriptorType.UniformTexelBuffer);
        }

        private DescriptorSetCollection CreateBufferImage(CommandBufferScoped cbs, ref HandleSet key)
        {
            return CreateBufferImage(cbs, ref key, PipelineBase.BufferImageSetIndex, DescriptorType.StorageTexelBuffer);
        }

        private DescriptorSetCollection CreateBufferImage(CommandBufferScoped cbs, ref HandleSet key, int setIndex, DescriptorType type)
        {
            var dsc = _gd.DescriptorSetManager.AllocateDescriptorSet(_gd.Api, _descriptorSetLayouts[setIndex]).Get(cbs);

            Span<BufferView> texelBufferView = stackalloc BufferView[key.Count];

            for (int i = 0; i < key.Count; i++)
            {
                texelBufferView[i] = new BufferView(key.Handles[i]);
            }

            dsc.UpdateBufferImages(0, 0, texelBufferView, type);

            return dsc;
        }
    }
}
