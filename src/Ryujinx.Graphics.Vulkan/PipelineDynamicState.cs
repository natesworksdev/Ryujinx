using Ryujinx.Common.Memory;
using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    struct PipelineDynamicState
    {
        private float _depthBiasSlopeFactor;
        private float _depthBiasConstantFactor;
        private float _depthBiasClamp;

        public int ScissorsCount;
        private Array16<Rect2D> _scissors;

        private uint _backCompareMask;
        private uint _backWriteMask;
        private uint _backReference;
        private uint _frontCompareMask;
        private uint _frontWriteMask;
        private uint _frontReference;
        
        private bool _opToo;
        private StencilOp _backfailop;
        private StencilOp _backpassop;
        private StencilOp _backdepthfailop;
        private CompareOp _backcompareop;
        private StencilOp _frontfailop;
        private StencilOp _frontpassop;
        private StencilOp _frontdepthfailop;
        private CompareOp _frontcompareop;

        private float _linewidth;
        
        public bool _stencilTestEnable;

        public bool _depthtestEnable;
        public bool _depthwriteEnable;
        private CompareOp _depthCompareOp;

        public PrimitiveTopology Topology;

        private Array4<float> _blendConstants;

        public uint ViewportsCount;
        public Array16<Viewport> Viewports;

        public CullModeFlags CullMode;
        public FrontFace FrontFace;

        [Flags]
        private enum DirtyFlags
        {
            None = 0,
            Blend = 1 << 0,
            DepthBias = 1 << 1,
            Scissor = 1 << 2,
            Stencil = 1 << 3,
            Viewport = 1 << 4,
            CullMode = 1 << 5,
            FrontFace = 1 << 6,
            DepthTestBool = 1 << 7, 
            DepthTestCompareOp = 1 << 8,
            StencilTestEnable = 1 << 9,
            Toplogy = 1 << 10,
            LineWidth = 1 << 11,
            Standard = Blend | DepthBias | Scissor | Stencil | Viewport | LineWidth,
            Extended = CullMode | FrontFace | DepthTestBool | DepthTestCompareOp | StencilTestEnable | Toplogy,
        }

        private DirtyFlags _dirty;

        public void SetBlendConstants(float r, float g, float b, float a)
        {
            _blendConstants[0] = r;
            _blendConstants[1] = g;
            _blendConstants[2] = b;
            _blendConstants[3] = a;

            _dirty |= DirtyFlags.Blend;
        }

        public void SetDepthBias(float slopeFactor, float constantFactor, float clamp)
        {
            _depthBiasSlopeFactor = slopeFactor;
            _depthBiasConstantFactor = constantFactor;
            _depthBiasClamp = clamp;

            _dirty |= DirtyFlags.DepthBias;
        }

        public void SetScissor(int index, Rect2D scissor)
        {
            _scissors[index] = scissor;

            _dirty |= DirtyFlags.Scissor;
        }

        public void SetDepthTestBool(bool testEnable, bool writeEnable)
        {
            _depthtestEnable = testEnable;
            _depthwriteEnable = writeEnable;

            _dirty |= DirtyFlags.DepthTestBool;
        }
        
        public void SetDepthTestCompareOp(CompareOp depthTestOp)
        {
            _depthCompareOp = depthTestOp;

            _dirty |= DirtyFlags.DepthTestCompareOp;
        }
        
        public void SetPrimitiveTopology(PrimitiveTopology topology)
        {
            Topology = topology;

            _dirty |= DirtyFlags.Toplogy;
        }
        
        public void SetStencilOp(StencilOp backFailOp,
            StencilOp backPassOp,
            StencilOp backDepthFailOp,
            CompareOp backCompareOp,
            StencilOp frontFailOp,
            StencilOp frontPassOp,
            StencilOp frontDepthFailOp,
            CompareOp frontCompareOp)
        {
            _backfailop = backFailOp;
            _backpassop = backPassOp;
            _backdepthfailop = backDepthFailOp;
            _backcompareop = backCompareOp;

            _frontfailop = frontFailOp;
            _frontpassop = frontPassOp;
            _frontdepthfailop = frontDepthFailOp;
            _frontcompareop = frontCompareOp;
            
            _opToo = true;
        }
        
        public void SetStencilMask(
            uint backCompareMask,
            uint backWriteMask,
            uint backReference,
            uint frontCompareMask,
            uint frontWriteMask,
            uint frontReference)
        {
            _backCompareMask = backCompareMask;
            _backWriteMask = backWriteMask;
            _backReference = backReference;
            _frontCompareMask = frontCompareMask;
            _frontWriteMask = frontWriteMask;
            _frontReference = frontReference;

            _dirty |= DirtyFlags.Stencil;
        }
        
        public void SetStencilTest(bool stencilTestEnable)
        {
            _stencilTestEnable = stencilTestEnable;
            
            _dirty |= DirtyFlags.StencilTestEnable;
        }


        public void SetViewport(int index, Viewport viewport)
        {
            Viewports[index] = viewport;

            _dirty |= DirtyFlags.Viewport;
        }

        public void SetViewports(ref Array16<Viewport> viewports, uint viewportsCount)
        {
            Viewports = viewports;
            ViewportsCount = viewportsCount;

            if (ViewportsCount != 0)
            {
                _dirty |= DirtyFlags.Viewport;
            }
        }
        
        public void SetCullMode(CullModeFlags cullMode)
        {
            CullMode = cullMode;
            
            _dirty |= DirtyFlags.CullMode;
        }
        
        public void SetFrontFace(FrontFace frontFace)
        {
            FrontFace = frontFace;
            
            _dirty |= DirtyFlags.FrontFace;
        }
        
        public void SetLineWidth(float width)
        {
            _linewidth = width;
            
            _dirty |= DirtyFlags.LineWidth;
        }

        public void ForceAllDirty(VulkanRenderer gd)
        {
            _dirty = DirtyFlags.Standard;

            if (gd.Capabilities.SupportsExtendedDynamicState)
            {
                _dirty = DirtyFlags.Standard | DirtyFlags.Extended;
            }

            if (gd.IsMoltenVk)
            {
                _dirty &= ~DirtyFlags.LineWidth;
            }
        }

        public void ReplayIfDirty(Vk api, CommandBuffer commandBuffer)
        {
            if (_dirty.HasFlag(DirtyFlags.Blend))
            {
                RecordBlend(api, commandBuffer);
            }

            if (_dirty.HasFlag(DirtyFlags.DepthBias))
            {
                RecordDepthBias(api, commandBuffer);
            }

            if (_dirty.HasFlag(DirtyFlags.Scissor))
            {
                RecordScissor(api, commandBuffer);
            }

            if (_dirty.HasFlag(DirtyFlags.Stencil))
            {
                RecordStencil(api, commandBuffer);
            }

            if (_dirty.HasFlag(DirtyFlags.Viewport))
            {
                RecordViewport(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.CullMode))
            {
                RecordCullMode(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.FrontFace))
            {
                RecordFrontFace(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.DepthTestBool))
            {
                RecordDepthTestBool(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.DepthTestCompareOp))
            {
                RecordDepthTestCompareOp(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.StencilTestEnable))
            {
                RecordStencilTestEnable(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.Toplogy))
            {
                RecordPrimitiveTopology(api, commandBuffer);
            }
            
            if (_dirty.HasFlag(DirtyFlags.LineWidth))
            {
                RecordLineWidth(api, commandBuffer);
            }

            _dirty = DirtyFlags.None;
        }

        private void RecordBlend(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetBlendConstants(commandBuffer, _blendConstants.AsSpan());
        }

        private readonly void RecordDepthBias(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetDepthBias(commandBuffer, _depthBiasConstantFactor, _depthBiasClamp, _depthBiasSlopeFactor);
        }

        private void RecordScissor(Vk api, CommandBuffer commandBuffer)
        {
            if (ScissorsCount != 0)
            {
                api.CmdSetScissor(commandBuffer, 0, (uint)ScissorsCount, _scissors.AsSpan());
            }
        }
        
        private readonly void RecordStencil(Vk api, CommandBuffer commandBuffer)
        {
            if (_opToo)
            {
                api.CmdSetStencilOp(commandBuffer, StencilFaceFlags.FaceBackBit, _backfailop, _backpassop,
                    _backdepthfailop, _backcompareop);
                api.CmdSetStencilOp(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontfailop, _frontpassop,
                    _frontdepthfailop, _frontcompareop);
            }
            
            api.CmdSetStencilCompareMask(commandBuffer, StencilFaceFlags.FaceBackBit, _backCompareMask);
            api.CmdSetStencilWriteMask(commandBuffer, StencilFaceFlags.FaceBackBit, _backWriteMask);
            api.CmdSetStencilReference(commandBuffer, StencilFaceFlags.FaceBackBit, _backReference);
            api.CmdSetStencilCompareMask(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontCompareMask);
            api.CmdSetStencilWriteMask(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontWriteMask);
            api.CmdSetStencilReference(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontReference);
        }
        
        private readonly void RecordStencilTestEnable(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetStencilTestEnable(commandBuffer, _stencilTestEnable);
        }
        
        private void RecordViewport(Vk api, CommandBuffer commandBuffer)
        {
            if (ViewportsCount != 0)
            {
                api.CmdSetViewport(commandBuffer, 0, ViewportsCount, Viewports.AsSpan());
            }
        }
        
        private void RecordCullMode(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetCullMode(commandBuffer, CullMode);
        }
        
        private void RecordFrontFace(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetFrontFace(commandBuffer, FrontFace);
        }
        
        private void RecordDepthTestBool(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetDepthTestEnable(commandBuffer, _depthtestEnable);
            api.CmdSetDepthWriteEnable(commandBuffer, _depthwriteEnable);
        }
        
        private void RecordDepthTestCompareOp(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetDepthCompareOp(commandBuffer, _depthCompareOp);
        }
        
        private void RecordPrimitiveTopology(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetPrimitiveTopology(commandBuffer, Topology);
        }
        
        private void RecordLineWidth(Vk api, CommandBuffer commandBuffer)
        {
            if (!OperatingSystem.IsMacOS())
            {
                api.CmdSetLineWidth(commandBuffer, _linewidth);
            }
        }
    }
}
