using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public class OGLPipeline : IGalPipeline
    {
        private GalPipelineState O;

        public OGLPipeline()
        {
            //The following values match OpenGL's defaults
            O = new GalPipelineState();

            O.FrontFace = GalFrontFace.CCW;

            O.CullFaceEnabled = false;
            O.CullFace = GalCullFace.Back;

            O.DepthTestEnabled = false;
            O.DepthClear = 1f;
            O.DepthFunc = GalComparisonOp.Less;

            O.StencilTestEnabled = false;
            O.StencilClear = 0;

            O.StencilBackFuncFunc = GalComparisonOp.Always;
            O.StencilBackFuncRef = 0;
            O.StencilBackFuncMask = UInt32.MaxValue;
            O.StencilBackOpFail = GalStencilOp.Keep;
            O.StencilBackOpZFail = GalStencilOp.Keep;
            O.StencilBackOpZPass = GalStencilOp.Keep;
            O.StencilBackMask = UInt32.MaxValue;

            O.StencilFrontFuncFunc = GalComparisonOp.Always;
            O.StencilFrontFuncRef = 0;
            O.StencilFrontFuncMask = UInt32.MaxValue;
            O.StencilFrontOpFail = GalStencilOp.Keep;
            O.StencilFrontOpZFail = GalStencilOp.Keep;
            O.StencilFrontOpZPass = GalStencilOp.Keep;
            O.StencilFrontMask = UInt32.MaxValue;

            O.BlendEnabled = false;
            O.BlendSeparateAlpha = false;

            O.BlendEquationRgb = 0;
            O.BlendFuncSrcRgb = GalBlendFactor.One;
            O.BlendFuncDstRgb = GalBlendFactor.Zero;
            O.BlendEquationAlpha = 0;
            O.BlendFuncSrcAlpha = GalBlendFactor.One;
            O.BlendFuncDstAlpha = GalBlendFactor.Zero;

            O.PrimitiveRestartEnabled = false;
            O.PrimitiveRestartIndex = 0;
        }

        public void Bind(ref GalPipelineState S)
        {
            // O stands for Other, S from State (current state)

            //Note: Uncomment SetFrontFace and SetCullFace when flipping issues are solved

            //if (S.FrontFace != O.FrontFace)
            //{
            //    GL.FrontFace(OGLEnumConverter.GetFrontFace(S.FrontFace));
            //}

            //if (S.CullFaceEnabled != O.CullFaceEnabled)
            //{
            //    Enable(EnableCap.CullFace, S.CullFaceEnabled);
            //}

            //if (S.CullFaceEnabled)
            //{
            //    if (S.CullFace != O.CullFace)
            //    {
            //        GL.CullFace(OGLEnumConverter.GetCullFace(S.CullFace));
            //    }
            //}

            if (S.DepthTestEnabled != O.DepthTestEnabled)
            {
                Enable(EnableCap.DepthTest, S.DepthTestEnabled);
            }

            if (S.DepthClear != O.DepthClear)
            {
                GL.ClearDepth(S.DepthClear);
            }

            if (S.DepthTestEnabled)
            {
                if (S.DepthFunc != O.DepthFunc)
                {
                    GL.DepthFunc(OGLEnumConverter.GetDepthFunc(S.DepthFunc));
                }
            }

            if (S.StencilTestEnabled != O.StencilTestEnabled)
            {
                Enable(EnableCap.StencilTest, S.StencilTestEnabled);
            }

            if (S.StencilClear != O.StencilClear)
            {
                GL.ClearStencil(S.StencilClear);
            }

            if (S.StencilTestEnabled)
            {
                if (S.StencilBackFuncFunc != O.StencilBackFuncFunc ||
                    S.StencilBackFuncRef  != O.StencilBackFuncRef  ||
                    S.StencilBackFuncMask != O.StencilBackFuncMask)
                {
                    GL.StencilFuncSeparate(
                        StencilFace.Back,
                        OGLEnumConverter.GetStencilFunc(S.StencilBackFuncFunc),
                        S.StencilBackFuncRef,
                        S.StencilBackFuncMask);
                }

                if (S.StencilBackOpFail  != O.StencilBackOpFail  ||
                    S.StencilBackOpZFail != O.StencilBackOpZFail ||
                    S.StencilBackOpZPass != O.StencilBackOpZPass)
                {
                    GL.StencilOpSeparate(
                        StencilFace.Back,
                        OGLEnumConverter.GetStencilOp(S.StencilBackOpFail),
                        OGLEnumConverter.GetStencilOp(S.StencilBackOpZFail),
                        OGLEnumConverter.GetStencilOp(S.StencilBackOpZPass));
                }

                if (S.StencilBackMask != O.StencilBackMask)
                {
                    GL.StencilMaskSeparate(StencilFace.Back, S.StencilBackMask);
                }

                if (S.StencilFrontFuncFunc != O.StencilFrontFuncFunc ||
                    S.StencilFrontFuncRef  != O.StencilFrontFuncRef  ||
                    S.StencilFrontFuncMask != O.StencilFrontFuncMask)
                {
                    GL.StencilFuncSeparate(
                        StencilFace.Front,
                        OGLEnumConverter.GetStencilFunc(S.StencilFrontFuncFunc),
                        S.StencilFrontFuncRef,
                        S.StencilFrontFuncMask);
                }

                if (S.StencilFrontOpFail  != O.StencilFrontOpFail  ||
                    S.StencilFrontOpZFail != O.StencilFrontOpZFail ||
                    S.StencilFrontOpZPass != O.StencilFrontOpZPass)
                {
                    GL.StencilOpSeparate(
                        StencilFace.Front,
                        OGLEnumConverter.GetStencilOp(S.StencilFrontOpFail),
                        OGLEnumConverter.GetStencilOp(S.StencilFrontOpZFail),
                        OGLEnumConverter.GetStencilOp(S.StencilFrontOpZPass));
                }

                if (S.StencilFrontMask != O.StencilFrontMask)
                {
                    GL.StencilMaskSeparate(StencilFace.Front, S.StencilFrontMask);
                }
            }

            if (S.BlendEnabled != O.BlendEnabled)
            {
                Enable(EnableCap.Blend, S.BlendEnabled);
            }

            if (S.BlendEnabled)
            {
                if (S.BlendSeparateAlpha)
                {
                    if (S.BlendEquationRgb   != O.BlendEquationRgb ||
                        S.BlendEquationAlpha != O.BlendEquationAlpha)
                    {
                        GL.BlendEquationSeparate(
                            OGLEnumConverter.GetBlendEquation(S.BlendEquationRgb),
                            OGLEnumConverter.GetBlendEquation(S.BlendEquationAlpha));
                    }

                    if (S.BlendFuncSrcRgb   != O.BlendFuncSrcRgb   ||
                        S.BlendFuncDstRgb   != O.BlendFuncDstRgb   ||
                        S.BlendFuncSrcAlpha != O.BlendFuncSrcAlpha ||
                        S.BlendFuncDstAlpha != O.BlendFuncDstAlpha)
                    {
                        GL.BlendFuncSeparate(
                            (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(S.BlendFuncSrcRgb),
                            (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(S.BlendFuncDstRgb),
                            (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(S.BlendFuncSrcAlpha),
                            (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(S.BlendFuncDstAlpha));
                    }
                }
                else
                {
                    if (S.BlendEquationRgb != O.BlendEquationRgb)
                    {
                        GL.BlendEquation(OGLEnumConverter.GetBlendEquation(S.BlendEquationRgb));
                    }

                    if (S.BlendFuncSrcRgb != O.BlendFuncSrcRgb ||
                        S.BlendFuncDstRgb != O.BlendFuncDstRgb)
                    {
                        GL.BlendFunc(
                            OGLEnumConverter.GetBlendFactor(S.BlendFuncSrcRgb),
                            OGLEnumConverter.GetBlendFactor(S.BlendFuncDstRgb));
                    }
                }
            }

            if (S.PrimitiveRestartEnabled != O.PrimitiveRestartEnabled)
            {
                Enable(EnableCap.PrimitiveRestart, S.PrimitiveRestartEnabled);
            }

            if (S.PrimitiveRestartEnabled)
            {
                if (S.PrimitiveRestartIndex != O.PrimitiveRestartIndex)
                {
                    GL.PrimitiveRestartIndex(S.PrimitiveRestartIndex);
                }
            }

            O = S;
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
    }
}