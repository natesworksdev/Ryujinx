using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    static class PipelineLayoutFactory
    {
        public static unsafe DescriptorSetLayout[] Create(
            VulkanGraphicsDevice gd,
            Device device,
            Shader[] shaders,
            out PipelineLayout layout)
        {
            bool isCompute = false;

            foreach (var shader in shaders)
            {
                if (shader.StageFlags == ShaderStageFlags.ShaderStageComputeBit)
                {
                    isCompute = true;
                    break;
                }
            }

            int uCount = shaders.Sum(x => x.Bindings.UniformBufferBindings.Count) + 1;
            int tCount = shaders.Sum(x => x.Bindings.TextureBindings.Count);
            int iCount = shaders.Sum(x => x.Bindings.ImageBindings.Count);
            int bTCount = shaders.Sum(x => x.Bindings.BufferTextureBindings.Count);
            int bICount = shaders.Sum(x => x.Bindings.BufferImageBindings.Count);

            int sArraysCount = shaders.Sum(x => x.Bindings.StorageBufferBindings.Count != 0 ? 1 : 0);

            DescriptorSetLayoutBinding* uLayoutBindings = stackalloc DescriptorSetLayoutBinding[uCount];
            DescriptorSetLayoutBinding* sLayoutBindings = stackalloc DescriptorSetLayoutBinding[sArraysCount];
            DescriptorSetLayoutBinding* tLayoutBindings = stackalloc DescriptorSetLayoutBinding[tCount];
            DescriptorSetLayoutBinding* iLayoutBindings = stackalloc DescriptorSetLayoutBinding[iCount];
            DescriptorSetLayoutBinding* bTLayoutBindings = stackalloc DescriptorSetLayoutBinding[bTCount];
            DescriptorSetLayoutBinding* bILayoutBindings = stackalloc DescriptorSetLayoutBinding[bICount];

            uLayoutBindings[0] = new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = isCompute ? ShaderStageFlags.ShaderStageComputeBit : ShaderStageFlags.ShaderStageFragmentBit
            };

            void InitializeBinding(
                DescriptorSetLayoutBinding* bindings,
                Func<ShaderBindings, IReadOnlyCollection<int>> selector,
                DescriptorType type,
                int start = 0)
            {
                int index = start;

                for (int stage = 0; stage < shaders.Length; stage++)
                {
                    var collection = selector(shaders[stage].Bindings);

                    foreach (var binding in collection)
                    {
                        bindings[index++] = new DescriptorSetLayoutBinding
                        {
                            Binding = (uint)binding,
                            DescriptorType = type,
                            DescriptorCount = 1,
                            StageFlags = shaders[stage].StageFlags
                        };
                    }
                }
            }

            void InitializeStorageBufferBinding(DescriptorSetLayoutBinding* bindings)
            {
                int index = 0;

                for (int stage = 0; stage < shaders.Length; stage++)
                {
                    var collection = shaders[stage].Bindings.StorageBufferBindings;

                    if (collection.Count != 0)
                    {
                        bindings[index++] = new DescriptorSetLayoutBinding
                        {
                            Binding = (uint)collection.First(),
                            DescriptorType = DescriptorType.StorageBuffer,
                            DescriptorCount = (uint)collection.Count,
                            StageFlags = shaders[stage].StageFlags
                        };
                    }
                }
            }

            InitializeBinding(uLayoutBindings, x => x.UniformBufferBindings, DescriptorType.UniformBuffer, 1);
            InitializeStorageBufferBinding(sLayoutBindings);
            InitializeBinding(tLayoutBindings, x => x.TextureBindings, DescriptorType.CombinedImageSampler);
            InitializeBinding(iLayoutBindings, x => x.ImageBindings, DescriptorType.StorageImage);
            InitializeBinding(bTLayoutBindings, x => x.BufferTextureBindings, DescriptorType.UniformTexelBuffer);
            InitializeBinding(bILayoutBindings, x => x.BufferImageBindings, DescriptorType.StorageTexelBuffer);

            DescriptorSetLayout[] allLayouts = new DescriptorSetLayout[PipelineBase.DescriptorSetLayouts];

            void InitializeDescriptorSetLayout(DescriptorSetLayoutBinding* pBindings, int count, out DescriptorSetLayout layout)
            {
                var createInfo = new DescriptorSetLayoutCreateInfo()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    PBindings = pBindings,
                    BindingCount = (uint)count
                };

                gd.Api.CreateDescriptorSetLayout(device, createInfo, null, out layout).ThrowOnError();
            }

            InitializeDescriptorSetLayout(uLayoutBindings, uCount, out allLayouts[PipelineBase.UniformSetIndex]);
            InitializeDescriptorSetLayout(sLayoutBindings, sArraysCount, out allLayouts[PipelineBase.StorageSetIndex]);
            InitializeDescriptorSetLayout(tLayoutBindings, tCount, out allLayouts[PipelineBase.TextureSetIndex]);
            InitializeDescriptorSetLayout(iLayoutBindings, iCount, out allLayouts[PipelineBase.ImageSetIndex]);
            InitializeDescriptorSetLayout(bTLayoutBindings, bTCount, out allLayouts[PipelineBase.BufferTextureSetIndex]);
            InitializeDescriptorSetLayout(bILayoutBindings, bICount, out allLayouts[PipelineBase.BufferImageSetIndex]);

            fixed (DescriptorSetLayout* pLayouts = allLayouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = PipelineBase.DescriptorSetLayouts
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return allLayouts;
        }

        private static int NonZero(int value)
        {
            return value != 0 ? 1 : 0;
        }
    }
}
