using Ryujinx.Common;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Graphics3d
{
    class NvGpuEngine3d : INvGpuEngine
    {
        private const int RenderTargetsCount = 8;

        public int[] Registers { get; private set; }

        private NvGpu Gpu;

        private Dictionary<int, NvGpuMethod> Methods;

        private struct ConstBuffer
        {
            public bool Enabled;
            public long Position;
            public int  Size;
        }

        private ConstBuffer[][] ConstBuffers;

        [Flags]
        private enum PipelineState
        {
            Depth       = 1 << 0,
            Stencil     = 1 << 1,
            Blend       = 1 << 2,
            ColorMask   = 1 << 3,
            PrimRestart = 1 << 4
        }

        private PipelineState DirtyState;

        private int CurrentInstance = 0;

        public NvGpuEngine3d(NvGpu Gpu)
        {
            this.Gpu = Gpu;

            Registers = new int[0xe00];

            Methods = new Dictionary<int, NvGpuMethod>();

            void AddMethod(int Meth, int Count, int Stride, NvGpuMethod Method)
            {
                while (Count-- > 0)
                {
                    Methods.Add(Meth, Method);

                    Meth += Stride;
                }
            }

            AddMethod(0x585,  1, 1, VertexEndGl);
            AddMethod(0x674,  1, 1, ClearBuffers);
            AddMethod(0x6c3,  1, 1, QueryControl);
            AddMethod(0x8e4, 16, 1, CbData);
            AddMethod(0x904,  5, 8, CbBind);

            AddMethod((int)NvGpuEngine3dReg.DepthTestEnable,   1, 1, SetDepth);
            AddMethod((int)NvGpuEngine3dReg.DepthWriteEnable,  1, 1, SetDepth);
            AddMethod((int)NvGpuEngine3dReg.DepthTestFunction, 1, 1, SetDepth);
            AddMethod((int)NvGpuEngine3dReg.DepthRangeNNear,   1, 1, SetDepth);
            AddMethod((int)NvGpuEngine3dReg.DepthRangeNFar,    1, 1, SetDepth);

            AddMethod((int)NvGpuEngine3dReg.StencilEnable,        1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackFuncFunc,  1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackFuncRef,   1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackFuncMask,  1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackOpFail,    1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackOpZFail,   1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackOpZPass,   1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilBackMask,      1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontFuncFunc, 1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontFuncRef,  1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontFuncMask, 1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontOpFail,   1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontOpZFail,  1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontOpZPass,  1, 1, SetStencil);
            AddMethod((int)NvGpuEngine3dReg.StencilFrontMask,     1, 1, SetStencil);

            AddMethod((int)NvGpuEngine3dReg.BlendIndependent,     1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNEnable,        8, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNSeparateAlpha, 8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNEquationRgb,   8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNFuncSrcRgb,    8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNFuncDstRgb,    8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNEquationAlpha, 8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNFuncSrcAlpha,  8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.IBlendNFuncDstAlpha,  8, 8, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendSeparateAlpha,   1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendEquationRgb,     1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendFuncSrcRgb,      1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendFuncDstRgb,      1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendEquationAlpha,   1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendFuncSrcAlpha,    1, 1, SetBlend);
            AddMethod((int)NvGpuEngine3dReg.BlendFuncDstAlpha,    1, 1, SetBlend);

            AddMethod((int)NvGpuEngine3dReg.ColorMaskCommon, 1, 1, SetColorMask);
            AddMethod((int)NvGpuEngine3dReg.ColorMaskN,      8, 1, SetColorMask);

            AddMethod((int)NvGpuEngine3dReg.PrimRestartEnable, 1, 1, SetPrimRestart);
            AddMethod((int)NvGpuEngine3dReg.PrimRestartIndex,  1, 1, SetPrimRestart);

            AddMethod((int)NvGpuEngine3dReg.FrameBufferSrgb, 1, 1, SetFramebufferSrgb);

            ConstBuffers = new ConstBuffer[6][];

            for (int Index = 0; Index < ConstBuffers.Length; Index++)
            {
                ConstBuffers[Index] = new ConstBuffer[18];
            }

            //Ensure that all components are enabled by default.
            //FIXME: Is this correct?
            WriteRegister(NvGpuEngine3dReg.ColorMaskN, 0x1111);

            Gpu.Renderer.RenderTarget.FramebufferSrgb = true;

            for (int Index = 0; Index < RenderTargetsCount; Index++)
            {
                WriteRegister(NvGpuEngine3dReg.IBlendNEquationRgb   + Index * 8, (int)GalBlendEquation.FuncAdd);
                WriteRegister(NvGpuEngine3dReg.IBlendNFuncSrcRgb    + Index * 8, (int)GalBlendFactor.One);
                WriteRegister(NvGpuEngine3dReg.IBlendNFuncDstRgb    + Index * 8, (int)GalBlendFactor.Zero);
                WriteRegister(NvGpuEngine3dReg.IBlendNEquationAlpha + Index * 8, (int)GalBlendEquation.FuncAdd);
                WriteRegister(NvGpuEngine3dReg.IBlendNFuncSrcAlpha  + Index * 8, (int)GalBlendFactor.One);
                WriteRegister(NvGpuEngine3dReg.IBlendNFuncDstAlpha  + Index * 8, (int)GalBlendFactor.Zero);
            }
        }

        public void CallMethod(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            if (Methods.TryGetValue(MethCall.Method, out NvGpuMethod Method))
            {
                Method(Vmm, MethCall);
            }
            else
            {
                WriteRegister(MethCall);
            }
        }

        private void VertexEndGl(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            LockCaches();

            if ((DirtyState & PipelineState.Depth) == 0)
            {
                DirtyState |= PipelineState.Depth;

                UpdateDepth();
            }

            if ((DirtyState & PipelineState.Stencil) == 0)
            {
                DirtyState |= PipelineState.Stencil;

                UpdateStencil();
            }

            if ((DirtyState & PipelineState.Blend) == 0)
            {
                DirtyState |= PipelineState.Blend;

                UpdateBlend();
            }

            if ((DirtyState & PipelineState.ColorMask) == 0)
            {
                DirtyState |= PipelineState.ColorMask;

                UpdateColorMask();
            }

            if ((DirtyState & PipelineState.PrimRestart) == 0)
            {
                DirtyState |= PipelineState.PrimRestart;

                UpdatePrimRestart();
            }

            for (int FbIndex = 0; FbIndex < 8; FbIndex++)
            {
                SetFrameBuffer(Vmm, FbIndex);
            }

            SetZeta(Vmm);

            SetRenderTargets();

            long[] Keys = UploadShaders(Vmm);

            Gpu.Renderer.Shader.BindProgram();

            UploadTextures(Vmm, Keys);
            UploadConstBuffers(Vmm, Keys);
            UploadVertexArrays(Vmm);

            DispatchRender(Vmm);

            UnlockCaches();
        }

        private void LockCaches()
        {
            Gpu.Renderer.Buffer.LockCache();
            Gpu.Renderer.Rasterizer.LockCaches();
            Gpu.Renderer.Texture.LockCache();
        }

        private void UnlockCaches()
        {
            Gpu.Renderer.Buffer.UnlockCache();
            Gpu.Renderer.Rasterizer.UnlockCaches();
            Gpu.Renderer.Texture.UnlockCache();
        }

        private void ClearBuffers(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            int Attachment = (MethCall.Argument >> 6) & 0xf;

            GalClearBufferFlags Flags = (GalClearBufferFlags)(MethCall.Argument & 0x3f);

            float Red   = ReadRegisterFloat(NvGpuEngine3dReg.ClearNColor + 0);
            float Green = ReadRegisterFloat(NvGpuEngine3dReg.ClearNColor + 1);
            float Blue  = ReadRegisterFloat(NvGpuEngine3dReg.ClearNColor + 2);
            float Alpha = ReadRegisterFloat(NvGpuEngine3dReg.ClearNColor + 3);

            float Depth = ReadRegisterFloat(NvGpuEngine3dReg.ClearDepth);

            int Stencil = ReadRegister(NvGpuEngine3dReg.ClearStencil);

            SetFrameBuffer(Vmm, Attachment);

            SetZeta(Vmm);

            SetRenderTargets();

            Gpu.Renderer.RenderTarget.Bind();

            Gpu.Renderer.Rasterizer.ClearBuffers(Flags, Attachment, Red, Green, Blue, Alpha, Depth, Stencil);

            DirtyState &= ~PipelineState.Depth;
            DirtyState &= ~PipelineState.ColorMask;
        }

        private void SetFrameBuffer(NvGpuVmm Vmm, int FbIndex)
        {
            long VA = MakeInt64From2xInt32(NvGpuEngine3dReg.FrameBufferNAddress + FbIndex * 0x10);

            int SurfFormat = ReadRegister(NvGpuEngine3dReg.FrameBufferNFormat + FbIndex * 0x10);

            if (VA == 0 || SurfFormat == 0)
            {
                Gpu.Renderer.RenderTarget.UnbindColor(FbIndex);

                return;
            }

            long Key = Vmm.GetPhysicalAddress(VA);

            int Width  = ReadRegister(NvGpuEngine3dReg.FrameBufferNWidth  + FbIndex * 0x10);
            int Height = ReadRegister(NvGpuEngine3dReg.FrameBufferNHeight + FbIndex * 0x10);

            int BlockDim = ReadRegister(NvGpuEngine3dReg.FrameBufferNBlockDim + FbIndex * 0x10);

            int GobBlockHeight = 1 << ((BlockDim >> 4) & 7);

            GalMemoryLayout Layout = (GalMemoryLayout)((BlockDim >> 12) & 1);

            float TX = ReadRegisterFloat(NvGpuEngine3dReg.ViewportNTranslateX + FbIndex * 8);
            float TY = ReadRegisterFloat(NvGpuEngine3dReg.ViewportNTranslateY + FbIndex * 8);

            float SX = ReadRegisterFloat(NvGpuEngine3dReg.ViewportNScaleX + FbIndex * 8);
            float SY = ReadRegisterFloat(NvGpuEngine3dReg.ViewportNScaleY + FbIndex * 8);

            int VpX = (int)MathF.Max(0, TX - MathF.Abs(SX));
            int VpY = (int)MathF.Max(0, TY - MathF.Abs(SY));

            int VpW = (int)(TX + MathF.Abs(SX)) - VpX;
            int VpH = (int)(TY + MathF.Abs(SY)) - VpY;

            GalImageFormat Format = ImageUtils.ConvertSurface((GalSurfaceFormat)SurfFormat);

            GalImage Image = new GalImage(Width, Height, 1, GobBlockHeight, Layout, Format);

            Gpu.ResourceManager.SendColorBuffer(Vmm, Key, FbIndex, Image);

            Gpu.Renderer.RenderTarget.SetViewport(FbIndex, VpX, VpY, VpW, VpH);
        }

        private void SetZeta(NvGpuVmm Vmm)
        {
            long VA = MakeInt64From2xInt32(NvGpuEngine3dReg.ZetaAddress);

            int ZetaFormat = ReadRegister(NvGpuEngine3dReg.ZetaFormat);

            int BlockDim = ReadRegister(NvGpuEngine3dReg.ZetaBlockDimensions);

            int GobBlockHeight = 1 << ((BlockDim >> 4) & 7);

            GalMemoryLayout Layout = (GalMemoryLayout)((BlockDim >> 12) & 1); //?

            bool ZetaEnable = ReadRegisterBool(NvGpuEngine3dReg.ZetaEnable);

            if (VA == 0 || ZetaFormat == 0 || !ZetaEnable)
            {
                Gpu.Renderer.RenderTarget.UnbindZeta();

                return;
            }

            long Key = Vmm.GetPhysicalAddress(VA);

            int Width  = ReadRegister(NvGpuEngine3dReg.ZetaHoriz);
            int Height = ReadRegister(NvGpuEngine3dReg.ZetaVert);

            GalImageFormat Format = ImageUtils.ConvertZeta((GalZetaFormat)ZetaFormat);

            GalImage Image = new GalImage(Width, Height, 1, GobBlockHeight, Layout, Format);

            Gpu.ResourceManager.SendZetaBuffer(Vmm, Key, Image);
        }

        private long[] UploadShaders(NvGpuVmm Vmm)
        {
            long[] Keys = new long[5];

            long BasePosition = MakeInt64From2xInt32(NvGpuEngine3dReg.ShaderAddress);

            int Index = 1;

            int VpAControl = ReadRegister(NvGpuEngine3dReg.ShaderNControl);

            bool VpAEnable = (VpAControl & 1) != 0;

            if (VpAEnable)
            {
                //Note: The maxwell supports 2 vertex programs, usually
                //only VP B is used, but in some cases VP A is also used.
                //In this case, it seems to function as an extra vertex
                //shader stage.
                //The graphics abstraction layer has a special overload for this
                //case, which should merge the two shaders into one vertex shader.
                int VpAOffset = ReadRegister(NvGpuEngine3dReg.ShaderNOffset);
                int VpBOffset = ReadRegister(NvGpuEngine3dReg.ShaderNOffset + 0x10);

                long VpAPos = BasePosition + (uint)VpAOffset;
                long VpBPos = BasePosition + (uint)VpBOffset;

                Keys[(int)GalShaderType.Vertex] = VpBPos;

                Gpu.Renderer.Shader.Create(Vmm, VpAPos, VpBPos, GalShaderType.Vertex);
                Gpu.Renderer.Shader.Bind(VpBPos);

                Index = 2;
            }

            for (; Index < 6; Index++)
            {
                GalShaderType Type = GetTypeFromProgram(Index);

                int Control = ReadRegister(NvGpuEngine3dReg.ShaderNControl + Index * 0x10);
                int Offset  = ReadRegister(NvGpuEngine3dReg.ShaderNOffset  + Index * 0x10);

                //Note: Vertex Program (B) is always enabled.
                bool Enable = (Control & 1) != 0 || Index == 1;

                if (!Enable)
                {
                    Gpu.Renderer.Shader.Unbind(Type);

                    continue;
                }

                long Key = BasePosition + (uint)Offset;

                Keys[(int)Type] = Key;

                Gpu.Renderer.Shader.Create(Vmm, Key, Type);
                Gpu.Renderer.Shader.Bind(Key);
            }

            return Keys;
        }

        private static GalShaderType GetTypeFromProgram(int Program)
        {
            switch (Program)
            {
                case 0:
                case 1: return GalShaderType.Vertex;
                case 2: return GalShaderType.TessControl;
                case 3: return GalShaderType.TessEvaluation;
                case 4: return GalShaderType.Geometry;
                case 5: return GalShaderType.Fragment;
            }

            throw new ArgumentOutOfRangeException(nameof(Program));
        }

        private void SetDepth(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            DirtyState &= ~PipelineState.Depth;
        }

        private void SetStencil(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            DirtyState &= ~PipelineState.Stencil;
        }

        private void SetBlend(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            DirtyState &= ~PipelineState.Blend;
        }

        private void SetColorMask(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            DirtyState &= ~PipelineState.ColorMask;
        }

        private void SetPrimRestart(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            DirtyState &= ~PipelineState.PrimRestart;
        }

        private void SetFramebufferSrgb(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            bool FramebufferSrgb = ReadRegisterBool(NvGpuEngine3dReg.FrameBufferSrgb);

            Gpu.Renderer.Pipeline.SetFramebufferSrgb(FramebufferSrgb);

            Gpu.Renderer.RenderTarget.FramebufferSrgb = FramebufferSrgb;
        }

        private void UpdateDepth()
        {
            bool DepthTestEnabled  = ReadRegisterBool(NvGpuEngine3dReg.DepthTestEnable);
            bool DepthWriteEnabled = ReadRegisterBool(NvGpuEngine3dReg.DepthWriteEnable);

            Gpu.Renderer.Pipeline.SetDepthMask(DepthTestEnabled, DepthWriteEnabled);

            if (DepthTestEnabled)
            {
                GalComparisonOp DepthFunc = (GalComparisonOp)ReadRegister(NvGpuEngine3dReg.DepthTestFunction);

                Gpu.Renderer.Pipeline.SetDepthFunc(DepthFunc);
            }

            if (DepthTestEnabled || DepthWriteEnabled)
            {
                float DepthRangeNear = ReadRegisterFloat(NvGpuEngine3dReg.DepthRangeNNear);
                float DepthRangeFar  = ReadRegisterFloat(NvGpuEngine3dReg.DepthRangeNFar);

                Gpu.Renderer.Pipeline.SetDepthRange(DepthRangeNear, DepthRangeFar);
            }
        }

        private void UpdateStencil()
        {
            bool StencilTestEnabled = ReadRegisterBool(NvGpuEngine3dReg.StencilEnable);

            Gpu.Renderer.Pipeline.SetStencilTestEnabled(StencilTestEnabled);

            if (StencilTestEnabled)
            {
                Gpu.Renderer.Pipeline.SetStencilTest(
                    (GalComparisonOp)ReadRegister(NvGpuEngine3dReg.StencilBackFuncFunc),
                                     ReadRegister(NvGpuEngine3dReg.StencilBackFuncRef),
                               (uint)ReadRegister(NvGpuEngine3dReg.StencilBackFuncMask),
                       (GalStencilOp)ReadRegister(NvGpuEngine3dReg.StencilBackOpFail),
                       (GalStencilOp)ReadRegister(NvGpuEngine3dReg.StencilBackOpZFail),
                       (GalStencilOp)ReadRegister(NvGpuEngine3dReg.StencilBackOpZPass),
                               (uint)ReadRegister(NvGpuEngine3dReg.StencilBackMask),
                    (GalComparisonOp)ReadRegister(NvGpuEngine3dReg.StencilFrontFuncFunc),
                                     ReadRegister(NvGpuEngine3dReg.StencilFrontFuncRef),
                               (uint)ReadRegister(NvGpuEngine3dReg.StencilFrontFuncMask),
                       (GalStencilOp)ReadRegister(NvGpuEngine3dReg.StencilFrontOpFail),
                       (GalStencilOp)ReadRegister(NvGpuEngine3dReg.StencilFrontOpZFail),
                       (GalStencilOp)ReadRegister(NvGpuEngine3dReg.StencilFrontOpZPass),
                               (uint)ReadRegister(NvGpuEngine3dReg.StencilFrontMask));
            }
        }

        private void UpdateBlend()
        {
            bool BlendIndependent = ReadRegisterBool(NvGpuEngine3dReg.BlendIndependent);

            if (BlendIndependent)
            {
                for (int Index = 0; Index < RenderTargetsCount; Index++)
                {
                    bool Enabled = ReadRegisterBool(NvGpuEngine3dReg.IBlendNEnable + Index);

                    Gpu.Renderer.Pipeline.SetBlendEnabled(Index, Enabled);

                    if (Enabled)
                    {
                        bool SeparateAlpha = ReadRegisterBool(NvGpuEngine3dReg.IBlendNSeparateAlpha + Index * 8);

                        GalBlendEquation EquationRgb = ReadBlendEquation(NvGpuEngine3dReg.IBlendNEquationRgb + Index * 8);
                        GalBlendFactor   FuncSrcRgb  = ReadBlendFactor  (NvGpuEngine3dReg.IBlendNFuncSrcRgb  + Index * 8);
                        GalBlendFactor   FuncDstRgb  = ReadBlendFactor  (NvGpuEngine3dReg.IBlendNFuncDstRgb  + Index * 8);

                        if (SeparateAlpha)
                        {
                            GalBlendEquation EquationAlpha = ReadBlendEquation(NvGpuEngine3dReg.IBlendNEquationAlpha + Index * 8);
                            GalBlendFactor   FuncSrcAlpha  = ReadBlendFactor  (NvGpuEngine3dReg.IBlendNFuncSrcAlpha  + Index * 8);
                            GalBlendFactor   FuncDstAlpha  = ReadBlendFactor  (NvGpuEngine3dReg.IBlendNFuncDstAlpha  + Index * 8);

                            Gpu.Renderer.Pipeline.SetBlendSeparate(
                                Index,
                                EquationRgb,
                                FuncSrcRgb,
                                FuncDstRgb,
                                EquationAlpha,
                                FuncSrcAlpha,
                                FuncDstAlpha);
                        }
                        else
                        {
                            Gpu.Renderer.Pipeline.SetBlend(Index, EquationRgb, FuncSrcRgb, FuncDstRgb);
                        }
                    }
                }
            }
            else
            {
                //It seems that even when independent blend is disabled, the first IBlend enable
                //register is still set to indicate whenever blend is enabled or not (?).
                bool Enabled = ReadRegisterBool(NvGpuEngine3dReg.IBlendNEnable);

                Gpu.Renderer.Pipeline.SetBlendEnabled(Enabled);

                if (Enabled)
                {
                    bool SeparateAlpha = ReadRegisterBool(NvGpuEngine3dReg.BlendSeparateAlpha);

                    GalBlendEquation EquationRgb = ReadBlendEquation(NvGpuEngine3dReg.BlendEquationRgb);
                    GalBlendFactor   FuncSrcRgb  = ReadBlendFactor  (NvGpuEngine3dReg.BlendFuncSrcRgb);
                    GalBlendFactor   FuncDstRgb  = ReadBlendFactor  (NvGpuEngine3dReg.BlendFuncDstRgb);

                    if (SeparateAlpha)
                    {
                        GalBlendEquation EquationAlpha = ReadBlendEquation(NvGpuEngine3dReg.BlendEquationAlpha);
                        GalBlendFactor   FuncSrcAlpha  = ReadBlendFactor  (NvGpuEngine3dReg.BlendFuncSrcAlpha);
                        GalBlendFactor   FuncDstAlpha  = ReadBlendFactor  (NvGpuEngine3dReg.BlendFuncDstAlpha);

                        Gpu.Renderer.Pipeline.SetBlendSeparate(
                            EquationRgb,
                            FuncSrcRgb,
                            FuncDstRgb,
                            EquationAlpha,
                            FuncSrcAlpha,
                            FuncDstAlpha);
                    }
                    else
                    {
                        Gpu.Renderer.Pipeline.SetBlend(EquationRgb, FuncSrcRgb, FuncDstRgb);
                    }
                }
            }
        }

        private GalBlendEquation ReadBlendEquation(NvGpuEngine3dReg Register)
        {
            return (GalBlendEquation)ReadRegister(Register);
        }

        private GalBlendFactor ReadBlendFactor(NvGpuEngine3dReg Register)
        {
            return (GalBlendFactor)ReadRegister(Register);
        }

        private void UpdateColorMask()
        {
            bool ColorMaskCommon = ReadRegisterBool(NvGpuEngine3dReg.ColorMaskCommon);

            if (ColorMaskCommon)
            {
                int ColorMask = ReadRegister(NvGpuEngine3dReg.ColorMaskN);

                bool RedMask   = ((ColorMask >> 0)  & 0xf) != 0;
                bool GreenMask = ((ColorMask >> 4)  & 0xf) != 0;
                bool BlueMask  = ((ColorMask >> 8)  & 0xf) != 0;
                bool AlphaMask = ((ColorMask >> 12) & 0xf) != 0;

                Gpu.Renderer.Pipeline.SetColorMask(RedMask, GreenMask, BlueMask, AlphaMask);
            }
            else
            {
                for (int Index = 0; Index < RenderTargetsCount; Index++)
                {
                    int ColorMask = ReadRegister(NvGpuEngine3dReg.ColorMaskN + Index);

                    bool RedMask   = ((ColorMask >> 0)  & 0xf) != 0;
                    bool GreenMask = ((ColorMask >> 4)  & 0xf) != 0;
                    bool BlueMask  = ((ColorMask >> 8)  & 0xf) != 0;
                    bool AlphaMask = ((ColorMask >> 12) & 0xf) != 0;

                    Gpu.Renderer.Pipeline.SetColorMask(Index, RedMask, GreenMask, BlueMask, AlphaMask);
                }
            }
        }

        private void UpdatePrimRestart()
        {
            bool PrimitiveRestartEnabled = ReadRegisterBool(NvGpuEngine3dReg.PrimRestartEnable);

            Gpu.Renderer.Pipeline.SetPrimitiveRestartEnabled(PrimitiveRestartEnabled);

            if (PrimitiveRestartEnabled)
            {
                int Index = ReadRegister(NvGpuEngine3dReg.PrimRestartIndex);

                Gpu.Renderer.Pipeline.SetPrimitiveRestartIndex(Index);
            }
        }

        private void SetRenderTargets()
        {
            //Commercial games do not seem to
            //bool SeparateFragData = ReadRegisterBool(NvGpuEngine3dReg.RTSeparateFragData);

            uint Control = (uint)(ReadRegister(NvGpuEngine3dReg.RTControl));

            uint Count = Control & 0xf;

            if (Count > 0)
            {
                int[] Map = new int[Count];

                for (int Index = 0; Index < Count; Index++)
                {
                    int Shift = 4 + Index * 3;

                    Map[Index] = (int)((Control >> Shift) & 7);
                }

                Gpu.Renderer.RenderTarget.SetMap(Map);
            }
            else
            {
                Gpu.Renderer.RenderTarget.SetMap(null);
            }
        }

        private void UploadTextures(NvGpuVmm Vmm, long[] Keys)
        {
            long BaseShPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.ShaderAddress);

            int TextureCbIndex = ReadRegister(NvGpuEngine3dReg.TextureCbIndex);

            List<(long, GalImage, GalTextureSampler)> UnboundTextures = new List<(long, GalImage, GalTextureSampler)>();

            for (int Index = 0; Index < Keys.Length; Index++)
            {
                foreach (ShaderDeclInfo DeclInfo in Gpu.Renderer.Shader.GetTextureUsage(Keys[Index]))
                {
                    long Position;

                    if (DeclInfo.IsCb)
                    {
                        Position = ConstBuffers[Index][DeclInfo.Cbuf].Position;
                    }
                    else
                    {
                        Position = ConstBuffers[Index][TextureCbIndex].Position;
                    }

                    int TextureHandle = Vmm.ReadInt32(Position + DeclInfo.Index * 4);

                    UnboundTextures.Add(UploadTexture(Vmm, TextureHandle));
                }
            }

            for (int Index = 0; Index < UnboundTextures.Count; Index++)
            {
                (long Key, GalImage Image, GalTextureSampler Sampler) = UnboundTextures[Index];

                if (Key == 0)
                {
                    continue;
                }

                Gpu.Renderer.Texture.Bind(Key, Index, Image);
                Gpu.Renderer.Texture.SetSampler(Sampler);
            }
        }

        private (long, GalImage, GalTextureSampler) UploadTexture(NvGpuVmm Vmm, int TextureHandle)
        {
            if (TextureHandle == 0)
            {
                //FIXME: Some games like puyo puyo will use handles with the value 0.
                //This is a bug, most likely caused by sync issues.
                return (0, default(GalImage), default(GalTextureSampler));
            }

            bool LinkedTsc = ReadRegisterBool(NvGpuEngine3dReg.LinkedTsc);

            int TicIndex = (TextureHandle >>  0) & 0xfffff;

            int TscIndex = LinkedTsc ? TicIndex : (TextureHandle >> 20) & 0xfff;

            long TicPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.TexHeaderPoolOffset);
            long TscPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.TexSamplerPoolOffset);

            TicPosition += TicIndex * 0x20;
            TscPosition += TscIndex * 0x20;

            GalImage Image = TextureFactory.MakeTexture(Vmm, TicPosition);

            GalTextureSampler Sampler = TextureFactory.MakeSampler(Gpu, Vmm, TscPosition);

            long Key = Vmm.ReadInt64(TicPosition + 4) & 0xffffffffffff;

            if (Image.Layout == GalMemoryLayout.BlockLinear)
            {
                Key &= ~0x1ffL;
            }
            else if (Image.Layout == GalMemoryLayout.Pitch)
            {
                Key &= ~0x1fL;
            }

            Key = Vmm.GetPhysicalAddress(Key);

            if (Key == -1)
            {
                //FIXME: Shouldn't ignore invalid addresses.
                return (0, default(GalImage), default(GalTextureSampler));
            }

            Gpu.ResourceManager.SendTexture(Vmm, Key, Image);

            return (Key, Image, Sampler);
        }

        private void UploadConstBuffers(NvGpuVmm Vmm, long[] Keys)
        {
            long[][] ConstBufferKeys = new long[6][];

            for (int Stage = 0; Stage < Keys.Length; Stage++)
            {
                ConstBufferKeys[Stage] = new long[18];

                foreach (ShaderDeclInfo DeclInfo in Gpu.Renderer.Shader.GetConstBufferUsage(Keys[Stage]))
                {
                    ConstBuffer Cb = ConstBuffers[Stage][DeclInfo.Cbuf];

                    if (!Cb.Enabled)
                    {
                        continue;
                    }

                    long Key = Vmm.GetPhysicalAddress(Cb.Position);

                    bool CbCached = Gpu.Renderer.Buffer.IsCached(Key, Cb.Size);

                    if (Gpu.ResourceManager.MemoryRegionModified(Vmm, Key, Cb.Size, NvGpuBufferType.ConstBuffer) || !CbCached)
                    {
                        if (Vmm.TryGetHostAddress(Cb.Position, Cb.Size, out IntPtr CbPtr))
                        {
                            Gpu.Renderer.Buffer.Create(Key, CbPtr, Cb.Size);
                        }
                        else
                        {
                            Gpu.Renderer.Buffer.Create(Key, Vmm.ReadBytes(Cb.Position, Cb.Size));
                        }
                    }

                    ConstBufferKeys[Stage][DeclInfo.Cbuf] = Key;
                }
            }

            Gpu.Renderer.Pipeline.BindConstBuffers(ConstBufferKeys);
        }

        private void UploadVertexArrays(NvGpuVmm Vmm)
        {
            //Upload index buffers.
            long IbPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.IndexArrayAddress);

            long IboKey = Vmm.GetPhysicalAddress(IbPosition);

            int IndexEntryFmt = ReadRegister(NvGpuEngine3dReg.IndexArrayFormat);
            int IndexFirst    = ReadRegister(NvGpuEngine3dReg.IndexBatchFirst);
            int IndexCount    = ReadRegister(NvGpuEngine3dReg.IndexBatchCount);
            int VertexBase    = ReadRegister(NvGpuEngine3dReg.VertexArrayElemBase);
            int PrimCtrl      = ReadRegister(NvGpuEngine3dReg.VertexBeginGl);

            GalPrimitiveType PrimType = (GalPrimitiveType)(PrimCtrl & 0xffff);

            GalIndexFormat IndexFormat = (GalIndexFormat)IndexEntryFmt;

            int IndexEntrySize = 1 << IndexEntryFmt;

            if (IndexEntrySize > 4)
            {
                throw new InvalidOperationException($"Invalid index entry size \"{IndexEntrySize}\".");
            }

            long IbVtxCount = 0;

            if (IndexCount != 0)
            {
                int IbSize = (IndexFirst + IndexCount) * IndexEntrySize;

                int HostIbSize = IbSize;

                bool UsesLegacyQuads =
                    PrimType == GalPrimitiveType.Quads ||
                    PrimType == GalPrimitiveType.QuadStrip;

                if (UsesLegacyQuads)
                {
                    if (PrimType == GalPrimitiveType.Quads)
                    {
                        HostIbSize = IbHelper.ConvertIbSizeQuadsToTris(HostIbSize);
                    }
                    else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                    {
                        HostIbSize = IbHelper.ConvertIbSizeQuadStripToTris(HostIbSize);
                    }
                }

                bool IboCached = Gpu.Renderer.Rasterizer.IsIboCached(IboKey, HostIbSize, out IbVtxCount);

                if (Gpu.ResourceManager.MemoryRegionModified(Vmm, IboKey, (uint)IbSize, NvGpuBufferType.Index) || !IboCached)
                {
                    IbVtxCount = IbHelper.GetIbMaxVertexCount(IndexFormat);

                    if (!UsesLegacyQuads)
                    {
                        bool ShallGetVertexCount = IndexFormat != GalIndexFormat.Byte;

                        if (!ShallGetVertexCount && Vmm.TryGetHostAddress(IbPosition, IbSize, out IntPtr IbPtr))
                        {
                            Gpu.Renderer.Rasterizer.CreateIbo(IboKey, IbPtr, IbSize, IbVtxCount);
                        }
                        else
                        {
                            byte[] Data = Vmm.ReadBytes(IbPosition, IbSize);

                            if (IndexFormat == GalIndexFormat.Int16)
                            {
                                IbVtxCount = IbHelper.GetVertexCountFromIb16(Data);
                            }
                            else if (IndexFormat == GalIndexFormat.Int32)
                            {
                                IbVtxCount = IbHelper.GetVertexCountFromIb32(Data);
                            }

                            Gpu.Renderer.Rasterizer.CreateIbo(IboKey, Data, IbVtxCount);
                        }
                    }
                    else
                    {
                        byte[] Buffer = Vmm.ReadBytes(IbPosition, IbSize);

                        if (PrimType == GalPrimitiveType.Quads)
                        {
                            Buffer = IbHelper.ConvertIbQuadsToTris(Buffer, IndexEntrySize, IndexCount);
                        }
                        else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                        {
                            Buffer = IbHelper.ConvertIbQuadStripToTris(Buffer, IndexEntrySize, IndexCount);
                        }

                        Gpu.Renderer.Rasterizer.CreateIbo(IboKey, Buffer, IbVtxCount);
                    }
                }

                if (!UsesLegacyQuads)
                {
                    Gpu.Renderer.Rasterizer.SetIndexArray(IbSize, IndexFormat);
                }
                else
                {
                    if (PrimType == GalPrimitiveType.Quads)
                    {
                        Gpu.Renderer.Rasterizer.SetIndexArray(IbHelper.ConvertIbSizeQuadsToTris(IbSize), IndexFormat);
                    }
                    else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                    {
                        Gpu.Renderer.Rasterizer.SetIndexArray(IbHelper.ConvertIbSizeQuadStripToTris(IbSize), IndexFormat);
                    }
                }
            }

            //Get vertex arrays and attributes count from attribute registers.
            int AttribsCount = 0;
            int ArraysCount  = 0;

            for (int Index = 0; Index < 32; Index++)
            {
                int Packed = Registers[(int)NvGpuEngine3dReg.VertexAttribNFormat + Index];

                //The size is a 3 bits field, starting at bit 27.
                //If size is 0, then the attribute is unused, skip it.
                if ((Packed & (7 << 27)) == 0)
                {
                    continue;
                }

                if (AttribsCount < Index)
                {
                    AttribsCount = Index;
                }

                int ArrayIndex = Packed & 0x1f;

                if (ArraysCount < ArrayIndex)
                {
                    ArraysCount = ArrayIndex;
                }
            }

            //Those are actually the last valid indices, so we need
            //to add 1 to get the count.
            AttribsCount++;
            ArraysCount++;

            //Upload vertex buffers, build vertex array info table.
            GalVertexAttribArray[] Arrays = new GalVertexAttribArray[ArraysCount];

            int VertexFirst = ReadRegister(NvGpuEngine3dReg.VertexArrayFirst);
            int VertexCount = ReadRegister(NvGpuEngine3dReg.VertexArrayCount);

            for (int Index = 0; Index < ArraysCount; Index++)
            {
                int Control = ReadRegister(NvGpuEngine3dReg.VertexArrayNControl + Index * 4);

                bool Enable = (Control & 0x1000) != 0;

                if (!Enable)
                {
                    continue;
                }

                long VbPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.VertexArrayNAddress + Index * 4);
                long VbEndPos   = MakeInt64From2xInt32(NvGpuEngine3dReg.VertexArrayNEndAddr + Index * 2);

                int VertexDivisor = ReadRegister(NvGpuEngine3dReg.VertexArrayNDivisor + Index * 4);

                bool Instanced = ReadRegisterBool(NvGpuEngine3dReg.VertexArrayNInstance + Index);

                int Stride = Control & 0xfff;

                if (Instanced && VertexDivisor != 0)
                {
                    VbPosition += Stride * (CurrentInstance / VertexDivisor);
                }

                if (VbPosition > VbEndPos)
                {
                    //Instance is invalid, ignore the draw call
                    continue;
                }

                long VboKey = Vmm.GetPhysicalAddress(VbPosition);

                long VbSize = (VbEndPos - VbPosition) + 1;

                long MaxVbSize = (IndexCount != 0
                    ? VertexBase  + IbVtxCount
                    : VertexFirst + VertexCount) * Stride;

                if (MaxVbSize < VbSize)
                {
                    VbSize = MaxVbSize;
                }

                bool VboCached = Gpu.Renderer.Rasterizer.IsVboCached(VboKey, (int)VbSize);

                if (Gpu.ResourceManager.MemoryRegionModified(Vmm, VboKey, VbSize, NvGpuBufferType.Vertex) || !VboCached)
                {
                    if (Vmm.TryGetHostAddress(VbPosition, VbSize, out IntPtr VbPtr))
                    {
                        Gpu.Renderer.Rasterizer.CreateVbo(VboKey, VbPtr, (int)VbSize);
                    }
                    else
                    {
                        Gpu.Renderer.Rasterizer.CreateVbo(VboKey, Vmm.ReadBytes(VbPosition, VbSize));
                    }
                }

                Arrays[Index] = new GalVertexAttribArray(VboKey, Stride, Instanced ? VertexDivisor : 0);
            }

            //Set vertex attributes.
            ReadOnlySpan<int> RawAttribs = new ReadOnlySpan<int>(Registers, (int)NvGpuEngine3dReg.VertexAttribNFormat, AttribsCount);

            if (!Gpu.Renderer.Rasterizer.TryBindVao(RawAttribs, Arrays))
            {
                GalVertexAttrib[] Attributes = new GalVertexAttrib[AttribsCount];

                for (int Index = 0; Index < AttribsCount; Index++)
                {
                    int Packed = ReadRegister(NvGpuEngine3dReg.VertexAttribNFormat + Index);

                    int ArrayIndex = Packed & 0x1f;

                    long VbPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.VertexArrayNAddress + ArrayIndex * 4);

                    bool IsConst = ((Packed >> 6) & 1) != 0;

                    int Offset = (Packed >> 7) & 0x3fff;

                    //Note: 16 is the maximum size of an attribute,
                    //having a component size of 32-bits with 4 elements (a vec4).
                    byte[] Data = Vmm.ReadBytes(VbPosition + Offset, 16);

                    GalVertexAttribSize Size = (GalVertexAttribSize)((Packed >> 21) & 0x3f);
                    GalVertexAttribType Type = (GalVertexAttribType)((Packed >> 27) & 0x7);

                    bool IsRgba = ((Packed >> 31) & 1) != 0;

                    Attributes[Index] = new GalVertexAttrib(
                        IsConst,
                        ArrayIndex,
                        Offset,
                        Data,
                        Size,
                        Type,
                        IsRgba);
                }

                Gpu.Renderer.Rasterizer.CreateVao(RawAttribs, Attributes, Arrays);
            }
        }

        private void DispatchRender(NvGpuVmm Vmm)
        {
            int IndexCount = ReadRegister(NvGpuEngine3dReg.IndexBatchCount);
            int PrimCtrl   = ReadRegister(NvGpuEngine3dReg.VertexBeginGl);

            GalPrimitiveType PrimType = (GalPrimitiveType)(PrimCtrl & 0xffff);

            bool InstanceNext = ((PrimCtrl >> 26) & 1) != 0;
            bool InstanceCont = ((PrimCtrl >> 27) & 1) != 0;

            if (InstanceNext && InstanceCont)
            {
                throw new InvalidOperationException("GPU tried to increase and reset instance count at the same time");
            }

            if (InstanceNext)
            {
                CurrentInstance++;
            }
            else if (!InstanceCont)
            {
                CurrentInstance = 0;
            }

            float FlipX = GetFlipSign(NvGpuEngine3dReg.ViewportNScaleX);
            float FlipY = GetFlipSign(NvGpuEngine3dReg.ViewportNScaleY);

            int ScreenYControl = ReadRegister(NvGpuEngine3dReg.ScreenYControl);

            bool NegateY = (ScreenYControl & 1) != 0;

            if (NegateY)
            {
                FlipY = -FlipY;
            }

            Gpu.Renderer.Pipeline.SetFlip(FlipX, FlipY, CurrentInstance);

            Gpu.Renderer.RenderTarget.Bind();

            if (IndexCount != 0)
            {
                int IndexEntryFmt = ReadRegister(NvGpuEngine3dReg.IndexArrayFormat);
                int IndexFirst    = ReadRegister(NvGpuEngine3dReg.IndexBatchFirst);
                int VertexBase    = ReadRegister(NvGpuEngine3dReg.VertexArrayElemBase);

                long IndexPosition = MakeInt64From2xInt32(NvGpuEngine3dReg.IndexArrayAddress);

                long IboKey = Vmm.GetPhysicalAddress(IndexPosition);

                //Quad primitive types were deprecated on OpenGL 3.x,
                //they are converted to a triangles index buffer on IB creation,
                //so we should use the triangles type here too.
                if (PrimType == GalPrimitiveType.Quads ||
                    PrimType == GalPrimitiveType.QuadStrip)
                {
                    PrimType = GalPrimitiveType.Triangles;

                    //Note: We assume that index first points to the first
                    //vertex of a quad, if it points to the middle of a
                    //quad (First % 4 != 0 for Quads) then it will not work properly.
                    if (PrimType == GalPrimitiveType.Quads)
                    {
                        IndexFirst = IbHelper.ConvertIbSizeQuadsToTris(IndexFirst);
                    }
                    else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                    {
                        IndexFirst = IbHelper.ConvertIbSizeQuadStripToTris(IndexFirst);
                    }
                }

                Gpu.Renderer.Rasterizer.DrawElements(IboKey, IndexFirst, VertexBase, PrimType);
            }
            else
            {
                int VertexFirst = ReadRegister(NvGpuEngine3dReg.VertexArrayFirst);
                int VertexCount = ReadRegister(NvGpuEngine3dReg.VertexArrayCount);

                Gpu.Renderer.Rasterizer.DrawArrays(VertexFirst, VertexCount, PrimType);
            }

            //Is the GPU really clearing those registers after draw?
            WriteRegister(NvGpuEngine3dReg.IndexBatchFirst, 0);
            WriteRegister(NvGpuEngine3dReg.IndexBatchCount, 0);
        }

        private enum QueryMode
        {
            WriteSeq,
            Sync,
            WriteCounterAndTimestamp
        }

        private void QueryControl(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            long Position = MakeInt64From2xInt32(NvGpuEngine3dReg.QueryAddress);

            int Seq  = Registers[(int)NvGpuEngine3dReg.QuerySequence];
            int Ctrl = Registers[(int)NvGpuEngine3dReg.QueryControl];

            QueryMode Mode = (QueryMode)(Ctrl & 3);

            switch (Mode)
            {
                case QueryMode.WriteSeq: Vmm.WriteInt32(Position, Seq); break;

                case QueryMode.WriteCounterAndTimestamp:
                {
                    //TODO: Implement counters.
                    long Counter = 1;

                    long Timestamp = PerformanceCounter.ElapsedMilliseconds;

                    Timestamp = (long)(Timestamp * 615384.615385);

                    Vmm.WriteInt64(Position + 0, Counter);
                    Vmm.WriteInt64(Position + 8, Timestamp);

                    break;
                }
            }
        }

        private void CbData(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            long Position = MakeInt64From2xInt32(NvGpuEngine3dReg.ConstBufferAddress);

            long Key = Vmm.GetPhysicalAddress(Position);

            int Offset = ReadRegister(NvGpuEngine3dReg.ConstBufferOffset);

            Vmm.WriteInt32(Position + Offset, MethCall.Argument);

            WriteRegister(NvGpuEngine3dReg.ConstBufferOffset, Offset + 4);

            Gpu.ResourceManager.RemoveFromPbCache(NvGpuBufferType.ConstBuffer, Key);
        }

        private void CbBind(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            int Stage = (MethCall.Method - 0x904) >> 3;

            int Index = MethCall.Argument;

            bool Enabled = (Index & 1) != 0;

            Index = (Index >> 4) & 0x1f;

            long Position = MakeInt64From2xInt32(NvGpuEngine3dReg.ConstBufferAddress);

            int Size = ReadRegister(NvGpuEngine3dReg.ConstBufferSize);

            ConstBuffers[Stage][Index].Enabled  = Enabled;
            ConstBuffers[Stage][Index].Position = Position;
            ConstBuffers[Stage][Index].Size     = Size;
        }

        private float GetFlipSign(NvGpuEngine3dReg Reg)
        {
            return MathF.Sign(ReadRegisterFloat(Reg));
        }

        private long MakeInt64From2xInt32(NvGpuEngine3dReg Reg)
        {
            return
                (long)Registers[(int)Reg + 0] << 32 |
                (uint)Registers[(int)Reg + 1];
        }

        private void WriteRegister(GpuMethodCall MethCall)
        {
            Registers[MethCall.Method] = MethCall.Argument;
        }

        private int ReadRegister(NvGpuEngine3dReg Reg)
        {
            return Registers[(int)Reg];
        }

        private float ReadRegisterFloat(NvGpuEngine3dReg Reg)
        {
            return BitConverter.Int32BitsToSingle(ReadRegister(Reg));
        }

        private bool ReadRegisterBool(NvGpuEngine3dReg Reg)
        {
            return (ReadRegister(Reg) & 1) != 0;
        }

        private void WriteRegister(NvGpuEngine3dReg Reg, int Value)
        {
            Registers[(int)Reg] = Value;
        }
    }
}
