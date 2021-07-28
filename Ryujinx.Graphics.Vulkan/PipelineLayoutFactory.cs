using Silk.NET.Vulkan;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    static class PipelineLayoutFactory
    {
        public static unsafe DescriptorSetLayout[] Create(VulkanGraphicsDevice gd, Device device, uint stages, out PipelineLayout layout)
        {
            int stagesCount = BitOperations.PopCount(stages);

            int uCount = Constants.MaxUniformBuffersPerStage * stagesCount + 1;
            int tCount = Constants.MaxTexturesPerStage * stagesCount;
            int iCount = Constants.MaxImagesPerStage * stagesCount;
            int bTCount = tCount;
            int bICount = iCount;

            DescriptorSetLayoutBinding* uLayoutBindings = stackalloc DescriptorSetLayoutBinding[uCount];
            DescriptorSetLayoutBinding* sLayoutBindings = stackalloc DescriptorSetLayoutBinding[stagesCount];
            DescriptorSetLayoutBinding* tLayoutBindings = stackalloc DescriptorSetLayoutBinding[tCount];
            DescriptorSetLayoutBinding* iLayoutBindings = stackalloc DescriptorSetLayoutBinding[iCount];
            DescriptorSetLayoutBinding* bTLayoutBindings = stackalloc DescriptorSetLayoutBinding[bTCount];
            DescriptorSetLayoutBinding* bILayoutBindings = stackalloc DescriptorSetLayoutBinding[bICount];

            uLayoutBindings[0] = new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit | ShaderStageFlags.ShaderStageComputeBit
            };

            int iter = 0;

            while (stages != 0)
            {
                int stage = BitOperations.TrailingZeroCount(stages);
                stages &= ~(1u << stage);

                var stageFlags = stage switch
                {
                    1 => ShaderStageFlags.ShaderStageFragmentBit,
                    2 => ShaderStageFlags.ShaderStageGeometryBit,
                    3 => ShaderStageFlags.ShaderStageTessellationControlBit,
                    4 => ShaderStageFlags.ShaderStageTessellationEvaluationBit,
                    _ => ShaderStageFlags.ShaderStageVertexBit | ShaderStageFlags.ShaderStageComputeBit
                };

                void Set(DescriptorSetLayoutBinding* bindings, int maxPerStage, DescriptorType type, int start = 0)
                {
                    for (int i = 0; i < maxPerStage; i++)
                    {
                        bindings[start + iter * maxPerStage + i] = new DescriptorSetLayoutBinding
                        {
                            Binding = (uint)(start + stage * maxPerStage + i),
                            DescriptorType = type,
                            DescriptorCount = 1,
                            StageFlags = stageFlags
                        };
                    }
                }

                void SetStorage(DescriptorSetLayoutBinding* bindings, int maxPerStage, int start = 0)
                {
                    bindings[start + iter] = new DescriptorSetLayoutBinding
                    {
                        Binding = (uint)(start + stage * maxPerStage),
                        DescriptorType = DescriptorType.StorageBuffer,
                        DescriptorCount = (uint)maxPerStage,
                        StageFlags = stageFlags
                    };
                }

                Set(uLayoutBindings, Constants.MaxUniformBuffersPerStage, DescriptorType.UniformBuffer, 1);
                SetStorage(sLayoutBindings, Constants.MaxStorageBuffersPerStage);
                Set(tLayoutBindings, Constants.MaxTexturesPerStage, DescriptorType.CombinedImageSampler);
                Set(iLayoutBindings, Constants.MaxImagesPerStage, DescriptorType.StorageImage);
                Set(bTLayoutBindings, Constants.MaxTexturesPerStage, DescriptorType.UniformTexelBuffer);
                Set(bILayoutBindings, Constants.MaxImagesPerStage, DescriptorType.StorageTexelBuffer);

                iter++;
            }

            DescriptorSetLayout[] layouts = new DescriptorSetLayout[PipelineFull.DescriptorSetLayouts];

            var uDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = uLayoutBindings,
                BindingCount = (uint)uCount
            };

            var sDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = sLayoutBindings,
                BindingCount = (uint)stagesCount
            };

            var tDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = tLayoutBindings,
                BindingCount = (uint)tCount
            };

            var iDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = iLayoutBindings,
                BindingCount = (uint)iCount
            };

            var bTDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = bTLayoutBindings,
                BindingCount = (uint)bTCount
            };

            var bIDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = bILayoutBindings,
                BindingCount = (uint)bICount
            };

            gd.Api.CreateDescriptorSetLayout(device, uDescriptorSetLayoutCreateInfo, null, out layouts[PipelineFull.UniformSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, sDescriptorSetLayoutCreateInfo, null, out layouts[PipelineFull.StorageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, tDescriptorSetLayoutCreateInfo, null, out layouts[PipelineFull.TextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, iDescriptorSetLayoutCreateInfo, null, out layouts[PipelineFull.ImageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bTDescriptorSetLayoutCreateInfo, null, out layouts[PipelineFull.BufferTextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bIDescriptorSetLayoutCreateInfo, null, out layouts[PipelineFull.BufferImageSetIndex]).ThrowOnError();

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = PipelineFull.DescriptorSetLayouts
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return layouts;
        }
    }
}
