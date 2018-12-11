using OpenTK.Graphics.OpenGL;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLPipeline : IGalPipeline
    {
        private OGLConstBuffer Buffer;
        private OGLShader      Shader;

        private float FlipX;
        private float FlipY;
        private int   Instance;

        public OGLPipeline(OGLConstBuffer Buffer, OGLShader Shader)
        {
            this.Buffer = Buffer;
            this.Shader = Shader;
        }

        public void SetFlip(float FlipX, float FlipY, int Instance)
        {
            if (FlipX    != this.FlipX ||
                FlipY    != this.FlipY ||
                Instance != this.Instance)
            {
                this.FlipX    = FlipX;
                this.FlipY    = FlipY;
                this.Instance = Instance;

                Shader.SetExtraData(FlipX, FlipY, Instance);
            }
        }

        public void SetDepthMask(bool DepthTestEnabled, bool DepthWriteEnabled)
        {
            Enable(EnableCap.DepthTest, DepthTestEnabled);

            GL.DepthMask(DepthWriteEnabled);
        }

        public void SetDepthFunc(GalComparisonOp DepthFunc)
        {
            GL.DepthFunc(OGLEnumConverter.GetDepthFunc(DepthFunc));
        }

        public void SetDepthRange(float DepthRangeNear, float DepthRangeFar)
        {
            GL.DepthRange(DepthRangeNear, DepthRangeFar);
        }

        public void SetStencilTestEnabled(bool Enabled)
        {
            Enable(EnableCap.StencilTest, Enabled);
        }

        public void SetStencilTest(
            GalComparisonOp StencilBackFuncFunc,
            int             StencilBackFuncRef,
            uint            StencilBackFuncMask,
            GalStencilOp    StencilBackOpFail,
            GalStencilOp    StencilBackOpZFail,
            GalStencilOp    StencilBackOpZPass,
            uint            StencilBackMask,
            GalComparisonOp StencilFrontFuncFunc,
            int             StencilFrontFuncRef,
            uint            StencilFrontFuncMask,
            GalStencilOp    StencilFrontOpFail,
            GalStencilOp    StencilFrontOpZFail,
            GalStencilOp    StencilFrontOpZPass,
            uint            StencilFrontMask)
        {
            GL.StencilFuncSeparate(
                StencilFace.Back,
                OGLEnumConverter.GetStencilFunc(StencilBackFuncFunc),
                StencilBackFuncRef,
                StencilBackFuncMask);

            GL.StencilOpSeparate(
                StencilFace.Back,
                OGLEnumConverter.GetStencilOp(StencilBackOpFail),
                OGLEnumConverter.GetStencilOp(StencilBackOpZFail),
                OGLEnumConverter.GetStencilOp(StencilBackOpZPass));

            GL.StencilMaskSeparate(StencilFace.Back, StencilBackMask);

            GL.StencilFuncSeparate(
                StencilFace.Front,
                OGLEnumConverter.GetStencilFunc(StencilFrontFuncFunc),
                StencilFrontFuncRef,
                StencilFrontFuncMask);

            GL.StencilOpSeparate(
                StencilFace.Front,
                OGLEnumConverter.GetStencilOp(StencilFrontOpFail),
                OGLEnumConverter.GetStencilOp(StencilFrontOpZFail),
                OGLEnumConverter.GetStencilOp(StencilFrontOpZPass));

            GL.StencilMaskSeparate(StencilFace.Front, StencilFrontMask);
        }

        public void SetBlendEnabled(bool Enabled)
        {
            Enable(EnableCap.Blend, Enabled);
        }

        public void SetBlendEnabled(int Index, bool Enabled)
        {
            Enable(IndexedEnableCap.Blend, Index, Enabled);
        }

        public void SetBlend(
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb)
        {
            GL.BlendEquation(OGLEnumConverter.GetBlendEquation(EquationRgb));

            GL.BlendFunc(
                OGLEnumConverter.GetBlendFactor(FuncSrcRgb),
                OGLEnumConverter.GetBlendFactor(FuncDstRgb));
        }

        public void SetBlend(
            int              Index,
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb)
        {
            GL.BlendEquation(Index, OGLEnumConverter.GetBlendEquation(EquationRgb));

            GL.BlendFunc(
                Index,
                (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(FuncSrcRgb),
                (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(FuncDstRgb));
        }

        public void SetBlendSeparate(
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb,
            GalBlendEquation EquationAlpha,
            GalBlendFactor   FuncSrcAlpha,
            GalBlendFactor   FuncDstAlpha)
        {
            GL.BlendEquationSeparate(
                OGLEnumConverter.GetBlendEquation(EquationRgb),
                OGLEnumConverter.GetBlendEquation(EquationAlpha));

            GL.BlendFuncSeparate(
                (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(FuncSrcRgb),
                (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(FuncDstRgb),
                (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(FuncSrcAlpha),
                (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(FuncDstAlpha));
        }

        public void SetBlendSeparate(
            int              Index,
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb,
            GalBlendEquation EquationAlpha,
            GalBlendFactor   FuncSrcAlpha,
            GalBlendFactor   FuncDstAlpha)
        {
            GL.BlendEquationSeparate(
                Index,
                OGLEnumConverter.GetBlendEquation(EquationRgb),
                OGLEnumConverter.GetBlendEquation(EquationAlpha));

            GL.BlendFuncSeparate(
                Index,
                (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(FuncSrcRgb),
                (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(FuncDstRgb),
                (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(FuncSrcAlpha),
                (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(FuncDstAlpha));
        }

        public void SetColorMask(bool RedMask, bool GreenMask, bool BlueMask, bool AlphaMask)
        {
            GL.ColorMask(RedMask, GreenMask, BlueMask, AlphaMask);
        }

        public void SetColorMask(
            int  Index,
            bool RedMask,
            bool GreenMask,
            bool BlueMask,
            bool AlphaMask)
        {
            GL.ColorMask(Index, RedMask, GreenMask, BlueMask, AlphaMask);
        }

        public void SetPrimitiveRestartEnabled(bool Enabled)
        {
            Enable(EnableCap.PrimitiveRestart, Enabled);
        }

        public void SetPrimitiveRestartIndex(int Index)
        {
            GL.PrimitiveRestartIndex(Index);
        }

        public void SetFramebufferSrgb(bool Enabled)
        {
            Enable(EnableCap.FramebufferSrgb, Enabled);
        }

        public void BindConstBuffers(long[][] ConstBufferKeys)
        {
            int FreeBinding = OGLShader.ReservedCbufCount;

            void BindIfNotNull(OGLShaderStage Stage)
            {
                if (Stage != null)
                {
                    foreach (ShaderDeclInfo DeclInfo in Stage.ConstBufferUsage)
                    {
                        long Key = ConstBufferKeys[(int)Stage.Type][DeclInfo.Cbuf];

                        if (Key != 0 && Buffer.TryGetUbo(Key, out int UboHandle))
                        {
                            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, FreeBinding, UboHandle);
                        }

                        FreeBinding++;
                    }
                }
            }

            BindIfNotNull(Shader.Current.Vertex);
            BindIfNotNull(Shader.Current.TessControl);
            BindIfNotNull(Shader.Current.TessEvaluation);
            BindIfNotNull(Shader.Current.Geometry);
            BindIfNotNull(Shader.Current.Fragment);
        }

        private void Enable(EnableCap Cap, bool Enabled)
        {
            if (Enabled)
            {
                GL.Enable(Cap);
            }
            else
            {
                GL.Disable(Cap);
            }
        }

        private void Enable(IndexedEnableCap Cap, int Index, bool Enabled)
        {
            if (Enabled)
            {
                GL.Enable(Cap, Index);
            }
            else
            {
                GL.Disable(Cap, Index);
            }
        }
    }
}