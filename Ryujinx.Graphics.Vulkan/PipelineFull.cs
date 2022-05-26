using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Vulkan.Queries;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineFull : PipelineBase, IPipeline
    {
        private bool _hasPendingQuery;

        private readonly List<QueryPool> _activeQueries;
        private CounterQueueEvent _activeConditionalRender;

        private readonly List<BufferedQuery> _pendingQueryCopies;
        private readonly List<BufferedQuery> _pendingQueryResets;

        public PipelineFull(VulkanGraphicsDevice gd, Device device) : base(gd, device)
        {
            _activeQueries = new List<QueryPool>();
            _pendingQueryCopies = new();
            _pendingQueryResets = new List<BufferedQuery>();

            CommandBuffer = (Cbs = gd.CommandBufferPool.Rent()).CommandBuffer;
        }

        private void CopyPendingQuery()
        {
            foreach (var query in _pendingQueryCopies)
            {
                query.PoolCopy(Cbs);
            }

            lock (_pendingQueryResets)
            {
                foreach (var query in _pendingQueryResets)
                {
                    query.PoolReset(CommandBuffer);
                }

                _pendingQueryResets.Clear();
            }

            _pendingQueryCopies.Clear();
        }

        protected override unsafe DescriptorSetLayout[] CreateDescriptorSetLayouts(VulkanGraphicsDevice gd, Device device, out PipelineLayout layout)
        {
            DescriptorSetLayoutBinding* uLayoutBindings = stackalloc DescriptorSetLayoutBinding[Constants.MaxUniformBufferBindings];
            DescriptorSetLayoutBinding* sLayoutBindings = stackalloc DescriptorSetLayoutBinding[Constants.MaxStorageBufferBindings];
            DescriptorSetLayoutBinding* tLayoutBindings = stackalloc DescriptorSetLayoutBinding[Constants.MaxTextureBindings];
            DescriptorSetLayoutBinding* iLayoutBindings = stackalloc DescriptorSetLayoutBinding[Constants.MaxImageBindings];
            DescriptorSetLayoutBinding* bTLayoutBindings = stackalloc DescriptorSetLayoutBinding[Constants.MaxTextureBindings];
            DescriptorSetLayoutBinding* bILayoutBindings = stackalloc DescriptorSetLayoutBinding[Constants.MaxImageBindings];

            DescriptorBindingFlags* pUBindingsFlags = stackalloc DescriptorBindingFlags[Constants.MaxUniformBufferBindings];
            DescriptorBindingFlags* pSBindingsFlags = stackalloc DescriptorBindingFlags[Constants.MaxStorageBufferBindings];
            DescriptorBindingFlags* pTBindingsFlags = stackalloc DescriptorBindingFlags[Constants.MaxTextureBindings];
            DescriptorBindingFlags* pIBindingsFlags = stackalloc DescriptorBindingFlags[Constants.MaxImageBindings];
            DescriptorBindingFlags* pBTBindingsFlags = stackalloc DescriptorBindingFlags[Constants.MaxTextureBindings];
            DescriptorBindingFlags* pBIBindingsFlags = stackalloc DescriptorBindingFlags[Constants.MaxImageBindings];

            static DescriptorSetLayoutBindingFlagsCreateInfo CreateFlagsInfo(DescriptorBindingFlags* pBindingFlags, uint count)
            {
                return new DescriptorSetLayoutBindingFlagsCreateInfo()
                {
                    SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                    PBindingFlags = pBindingFlags,
                    BindingCount = count
                };
            }

            var uLayoutBindingFlags = CreateFlagsInfo(pUBindingsFlags, Constants.MaxUniformBufferBindings);
            var sLayoutBindingFlags = CreateFlagsInfo(pSBindingsFlags, Constants.MaxStorageBufferBindings);
            var tLayoutBindingFlags = CreateFlagsInfo(pTBindingsFlags, Constants.MaxTextureBindings);
            var iLayoutBindingFlags = CreateFlagsInfo(pIBindingsFlags, Constants.MaxImageBindings);
            var bTLayoutBindingFlags = CreateFlagsInfo(pBTBindingsFlags, Constants.MaxTextureBindings);
            var bILayoutBindingFlags = CreateFlagsInfo(pBIBindingsFlags, Constants.MaxImageBindings);

            for (int stage = 0; stage < Constants.MaxShaderStages; stage++)
            {
                var stageFlags = (ShaderStageFlags)(1 << stage);

                if (stage == 0)
                {
                    stageFlags |= ShaderStageFlags.ShaderStageComputeBit;
                }

                void Set(
                    DescriptorSetLayoutBinding* bindings,
                    DescriptorSetLayoutBindingFlagsCreateInfo bindingFlagsCreateInfo,
                    int maxPerStage,
                    DescriptorType type)
                {
                    for (int i = 0; i < maxPerStage; i++)
                    {
                        int j = stage * maxPerStage + i;

                        bindings[j] = new DescriptorSetLayoutBinding
                        {
                            Binding = (uint)j,
                            DescriptorType = type,
                            DescriptorCount = 1,
                            StageFlags = stageFlags
                        };

                        bindingFlagsCreateInfo.PBindingFlags[j] = DescriptorBindingFlags.DescriptorBindingPartiallyBoundBit;
                    }
                }

                Set(uLayoutBindings, uLayoutBindingFlags, Constants.MaxUniformBuffersPerStage, DescriptorType.UniformBuffer);
                Set(sLayoutBindings, sLayoutBindingFlags, Constants.MaxStorageBuffersPerStage, DescriptorType.StorageBuffer);
                Set(tLayoutBindings, tLayoutBindingFlags, Constants.MaxTexturesPerStage, DescriptorType.CombinedImageSampler);
                Set(iLayoutBindings, iLayoutBindingFlags, Constants.MaxImagesPerStage, DescriptorType.StorageImage);
                Set(bTLayoutBindings, bTLayoutBindingFlags, Constants.MaxTexturesPerStage, DescriptorType.UniformTexelBuffer);
                Set(bILayoutBindings, bILayoutBindingFlags, Constants.MaxImagesPerStage, DescriptorType.StorageTexelBuffer);
            }

            DescriptorSetLayout[] layouts = new DescriptorSetLayout[DescriptorSetLayouts];

            var uDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = uLayoutBindings,
                BindingCount = Constants.MaxUniformBufferBindings
            };

            var sDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = sLayoutBindings,
                BindingCount = Constants.MaxStorageBufferBindings
            };

            var tDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = tLayoutBindings,
                BindingCount = Constants.MaxTextureBindings
            };

            var iDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = iLayoutBindings,
                BindingCount = Constants.MaxImageBindings
            };

            var bTDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = bTLayoutBindings,
                BindingCount = Constants.MaxTextureBindings
            };

            var bIDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = bILayoutBindings,
                BindingCount = Constants.MaxImageBindings
            };

            gd.Api.CreateDescriptorSetLayout(device, uDescriptorSetLayoutCreateInfo, null, out layouts[UniformSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, sDescriptorSetLayoutCreateInfo, null, out layouts[StorageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, tDescriptorSetLayoutCreateInfo, null, out layouts[TextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, iDescriptorSetLayoutCreateInfo, null, out layouts[ImageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bTDescriptorSetLayoutCreateInfo, null, out layouts[BufferTextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bIDescriptorSetLayoutCreateInfo, null, out layouts[BufferImageSetIndex]).ThrowOnError();

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = DescriptorSetLayouts
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return layouts;
        }

        public void ClearRenderTargetColor(int index, uint componentMask, ColorF color)
        {
            if (FramebufferParams == null)
            {
                return;
            }

            if (componentMask != 0xf)
            {
                // We can't use CmdClearAttachments if not writing all components,
                // because on Vulkan, the pipeline state does not affect clears.
                var dstTexture = FramebufferParams.GetAttachment(index);
                if (dstTexture == null)
                {
                    return;
                }

                Span<float> clearColor = stackalloc float[4];
                clearColor[0] = color.Red;
                clearColor[1] = color.Green;
                clearColor[2] = color.Blue;
                clearColor[3] = color.Alpha;

                Gd.HelperShader.Clear(
                    Gd,
                    dstTexture,
                    clearColor,
                    componentMask,
                    (int)FramebufferParams.Width,
                    (int)FramebufferParams.Height,
                    FramebufferParams.AttachmentFormats[index],
                    ClearScissor);
            }
            else
            {
                ClearRenderTargetColor(index, color);
            }
        }

        public void EndHostConditionalRendering()
        {
            if (Gd.Capabilities.SupportsConditionalRendering)
            {
                // Gd.ConditionalRenderingApi.CmdEndConditionalRendering(CommandBuffer);
            }
            else
            {
                // throw new NotSupportedException();
            }

            _activeConditionalRender?.ReleaseHostAccess();
            _activeConditionalRender = null;
        }

        public bool TryHostConditionalRendering(ICounterEvent value, ulong compare, bool isEqual)
        {
            // Compare an event and a constant value.
            if (value is CounterQueueEvent evt)
            {
                // Easy host conditional rendering when the check matches what GL can do:
                //  - Event is of type samples passed.
                //  - Result is not a combination of multiple queries.
                //  - Comparing against 0.
                //  - Event has not already been flushed.

                if (compare == 0 && evt.Type == CounterType.SamplesPassed && evt.ClearCounter)
                {
                    if (!value.ReserveForHostAccess())
                    {
                        // If the event has been flushed, then just use the values on the CPU.
                        // The query object may already be repurposed for another draw (eg. begin + end).
                        return false;
                    }

                    if (Gd.Capabilities.SupportsConditionalRendering)
                    {
                        var buffer = evt.GetBuffer().Get(Cbs, 0, sizeof(long)).Value;
                        var flags = isEqual ? ConditionalRenderingFlagsEXT.ConditionalRenderingInvertedBitExt : 0;

                        var conditionalRenderingBeginInfo = new ConditionalRenderingBeginInfoEXT()
                        {
                            SType = StructureType.ConditionalRenderingBeginInfoExt,
                            Buffer = buffer,
                            Flags = flags
                        };

                        // Gd.ConditionalRenderingApi.CmdBeginConditionalRendering(CommandBuffer, conditionalRenderingBeginInfo);
                    }

                    _activeConditionalRender = evt;
                    return true;
                }
            }

            // The GPU will flush the queries to CPU and evaluate the condition there instead.

            FlushPendingQuery(); // The thread will be stalled manually flushing the counter, so flush commands now.
            return false;
        }

        public bool TryHostConditionalRendering(ICounterEvent value, ICounterEvent compare, bool isEqual)
        {
            FlushPendingQuery(); // The thread will be stalled manually flushing the counter, so flush commands now.
            return false;
        }

        private void FlushPendingQuery()
        {
            if (_hasPendingQuery)
            {
                _hasPendingQuery = false;
                FlushCommandsImpl();
            }
        }

        public CommandBufferScoped GetPreloadCommandBuffer()
        {
            if (PreloadCbs == null)
            {
                PreloadCbs = Gd.CommandBufferPool.Rent();
            }

            return PreloadCbs.Value;
        }

        public void FlushCommandsImpl([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            // System.Console.WriteLine("flush by " + caller);

            EndRenderPass();

            foreach (var queryPool in _activeQueries)
            {
                Gd.Api.CmdEndQuery(CommandBuffer, queryPool, 0);
            }

            if (PreloadCbs != null)
            {
                PreloadCbs.Value.Dispose();
                PreloadCbs = null;
            }

            CommandBuffer = (Cbs = Gd.CommandBufferPool.ReturnAndRent(Cbs)).CommandBuffer;

            // Restore per-command buffer state.

            if (Pipeline != null)
            {
                Gd.Api.CmdBindPipeline(CommandBuffer, Pbp, Pipeline.Get(Cbs).Value);
            }

            foreach (var queryPool in _activeQueries)
            {
                Gd.Api.CmdResetQueryPool(CommandBuffer, queryPool, 0, 1);
                Gd.Api.CmdBeginQuery(CommandBuffer, queryPool, 0, 0);
            }

            SignalCommandBufferChange();
        }

        public void BeginQuery(BufferedQuery query, QueryPool pool, bool needsReset)
        {
            if (needsReset)
            {
                EndRenderPass();

                Gd.Api.CmdResetQueryPool(CommandBuffer, pool, 0, 1);

                lock (_pendingQueryResets)
                {
                    _pendingQueryResets.Remove(query); // Might be present on here.
                }
            }

            Gd.Api.CmdBeginQuery(CommandBuffer, pool, 0, 0);

            _activeQueries.Add(pool);
        }

        public void EndQuery(QueryPool pool)
        {
            Gd.Api.CmdEndQuery(CommandBuffer, pool, 0);

            _activeQueries.Remove(pool);
        }

        public void ResetQuery(BufferedQuery query)
        {
            lock (_pendingQueryResets)
            {
                _pendingQueryResets.Add(query);
            }
        }

        public void CopyQueryResults(BufferedQuery query)
        {
            _pendingQueryCopies.Add(query);

            _hasPendingQuery = true;
        }

        protected override void SignalAttachmentChange()
        {
            FlushPendingQuery();
        }

        protected override void SignalRenderPassEnd()
        {
            CopyPendingQuery();
        }
    }
}
