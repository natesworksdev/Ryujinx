using Silk.NET.Vulkan;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineBlit : PipelineBase
    {
        public PipelineBlit(VulkanGraphicsDevice gd, Device device) : base(gd, device)
        {
        }

        protected override unsafe DescriptorSetLayout[] CreateDescriptorSetLayouts(VulkanGraphicsDevice gd, Device device, out PipelineLayout layout)
        {
            DescriptorSetLayoutBinding uLayoutBindings = new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            };

            DescriptorSetLayoutBinding tLayoutBindings = new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            };

            DescriptorSetLayout[] layouts = new DescriptorSetLayout[3];

            var uDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = &uLayoutBindings,
                BindingCount = 1
            };

            var sDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 0
            };

            var tDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = &tLayoutBindings,
                BindingCount = 1
            };

            gd.Api.CreateDescriptorSetLayout(device, uDescriptorSetLayoutCreateInfo, null, out layouts[0]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, sDescriptorSetLayoutCreateInfo, null, out layouts[1]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, tDescriptorSetLayoutCreateInfo, null, out layouts[2]).ThrowOnError();

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = 3
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return layouts;
        }

        public void SetRenderTarget(Auto<DisposableImageView> view, uint width, uint height, bool isDepthStencil, VkFormat format)
        {
            CreateFramebuffer(view, width, height, isDepthStencil, format);
            CreateRenderPass();
            SignalStateChange();
        }

        private void CreateFramebuffer(Auto<DisposableImageView> view, uint width, uint height, bool isDepthStencil, VkFormat format)
        {
            FramebufferParams = new FramebufferParams(Device, view, width, height, isDepthStencil, format);
            UpdatePipelineAttachmentFormats();
        }

        public void SetCommandBuffer(CommandBufferScoped cbs)
        {
            CommandBuffer = (Cbs = cbs).CommandBuffer;

            // Restore per-command buffer state.

            if (Pipeline != null)
            {
                Gd.Api.CmdBindPipeline(CommandBuffer, Pbp, Pipeline.Get(CurrentCommandBuffer).Value);
            }

            SignalCommandBufferChange();
        }

        public void Finish()
        {
            EndRenderPass();
        }
    }
}
