namespace Ryujinx.Graphics.Gal
{
    public interface IGalPipeline
    {
        void SetFlip(float FlipX, float FlipY, int Instance);

        //Depth.
        void SetDepthMask(bool DepthTestEnabled, bool DepthWriteEnabled);

        void SetDepthFunc(GalComparisonOp DepthFunc);

        void SetDepthRange(float DepthRangeNear, float DepthRangeFar);

        //Stencil.
        void SetStencilTestEnabled(bool Enabled);

        void SetStencilTest(
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
            uint            StencilFrontMask);

        //Blend.
        void SetBlendEnabled(bool Enabled);

        void SetBlendEnabled(int Index, bool Enabled);

        void SetBlend(
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb);

        void SetBlend(
            int              Index,
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb);

        void SetBlendSeparate(
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb,
            GalBlendEquation EquationAlpha,
            GalBlendFactor   FuncSrcAlpha,
            GalBlendFactor   FuncDstAlpha);

        void SetBlendSeparate(
            int              Index,
            GalBlendEquation EquationRgb,
            GalBlendFactor   FuncSrcRgb,
            GalBlendFactor   FuncDstRgb,
            GalBlendEquation EquationAlpha,
            GalBlendFactor   FuncSrcAlpha,
            GalBlendFactor   FuncDstAlpha);

        //Color mask.
        void SetColorMask(bool RedMask, bool GreenMask, bool BlueMask, bool AlphaMask);

        void SetColorMask(
            int  Index,
            bool RedMask,
            bool GreenMask,
            bool BlueMask,
            bool AlphaMask);

        //Primitive restart.
        void SetPrimitiveRestartEnabled(bool Enabled);
        void SetPrimitiveRestartIndex(int Index);

        void SetFramebufferSrgb(bool Enabled);

        void BindConstBuffers(long[][] ConstBufferKeys);
    }
}