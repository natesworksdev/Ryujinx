using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Vulkan
{
    static class PipelineLayoutFactory
    {
        public static unsafe (DescriptorSetLayout[], PipelineLayout) Create(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            PipelineLayoutUsageInfo usageInfo)
        {
            DescriptorSetLayout[] layouts = new DescriptorSetLayout[setDescriptors.Count];

            bool isMoltenVk = gd.IsMoltenVk;

            for (int setIndex = 0; setIndex < setDescriptors.Count; setIndex++)
            {
                ResourceDescriptorCollection rdc = setDescriptors[setIndex];

                ResourceStages activeStages = ResourceStages.None;

                if (isMoltenVk)
                {
                    for (int descIndex = 0; descIndex < rdc.Descriptors.Count; descIndex++)
                    {
                        activeStages |= rdc.Descriptors[descIndex].Stages;
                    }
                }

                bool hasRuntimeArray = false;

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

                    uint count = (uint)descriptor.Count;

                    if (count == 0)
                    {
                        count = descriptor.Type == ResourceType.Sampler
                            ? usageInfo.BindlessSamplersCount
                            : usageInfo.BindlessTexturesCount;

                        hasRuntimeArray = true;
                    }

                    layoutBindings[descIndex] = new DescriptorSetLayoutBinding
                    {
                        Binding = (uint)descriptor.Binding,
                        DescriptorType = descriptor.Type.Convert(),
                        DescriptorCount = count,
                        StageFlags = stages.Convert(),
                    };
                }

                fixed (DescriptorSetLayoutBinding* pLayoutBindings = layoutBindings)
                {
                    var descriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo
                    {
                        SType = StructureType.DescriptorSetLayoutCreateInfo,
                        PBindings = pLayoutBindings,
                        BindingCount = (uint)layoutBindings.Length,
                        Flags = usageInfo.UsePushDescriptors && setIndex == 0 ? DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr : DescriptorSetLayoutCreateFlags.None,
                    };

                    if (hasRuntimeArray)
                    {
                        var bindingFlags = new DescriptorBindingFlags[rdc.Descriptors.Count];

                        for (int descIndex = 0; descIndex < rdc.Descriptors.Count; descIndex++)
                        {
                            if (rdc.Descriptors.Count == 0)
                            {
                                bindingFlags[descIndex] = DescriptorBindingFlags.UpdateAfterBindBit;
                            }
                        }

                        fixed (DescriptorBindingFlags* pBindingFlags = bindingFlags)
                        {
                            var descriptorSetLayoutFlagsCreateInfo = new DescriptorSetLayoutBindingFlagsCreateInfo()
                            {
                                SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                                PBindingFlags = pBindingFlags,
                                BindingCount = (uint)bindingFlags.Length,
                            };

                            descriptorSetLayoutCreateInfo.PNext = &descriptorSetLayoutFlagsCreateInfo;
                            descriptorSetLayoutCreateInfo.Flags |= DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit;

                            gd.Api.CreateDescriptorSetLayout(device, descriptorSetLayoutCreateInfo, null, out layouts[setIndex]).ThrowOnError();
                        }
                    }
                    else
                    {
                        gd.Api.CreateDescriptorSetLayout(device, descriptorSetLayoutCreateInfo, null, out layouts[setIndex]).ThrowOnError();
                    }
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

            return (layouts, layout);
        }
    }
}
