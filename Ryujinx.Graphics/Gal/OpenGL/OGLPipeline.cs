using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    internal class OGLPipeline : IGalPipeline
    {
        private static Dictionary<GalVertexAttribSize, int> _attribElements =
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

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> _floatAttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.Float     },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.Float     },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.HalfFloat },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.Float     },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.HalfFloat },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.HalfFloat },
            { GalVertexAttribSize._32,          VertexAttribPointerType.Float     },
            { GalVertexAttribSize._16,          VertexAttribPointerType.HalfFloat }
        };

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> _signedAttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.Int           },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.Int           },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.Short         },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.Int           },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.Short         },
            { GalVertexAttribSize._8_8_8_8,     VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.Short         },
            { GalVertexAttribSize._32,          VertexAttribPointerType.Int           },
            { GalVertexAttribSize._8_8_8,       VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._8_8,         VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._16,          VertexAttribPointerType.Short         },
            { GalVertexAttribSize._8,           VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._10_10_10_2,  VertexAttribPointerType.Int2101010Rev }
        };

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> _unsignedAttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._8_8_8_8,     VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._32,          VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._8_8_8,       VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._8_8,         VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._16,          VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._8,           VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._10_10_10_2,  VertexAttribPointerType.UnsignedInt2101010Rev   },
            { GalVertexAttribSize._11_11_10,    VertexAttribPointerType.UnsignedInt10F11F11FRev }
        };

        private GalPipelineState _old;

        private OGLConstBuffer _buffer;
        private OGLRasterizer _rasterizer;
        private OGLShader _shader;

        private int _vaoHandle;

        public OGLPipeline(OGLConstBuffer buffer, OGLRasterizer rasterizer, OGLShader shader)
        {
            _buffer     = buffer;
            _rasterizer = rasterizer;
            _shader     = shader;

            //These values match OpenGL's defaults
            _old = new GalPipelineState
            {
                FrontFace = GalFrontFace.Ccw,

                CullFaceEnabled = false,
                CullFace        = GalCullFace.Back,

                DepthTestEnabled  = false,
                DepthWriteEnabled = true,
                DepthFunc         = GalComparisonOp.Less,
                DepthRangeNear    = 0,
                DepthRangeFar     = 1,

                StencilTestEnabled = false,

                StencilBackFuncFunc = GalComparisonOp.Always,
                StencilBackFuncRef  = 0,
                StencilBackFuncMask = UInt32.MaxValue,
                StencilBackOpFail   = GalStencilOp.Keep,
                StencilBackOpZFail  = GalStencilOp.Keep,
                StencilBackOpZPass  = GalStencilOp.Keep,
                StencilBackMask     = UInt32.MaxValue,

                StencilFrontFuncFunc = GalComparisonOp.Always,
                StencilFrontFuncRef  = 0,
                StencilFrontFuncMask = UInt32.MaxValue,
                StencilFrontOpFail   = GalStencilOp.Keep,
                StencilFrontOpZFail  = GalStencilOp.Keep,
                StencilFrontOpZPass  = GalStencilOp.Keep,
                StencilFrontMask     = UInt32.MaxValue,

                BlendEnabled       = false,
                BlendSeparateAlpha = false,

                BlendEquationRgb   = 0,
                BlendFuncSrcRgb    = GalBlendFactor.One,
                BlendFuncDstRgb    = GalBlendFactor.Zero,
                BlendEquationAlpha = 0,
                BlendFuncSrcAlpha  = GalBlendFactor.One,
                BlendFuncDstAlpha  = GalBlendFactor.Zero,

                PrimitiveRestartEnabled = false,
                PrimitiveRestartIndex   = 0
            };

            for (int index = 0; index < GalPipelineState.RenderTargetsCount; index++) _old.ColorMasks[index] = ColorMaskRgba.Default;
        }

        public void Bind(GalPipelineState New)
        {
            BindConstBuffers(New);

            BindVertexLayout(New);

            if (New.FramebufferSrgb != _old.FramebufferSrgb) Enable(EnableCap.FramebufferSrgb, New.FramebufferSrgb);

            if (New.FlipX != _old.FlipX || New.FlipY != _old.FlipY || New.Instance != _old.Instance) _shader.SetExtraData(New.FlipX, New.FlipY, New.Instance);

            //Note: Uncomment SetFrontFace and SetCullFace when flipping issues are solved

            //if (New.FrontFace != Old.FrontFace)
            //{
            //    GL.FrontFace(OGLEnumConverter.GetFrontFace(New.FrontFace));
            //}

            //if (New.CullFaceEnabled != Old.CullFaceEnabled)
            //{
            //    Enable(EnableCap.CullFace, New.CullFaceEnabled);
            //}

            //if (New.CullFaceEnabled)
            //{
            //    if (New.CullFace != Old.CullFace)
            //    {
            //        GL.CullFace(OGLEnumConverter.GetCullFace(New.CullFace));
            //    }
            //}

            if (New.DepthTestEnabled != _old.DepthTestEnabled) Enable(EnableCap.DepthTest, New.DepthTestEnabled);

            if (New.DepthWriteEnabled != _old.DepthWriteEnabled) GL.DepthMask(New.DepthWriteEnabled);

            if (New.DepthTestEnabled)
                if (New.DepthFunc != _old.DepthFunc) GL.DepthFunc(OGLEnumConverter.GetDepthFunc(New.DepthFunc));

            if (New.DepthRangeNear != _old.DepthRangeNear ||
                New.DepthRangeFar  != _old.DepthRangeFar)
                GL.DepthRange(New.DepthRangeNear, New.DepthRangeFar);

            if (New.StencilTestEnabled != _old.StencilTestEnabled) Enable(EnableCap.StencilTest, New.StencilTestEnabled);

            if (New.StencilTwoSideEnabled != _old.StencilTwoSideEnabled) Enable((EnableCap)All.StencilTestTwoSideExt, New.StencilTwoSideEnabled);

            if (New.StencilTestEnabled)
            {
                if (New.StencilBackFuncFunc != _old.StencilBackFuncFunc ||
                    New.StencilBackFuncRef  != _old.StencilBackFuncRef  ||
                    New.StencilBackFuncMask != _old.StencilBackFuncMask)
                    GL.StencilFuncSeparate(
                        StencilFace.Back,
                        OGLEnumConverter.GetStencilFunc(New.StencilBackFuncFunc),
                        New.StencilBackFuncRef,
                        New.StencilBackFuncMask);

                if (New.StencilBackOpFail  != _old.StencilBackOpFail  ||
                    New.StencilBackOpZFail != _old.StencilBackOpZFail ||
                    New.StencilBackOpZPass != _old.StencilBackOpZPass)
                    GL.StencilOpSeparate(
                        StencilFace.Back,
                        OGLEnumConverter.GetStencilOp(New.StencilBackOpFail),
                        OGLEnumConverter.GetStencilOp(New.StencilBackOpZFail),
                        OGLEnumConverter.GetStencilOp(New.StencilBackOpZPass));

                if (New.StencilBackMask != _old.StencilBackMask) GL.StencilMaskSeparate(StencilFace.Back, New.StencilBackMask);

                if (New.StencilFrontFuncFunc != _old.StencilFrontFuncFunc ||
                    New.StencilFrontFuncRef  != _old.StencilFrontFuncRef  ||
                    New.StencilFrontFuncMask != _old.StencilFrontFuncMask)
                    GL.StencilFuncSeparate(
                        StencilFace.Front,
                        OGLEnumConverter.GetStencilFunc(New.StencilFrontFuncFunc),
                        New.StencilFrontFuncRef,
                        New.StencilFrontFuncMask);

                if (New.StencilFrontOpFail  != _old.StencilFrontOpFail  ||
                    New.StencilFrontOpZFail != _old.StencilFrontOpZFail ||
                    New.StencilFrontOpZPass != _old.StencilFrontOpZPass)
                    GL.StencilOpSeparate(
                        StencilFace.Front,
                        OGLEnumConverter.GetStencilOp(New.StencilFrontOpFail),
                        OGLEnumConverter.GetStencilOp(New.StencilFrontOpZFail),
                        OGLEnumConverter.GetStencilOp(New.StencilFrontOpZPass));

                if (New.StencilFrontMask != _old.StencilFrontMask) GL.StencilMaskSeparate(StencilFace.Front, New.StencilFrontMask);
            }

            if (New.BlendEnabled != _old.BlendEnabled) Enable(EnableCap.Blend, New.BlendEnabled);

            if (New.BlendEnabled)
            {
                if (New.BlendSeparateAlpha)
                {
                    if (New.BlendEquationRgb   != _old.BlendEquationRgb ||
                        New.BlendEquationAlpha != _old.BlendEquationAlpha)
                        GL.BlendEquationSeparate(
                            OGLEnumConverter.GetBlendEquation(New.BlendEquationRgb),
                            OGLEnumConverter.GetBlendEquation(New.BlendEquationAlpha));

                    if (New.BlendFuncSrcRgb   != _old.BlendFuncSrcRgb   ||
                        New.BlendFuncDstRgb   != _old.BlendFuncDstRgb   ||
                        New.BlendFuncSrcAlpha != _old.BlendFuncSrcAlpha ||
                        New.BlendFuncDstAlpha != _old.BlendFuncDstAlpha)
                        GL.BlendFuncSeparate(
                            (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(New.BlendFuncSrcRgb),
                            (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(New.BlendFuncDstRgb),
                            (BlendingFactorSrc) OGLEnumConverter.GetBlendFactor(New.BlendFuncSrcAlpha),
                            (BlendingFactorDest)OGLEnumConverter.GetBlendFactor(New.BlendFuncDstAlpha));
                }
                else
                {
                    if (New.BlendEquationRgb != _old.BlendEquationRgb) GL.BlendEquation(OGLEnumConverter.GetBlendEquation(New.BlendEquationRgb));

                    if (New.BlendFuncSrcRgb != _old.BlendFuncSrcRgb ||
                        New.BlendFuncDstRgb != _old.BlendFuncDstRgb)
                        GL.BlendFunc(
                            OGLEnumConverter.GetBlendFactor(New.BlendFuncSrcRgb),
                            OGLEnumConverter.GetBlendFactor(New.BlendFuncDstRgb));
                }
            }

            if (New.ColorMaskCommon)
            {
                if (New.ColorMaskCommon != _old.ColorMaskCommon || !New.ColorMasks[0].Equals(_old.ColorMasks[0]))
                    GL.ColorMask(
                        New.ColorMasks[0].Red,
                        New.ColorMasks[0].Green,
                        New.ColorMasks[0].Blue,
                        New.ColorMasks[0].Alpha);
            }
            else
            {
                for (int index = 0; index < GalPipelineState.RenderTargetsCount; index++)
                    if (!New.ColorMasks[index].Equals(_old.ColorMasks[index]))
                        GL.ColorMask(
                            index,
                            New.ColorMasks[index].Red,
                            New.ColorMasks[index].Green,
                            New.ColorMasks[index].Blue,
                            New.ColorMasks[index].Alpha);
            }

            if (New.PrimitiveRestartEnabled != _old.PrimitiveRestartEnabled) Enable(EnableCap.PrimitiveRestart, New.PrimitiveRestartEnabled);

            if (New.PrimitiveRestartEnabled)
                if (New.PrimitiveRestartIndex != _old.PrimitiveRestartIndex) GL.PrimitiveRestartIndex(New.PrimitiveRestartIndex);

            _old = New;
        }

        private void BindConstBuffers(GalPipelineState New)
        {
            int freeBinding = OGLShader.ReservedCbufCount;

            void BindIfNotNull(OGLShaderStage stage)
            {
                if (stage != null)
                    foreach (ShaderDeclInfo declInfo in stage.ConstBufferUsage)
                    {
                        long key = New.ConstBufferKeys[(int)stage.Type][declInfo.Cbuf];

                        if (key != 0 && _buffer.TryGetUbo(key, out int uboHandle)) GL.BindBufferBase(BufferRangeTarget.UniformBuffer, freeBinding, uboHandle);

                        freeBinding++;
                    }
            }

            BindIfNotNull(_shader.Current.Vertex);
            BindIfNotNull(_shader.Current.TessControl);
            BindIfNotNull(_shader.Current.TessEvaluation);
            BindIfNotNull(_shader.Current.Geometry);
            BindIfNotNull(_shader.Current.Fragment);
        }

        private void BindVertexLayout(GalPipelineState New)
        {
            foreach (GalVertexBinding binding in New.VertexBindings)
            {
                if (!binding.Enabled || !_rasterizer.TryGetVbo(binding.VboKey, out int vboHandle)) continue;

                if (_vaoHandle == 0)
                {
                    _vaoHandle = GL.GenVertexArray();

                    //Vertex arrays shouldn't be used anywhere else in OpenGL's backend
                    //if you want to use it, move this line out of the if
                    GL.BindVertexArray(_vaoHandle);
                }

                foreach (GalVertexAttrib attrib in binding.Attribs)
                {
                    //Skip uninitialized attributes.
                    if (attrib.Size == 0) continue;

                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);

                    bool unsigned =
                        attrib.Type == GalVertexAttribType.Unorm ||
                        attrib.Type == GalVertexAttribType.Uint  ||
                        attrib.Type == GalVertexAttribType.Uscaled;

                    bool normalize =
                        attrib.Type == GalVertexAttribType.Snorm ||
                        attrib.Type == GalVertexAttribType.Unorm;

                    VertexAttribPointerType type = 0;

                    if (attrib.Type == GalVertexAttribType.Float)
                    {
                        type = GetType(_floatAttribTypes, attrib);
                    }
                    else
                    {
                        if (unsigned)
                            type = GetType(_unsignedAttribTypes, attrib);
                        else
                            type = GetType(_signedAttribTypes, attrib);
                    }

                    if (!_attribElements.TryGetValue(attrib.Size, out int size)) throw new InvalidOperationException("Invalid attribute size \"" + attrib.Size + "\"!");

                    int offset = attrib.Offset;

                    if (binding.Stride != 0)
                    {
                        GL.EnableVertexAttribArray(attrib.Index);

                        if (attrib.Type == GalVertexAttribType.Sint ||
                            attrib.Type == GalVertexAttribType.Uint)
                        {
                            IntPtr pointer = new IntPtr(offset);

                            VertexAttribIntegerType integerType = (VertexAttribIntegerType)type;

                            GL.VertexAttribIPointer(attrib.Index, size, integerType, binding.Stride, pointer);
                        }
                        else
                        {
                            GL.VertexAttribPointer(attrib.Index, size, type, normalize, binding.Stride, offset);
                        }
                    }
                    else
                    {
                        GL.DisableVertexAttribArray(attrib.Index);

                        SetConstAttrib(attrib);
                    }

                    if (binding.Instanced && binding.Divisor != 0)
                        GL.VertexAttribDivisor(attrib.Index, 1);
                    else
                        GL.VertexAttribDivisor(attrib.Index, 0);
                }
            }
        }

        private static VertexAttribPointerType GetType(Dictionary<GalVertexAttribSize, VertexAttribPointerType> dict, GalVertexAttrib attrib)
        {
            if (!dict.TryGetValue(attrib.Size, out VertexAttribPointerType type)) ThrowUnsupportedAttrib(attrib);

            return type;
        }

        private static unsafe void SetConstAttrib(GalVertexAttrib attrib)
        {
            if (attrib.Size == GalVertexAttribSize._10_10_10_2 ||
                attrib.Size == GalVertexAttribSize._11_11_10)
                ThrowUnsupportedAttrib(attrib);

            if (attrib.Type == GalVertexAttribType.Unorm)
                switch (attrib.Size)
                {
                    case GalVertexAttribSize._8:
                    case GalVertexAttribSize._8_8:
                    case GalVertexAttribSize._8_8_8:
                    case GalVertexAttribSize._8_8_8_8:
                        GL.VertexAttrib4N((uint)attrib.Index, (byte*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._16:
                    case GalVertexAttribSize._16_16:
                    case GalVertexAttribSize._16_16_16:
                    case GalVertexAttribSize._16_16_16_16:
                        GL.VertexAttrib4N((uint)attrib.Index, (ushort*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._32:
                    case GalVertexAttribSize._32_32:
                    case GalVertexAttribSize._32_32_32:
                    case GalVertexAttribSize._32_32_32_32:
                        GL.VertexAttrib4N((uint)attrib.Index, (uint*)attrib.Pointer);
                        break;
                }
            else if (attrib.Type == GalVertexAttribType.Snorm)
                switch (attrib.Size)
                {
                    case GalVertexAttribSize._8:
                    case GalVertexAttribSize._8_8:
                    case GalVertexAttribSize._8_8_8:
                    case GalVertexAttribSize._8_8_8_8:
                        GL.VertexAttrib4N((uint)attrib.Index, (sbyte*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._16:
                    case GalVertexAttribSize._16_16:
                    case GalVertexAttribSize._16_16_16:
                    case GalVertexAttribSize._16_16_16_16:
                        GL.VertexAttrib4N((uint)attrib.Index, (short*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._32:
                    case GalVertexAttribSize._32_32:
                    case GalVertexAttribSize._32_32_32:
                    case GalVertexAttribSize._32_32_32_32:
                        GL.VertexAttrib4N((uint)attrib.Index, (int*)attrib.Pointer);
                        break;
                }
            else if (attrib.Type == GalVertexAttribType.Uint)
                switch (attrib.Size)
                {
                    case GalVertexAttribSize._8:
                    case GalVertexAttribSize._8_8:
                    case GalVertexAttribSize._8_8_8:
                    case GalVertexAttribSize._8_8_8_8:
                        GL.VertexAttribI4((uint)attrib.Index, (byte*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._16:
                    case GalVertexAttribSize._16_16:
                    case GalVertexAttribSize._16_16_16:
                    case GalVertexAttribSize._16_16_16_16:
                        GL.VertexAttribI4((uint)attrib.Index, (ushort*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._32:
                    case GalVertexAttribSize._32_32:
                    case GalVertexAttribSize._32_32_32:
                    case GalVertexAttribSize._32_32_32_32:
                        GL.VertexAttribI4((uint)attrib.Index, (uint*)attrib.Pointer);
                        break;
                }
            else if (attrib.Type == GalVertexAttribType.Sint)
                switch (attrib.Size)
                {
                    case GalVertexAttribSize._8:
                    case GalVertexAttribSize._8_8:
                    case GalVertexAttribSize._8_8_8:
                    case GalVertexAttribSize._8_8_8_8:
                        GL.VertexAttribI4((uint)attrib.Index, (sbyte*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._16:
                    case GalVertexAttribSize._16_16:
                    case GalVertexAttribSize._16_16_16:
                    case GalVertexAttribSize._16_16_16_16:
                        GL.VertexAttribI4((uint)attrib.Index, (short*)attrib.Pointer);
                        break;

                    case GalVertexAttribSize._32:
                    case GalVertexAttribSize._32_32:
                    case GalVertexAttribSize._32_32_32:
                    case GalVertexAttribSize._32_32_32_32:
                        GL.VertexAttribI4((uint)attrib.Index, (int*)attrib.Pointer);
                        break;
                }
            else if (attrib.Type == GalVertexAttribType.Float)
                switch (attrib.Size)
                {
                    case GalVertexAttribSize._32:
                    case GalVertexAttribSize._32_32:
                    case GalVertexAttribSize._32_32_32:
                    case GalVertexAttribSize._32_32_32_32:
                        GL.VertexAttrib4(attrib.Index, (float*)attrib.Pointer);
                        break;

                    default: ThrowUnsupportedAttrib(attrib); break;
                }
        }

        private static void ThrowUnsupportedAttrib(GalVertexAttrib attrib)
        {
            throw new NotImplementedException("Unsupported size \"" + attrib.Size + "\" on type \"" + attrib.Type + "\"!");
        }

        private void Enable(EnableCap cap, bool enabled)
        {
            if (enabled)
                GL.Enable(cap);
            else
                GL.Disable(cap);
        }

        public void ResetDepthMask()
        {
            _old.DepthWriteEnabled = true;
        }

        public void ResetColorMask(int index)
        {
            _old.ColorMasks[index] = ColorMaskRgba.Default;
        }
    }
}