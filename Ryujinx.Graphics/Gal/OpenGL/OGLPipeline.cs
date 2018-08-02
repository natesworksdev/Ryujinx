using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLPipeline : IGalPipeline
    {
        private static Dictionary<GalVertexAttribSize, int> AttribElements =
                   new Dictionary<GalVertexAttribSize, int>()
        {
            { GalVertexAttribSize._32_32_32_32, 4 },
            { GalVertexAttribSize._32_32_32,    3 },
            { GalVertexAttribSize._16_16_16_16, 4 },
            { GalVertexAttribSize._32_32,       2 },
            { GalVertexAttribSize._16_16_16,    3 },
            { GalVertexAttribSize._8_8_8_8,     4 },
            { GalVertexAttribSize._16_16,       2 },
            { GalVertexAttribSize._32,          1 },
            { GalVertexAttribSize._8_8_8,       3 },
            { GalVertexAttribSize._8_8,         2 },
            { GalVertexAttribSize._16,          1 },
            { GalVertexAttribSize._8,           1 },
            { GalVertexAttribSize._10_10_10_2,  4 },
            { GalVertexAttribSize._11_11_10,    3 }
        };

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> AttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.Int   },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.Int   },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.Short },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.Int   },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.Short },
            { GalVertexAttribSize._8_8_8_8,     VertexAttribPointerType.Byte  },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.Short },
            { GalVertexAttribSize._32,          VertexAttribPointerType.Int   },
            { GalVertexAttribSize._8_8_8,       VertexAttribPointerType.Byte  },
            { GalVertexAttribSize._8_8,         VertexAttribPointerType.Byte  },
            { GalVertexAttribSize._16,          VertexAttribPointerType.Short },
            { GalVertexAttribSize._8,           VertexAttribPointerType.Byte  },
            { GalVertexAttribSize._10_10_10_2,  VertexAttribPointerType.Int   }, //?
            { GalVertexAttribSize._11_11_10,    VertexAttribPointerType.Int   }  //?
        };

        private GalPipelineState O;

        private OGLConstBuffer Buffer;
        private OGLRasterizer Rasterizer;
        private OGLShader Shader;

        private int VaoHandle;

        public OGLPipeline(OGLConstBuffer Buffer, OGLRasterizer Rasterizer, OGLShader Shader)
        {
            this.Buffer     = Buffer;
            this.Rasterizer = Rasterizer;
            this.Shader     = Shader;

            //These values match OpenGL's defaults
            O = new GalPipelineState
            {
                FrontFace = GalFrontFace.CCW,

                CullFaceEnabled = false,
                CullFace = GalCullFace.Back,

                DepthTestEnabled = false,
                DepthClear = 1f,
                DepthFunc = GalComparisonOp.Less,

                StencilTestEnabled = false,
                StencilClear = 0,

                StencilBackFuncFunc = GalComparisonOp.Always,
                StencilBackFuncRef = 0,
                StencilBackFuncMask = UInt32.MaxValue,
                StencilBackOpFail = GalStencilOp.Keep,
                StencilBackOpZFail = GalStencilOp.Keep,
                StencilBackOpZPass = GalStencilOp.Keep,
                StencilBackMask = UInt32.MaxValue,

                StencilFrontFuncFunc = GalComparisonOp.Always,
                StencilFrontFuncRef = 0,
                StencilFrontFuncMask = UInt32.MaxValue,
                StencilFrontOpFail = GalStencilOp.Keep,
                StencilFrontOpZFail = GalStencilOp.Keep,
                StencilFrontOpZPass = GalStencilOp.Keep,
                StencilFrontMask = UInt32.MaxValue,

                BlendEnabled = false,
                BlendSeparateAlpha = false,

                BlendEquationRgb = 0,
                BlendFuncSrcRgb = GalBlendFactor.One,
                BlendFuncDstRgb = GalBlendFactor.Zero,
                BlendEquationAlpha = 0,
                BlendFuncSrcAlpha = GalBlendFactor.One,
                BlendFuncDstAlpha = GalBlendFactor.Zero,

                PrimitiveRestartEnabled = false,
                PrimitiveRestartIndex = 0
            };
        }

        public void Bind(GalPipelineState S)
        {
            //O stands for Older, S for (current) State

            BindConstBuffers(S);

            BindVertexLayout(S);

            if (S.FlipX != O.FlipX || S.FlipY != O.FlipY)
            {
                Shader.SetFlip(S.FlipX, S.FlipY);
            }

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

        private void BindConstBuffers(GalPipelineState S)
        {
            //Index 0 is reserved
            int FreeBinding = 1;

            void BindIfNotNull(OGLShaderStage Stage)
            {
                if (Stage != null)
                {
                    foreach (ShaderDeclInfo DeclInfo in Stage.UniformUsage)
                    {
                        long Key = S.ConstBufferKeys[(int)Stage.Type][DeclInfo.Cbuf];

                        if (Key != 0 && Key != O.ConstBufferKeys[(int)Stage.Type][DeclInfo.Cbuf])
                        {
                            if (Buffer.TryGetUbo(Key, out int UboHandle))
                            {
                                GL.BindBufferBase(BufferRangeTarget.UniformBuffer, FreeBinding, UboHandle);
                            }
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

        private void BindVertexLayout(GalPipelineState S)
        {
            foreach (GalVertexBinding Binding in S.VertexBindings)
            {
                if (!Binding.Enabled || !Rasterizer.TryGetVbo(Binding.VboKey, out int VboHandle))
                {
                    continue;
                }

                if (VaoHandle == 0)
                {
                    VaoHandle = GL.GenVertexArray();

                    //Vertex arrays shouldn't be used anywhere else in OpenGL's backend
                    //if you want to use it, move this line out of the if
                    GL.BindVertexArray(VaoHandle);
                }

                foreach (GalVertexAttrib Attrib in Binding.Attribs)
                {
                    GL.EnableVertexAttribArray(Attrib.Index);

                    GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);

                    bool Unsigned =
                        Attrib.Type == GalVertexAttribType.Unorm ||
                        Attrib.Type == GalVertexAttribType.Uint ||
                        Attrib.Type == GalVertexAttribType.Uscaled;

                    bool Normalize =
                        Attrib.Type == GalVertexAttribType.Snorm ||
                        Attrib.Type == GalVertexAttribType.Unorm;

                    VertexAttribPointerType Type = 0;

                    if (Attrib.Type == GalVertexAttribType.Float)
                    {
                        Type = VertexAttribPointerType.Float;
                    }
                    else
                    {
                        Type = AttribTypes[Attrib.Size] + (Unsigned ? 1 : 0);
                    }

                    int Size = AttribElements[Attrib.Size];
                    int Offset = Attrib.Offset;

                    GL.VertexAttribPointer(Attrib.Index, Size, Type, Normalize, Binding.Stride, Offset);
                }
            }
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