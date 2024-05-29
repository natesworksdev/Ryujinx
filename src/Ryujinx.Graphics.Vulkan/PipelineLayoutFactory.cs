using Ryujinx.Common.Memory;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Vulkan
{
    record struct ResourceLayouts(DescriptorSetLayout[] DescriptorSetLayouts, bool[] DescriptorSetLayoutsUpdateAfterBind, PipelineLayout PipelineLayout);

    static class PipelineLayoutFactory
    {
        private struct ResourceCounts
        {
            private Array5<int> _uniformBuffersCount;
            private Array5<int> _storageBuffersCount;
            private Array5<int> _texturesCount;
            private Array5<int> _imagesCount;
            private Array5<int> _samplersCount;
            private Array5<int> _totalCount;

            private void AddToStage(in ResourceDescriptor descriptor, int stageIndex)
            {
                switch (descriptor.Type)
                {
                    case ResourceType.UniformBuffer:
                        _uniformBuffersCount[stageIndex] += descriptor.Count;
                        break;
                    case ResourceType.StorageBuffer:
                        _storageBuffersCount[stageIndex] += descriptor.Count;
                        break;
                    case ResourceType.Texture:
                    case ResourceType.TextureAndSampler:
                    case ResourceType.BufferTexture:
                        _texturesCount[stageIndex] += descriptor.Count;
                        break;
                    case ResourceType.Image:
                    case ResourceType.BufferImage:
                        _imagesCount[stageIndex] += descriptor.Count;
                        break;
                    case ResourceType.Sampler:
                        _samplersCount[stageIndex] += descriptor.Count;
                        break;
                }

                _totalCount[stageIndex] += descriptor.Count;
            }

            public void Add(in ResourceDescriptor descriptor)
            {
                if (descriptor.Stages.HasFlag(ResourceStages.Vertex) || descriptor.Stages.HasFlag(ResourceStages.Compute))
                {
                    AddToStage(descriptor, 0);
                }

                if (descriptor.Stages.HasFlag(ResourceStages.TessellationControl))
                {
                    AddToStage(descriptor, 1);
                }

                if (descriptor.Stages.HasFlag(ResourceStages.TessellationEvaluation))
                {
                    AddToStage(descriptor, 2);
                }

                if (descriptor.Stages.HasFlag(ResourceStages.Geometry))
                {
                    AddToStage(descriptor, 3);
                }

                if (descriptor.Stages.HasFlag(ResourceStages.Fragment))
                {
                    AddToStage(descriptor, 4);
                }
            }

            private static int Sum(ReadOnlySpan<int> values)
            {
                int sum = 0;

                foreach (int value in values)
                {
                    sum += value;
                }

                return sum;
            }

            public bool IsExceedingAnyMaxLimit(VulkanRenderer gd)
            {
                int maxUniformBuffers = Sum(_uniformBuffersCount.AsSpan());
                int maxStorageBuffers = Sum(_storageBuffersCount.AsSpan());
                int maxTextures = Sum(_texturesCount.AsSpan());
                int maxImages = Sum(_imagesCount.AsSpan());
                int maxSamplers = Sum(_samplersCount.AsSpan());
                int maxTotal = Sum(_totalCount.AsSpan());

                return (uint)maxUniformBuffers > gd.Capabilities.MaxPerStageUniformBuffers ||
                    (uint)maxStorageBuffers > gd.Capabilities.MaxPerStageStorageBuffers ||
                    (uint)maxTextures > gd.Capabilities.MaxPerStageSampledImages ||
                    (uint)maxImages > gd.Capabilities.MaxPerStageStorageImages ||
                    (uint)maxSamplers > gd.Capabilities.MaxPerStageSamplers ||
                    (uint)maxTotal > gd.Capabilities.MaxPerStageResources;
            }
        }

        public static unsafe ResourceLayouts Create(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            bool usePushDescriptors)
        {
            DescriptorSetLayout[] layouts = new DescriptorSetLayout[setDescriptors.Count];
            bool[] updateAfterBindFlags = new bool[setDescriptors.Count];

            bool isMoltenVk = gd.IsMoltenVk;

            for (int setIndex = 0; setIndex < setDescriptors.Count; setIndex++)
            {
                ResourceDescriptorCollection rdc = setDescriptors[setIndex];

                ResourceCounts counts = new();
                ResourceStages activeStages = ResourceStages.None;

                if (isMoltenVk)
                {
                    for (int descIndex = 0; descIndex < rdc.Descriptors.Count; descIndex++)
                    {
                        activeStages |= rdc.Descriptors[descIndex].Stages;
                    }
                }

                DescriptorSetLayoutBinding[] layoutBindings = new DescriptorSetLayoutBinding[rdc.Descriptors.Count];

                for (int descIndex = 0; descIndex < rdc.Descriptors.Count; descIndex++)
                {
                    ResourceDescriptor descriptor = rdc.Descriptors[descIndex];
                    ResourceStages stages = descriptor.Stages;

                    if (descriptor.Type == ResourceType.StorageBuffer && isMoltenVk)
                    {
                        // There's a bug on MoltenVK where using the same buffer across different stages
                        // causes invalid resource errors, allow the binding on all active stages as workaround.
                        stages = activeStages;
                    }

                    layoutBindings[descIndex] = new DescriptorSetLayoutBinding
                    {
                        Binding = (uint)descriptor.Binding,
                        DescriptorType = descriptor.Type.Convert(),
                        DescriptorCount = (uint)descriptor.Count,
                        StageFlags = stages.Convert(),
                    };

                    counts.Add(descriptor);
                }

                fixed (DescriptorSetLayoutBinding* pLayoutBindings = layoutBindings)
                {
                    DescriptorSetLayoutCreateFlags flags = DescriptorSetLayoutCreateFlags.None;

                    if (usePushDescriptors && setIndex == 0)
                    {
                        flags = DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr;
                    }

                    if (counts.IsExceedingAnyMaxLimit(gd))
                    {
                        // Some vendors (like Intel) have low per-stage limits.
                        // We must set the flag if we exceed those limits.
                        flags |= DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit;

                        updateAfterBindFlags[setIndex] = true;
                    }

                    var descriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo
                    {
                        SType = StructureType.DescriptorSetLayoutCreateInfo,
                        PBindings = pLayoutBindings,
                        BindingCount = (uint)layoutBindings.Length,
                        Flags = flags,
                    };

                    gd.Api.CreateDescriptorSetLayout(device, descriptorSetLayoutCreateInfo, null, out layouts[setIndex]).ThrowOnError();
                }
            }

            PipelineLayout layout;

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = (uint)layouts.Length,
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return new ResourceLayouts(layouts, updateAfterBindFlags, layout);
        }
    }
}
