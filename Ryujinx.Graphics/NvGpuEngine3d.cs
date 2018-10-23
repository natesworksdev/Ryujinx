using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public class NvGpuEngine3D : INvGpuEngine
    {
        public int[] Registers { get; private set; }

        private NvGpu _gpu;

        private Dictionary<int, NvGpuMethod> _methods;

        private struct ConstBuffer
        {
            public bool Enabled;
            public long Position;
            public int  Size;
        }

        private ConstBuffer[][] _constBuffers;

        private List<long>[] _uploadedKeys;

        private int _currentInstance = 0;

        public NvGpuEngine3D(NvGpu gpu)
        {
            this._gpu = gpu;

            Registers = new int[0xe00];

            _methods = new Dictionary<int, NvGpuMethod>();

            void AddMethod(int meth, int count, int stride, NvGpuMethod method)
            {
                while (count-- > 0)
                {
                    _methods.Add(meth, method);

                    meth += stride;
                }
            }

            AddMethod(0x585,  1, 1, VertexEndGl);
            AddMethod(0x674,  1, 1, ClearBuffers);
            AddMethod(0x6c3,  1, 1, QueryControl);
            AddMethod(0x8e4, 16, 1, CbData);
            AddMethod(0x904,  5, 8, CbBind);

            _constBuffers = new ConstBuffer[6][];

            for (int index = 0; index < _constBuffers.Length; index++)
            {
                _constBuffers[index] = new ConstBuffer[18];
            }

            _uploadedKeys = new List<long>[(int)NvGpuBufferType.Count];

            for (int i = 0; i < _uploadedKeys.Length; i++)
            {
                _uploadedKeys[i] = new List<long>();
            }
        }

        public void CallMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            if (_methods.TryGetValue(pbEntry.Method, out NvGpuMethod method))
            {
                method(vmm, pbEntry);
            }
            else
            {
                WriteRegister(pbEntry);
            }
        }

        public void ResetCache()
        {
            foreach (List<long> uploaded in _uploadedKeys)
            {
                uploaded.Clear();
            }
        }

        private void VertexEndGl(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            LockCaches();

            GalPipelineState state = new GalPipelineState();

            SetFrameBuffer(state);
            SetFrontFace(state);
            SetCullFace(state);
            SetDepth(state);
            SetStencil(state);
            SetBlending(state);
            SetColorMask(state);
            SetPrimitiveRestart(state);

            for (int fbIndex = 0; fbIndex < 8; fbIndex++)
            {
                SetFrameBuffer(vmm, fbIndex);
            }

            SetZeta(vmm);

            SetRenderTargets();

            long[] keys = UploadShaders(vmm);

            _gpu.Renderer.Shader.BindProgram();

            UploadTextures(vmm, state, keys);
            UploadConstBuffers(vmm, state, keys);
            UploadVertexArrays(vmm, state);

            DispatchRender(vmm, state);

            UnlockCaches();
        }

        private void LockCaches()
        {
            _gpu.Renderer.Buffer.LockCache();
            _gpu.Renderer.Rasterizer.LockCaches();
            _gpu.Renderer.Texture.LockCache();
        }

        private void UnlockCaches()
        {
            _gpu.Renderer.Buffer.UnlockCache();
            _gpu.Renderer.Rasterizer.UnlockCaches();
            _gpu.Renderer.Texture.UnlockCache();
        }

        private void ClearBuffers(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            int arg0 = pbEntry.Arguments[0];

            int attachment = (arg0 >> 6) & 0xf;

            GalClearBufferFlags flags = (GalClearBufferFlags)(arg0 & 0x3f);

            float red   = ReadRegisterFloat(NvGpuEngine3DReg.ClearNColor + 0);
            float green = ReadRegisterFloat(NvGpuEngine3DReg.ClearNColor + 1);
            float blue  = ReadRegisterFloat(NvGpuEngine3DReg.ClearNColor + 2);
            float alpha = ReadRegisterFloat(NvGpuEngine3DReg.ClearNColor + 3);

            float depth = ReadRegisterFloat(NvGpuEngine3DReg.ClearDepth);

            int stencil = ReadRegister(NvGpuEngine3DReg.ClearStencil);

            SetFrameBuffer(vmm, attachment);

            SetZeta(vmm);

            SetRenderTargets();

            _gpu.Renderer.RenderTarget.Bind();

            _gpu.Renderer.Rasterizer.ClearBuffers(flags, attachment, red, green, blue, alpha, depth, stencil);

            _gpu.Renderer.Pipeline.ResetDepthMask();
            _gpu.Renderer.Pipeline.ResetColorMask(attachment);
        }

        private void SetFrameBuffer(NvGpuVmm vmm, int fbIndex)
        {
            long va = MakeInt64From2XInt32(NvGpuEngine3DReg.FrameBufferNAddress + fbIndex * 0x10);

            int surfFormat = ReadRegister(NvGpuEngine3DReg.FrameBufferNFormat + fbIndex * 0x10);

            if (va == 0 || surfFormat == 0)
            {
                _gpu.Renderer.RenderTarget.UnbindColor(fbIndex);

                return;
            }

            long key = vmm.GetPhysicalAddress(va);

            int width  = ReadRegister(NvGpuEngine3DReg.FrameBufferNWidth  + fbIndex * 0x10);
            int height = ReadRegister(NvGpuEngine3DReg.FrameBufferNHeight + fbIndex * 0x10);

            int blockDim = ReadRegister(NvGpuEngine3DReg.FrameBufferNBlockDim + fbIndex * 0x10);

            int gobBlockHeight = 1 << ((blockDim >> 4) & 7);

            GalMemoryLayout layout = (GalMemoryLayout)((blockDim >> 12) & 1);

            float tx = ReadRegisterFloat(NvGpuEngine3DReg.ViewportNTranslateX + fbIndex * 8);
            float ty = ReadRegisterFloat(NvGpuEngine3DReg.ViewportNTranslateY + fbIndex * 8);

            float sx = ReadRegisterFloat(NvGpuEngine3DReg.ViewportNScaleX + fbIndex * 8);
            float sy = ReadRegisterFloat(NvGpuEngine3DReg.ViewportNScaleY + fbIndex * 8);

            int vpX = (int)MathF.Max(0, tx - MathF.Abs(sx));
            int vpY = (int)MathF.Max(0, ty - MathF.Abs(sy));

            int vpW = (int)(tx + MathF.Abs(sx)) - vpX;
            int vpH = (int)(ty + MathF.Abs(sy)) - vpY;

            GalImageFormat format = ImageUtils.ConvertSurface((GalSurfaceFormat)surfFormat);

            GalImage image = new GalImage(width, height, 1, gobBlockHeight, layout, format);

            _gpu.ResourceManager.SendColorBuffer(vmm, key, fbIndex, image);

            _gpu.Renderer.RenderTarget.SetViewport(fbIndex, vpX, vpY, vpW, vpH);
        }

        private void SetFrameBuffer(GalPipelineState state)
        {
            state.FramebufferSrgb = ReadRegisterBool(NvGpuEngine3DReg.FrameBufferSrgb);

            state.FlipX = GetFlipSign(NvGpuEngine3DReg.ViewportNScaleX);
            state.FlipY = GetFlipSign(NvGpuEngine3DReg.ViewportNScaleY);
        }

        private void SetZeta(NvGpuVmm vmm)
        {
            long va = MakeInt64From2XInt32(NvGpuEngine3DReg.ZetaAddress);

            int zetaFormat = ReadRegister(NvGpuEngine3DReg.ZetaFormat);

            int blockDim = ReadRegister(NvGpuEngine3DReg.ZetaBlockDimensions);

            int gobBlockHeight = 1 << ((blockDim >> 4) & 7);

            GalMemoryLayout layout = (GalMemoryLayout)((blockDim >> 12) & 1); //?

            bool zetaEnable = ReadRegisterBool(NvGpuEngine3DReg.ZetaEnable);

            if (va == 0 || zetaFormat == 0 || !zetaEnable)
            {
                _gpu.Renderer.RenderTarget.UnbindZeta();

                return;
            }

            long key = vmm.GetPhysicalAddress(va);

            int width  = ReadRegister(NvGpuEngine3DReg.ZetaHoriz);
            int height = ReadRegister(NvGpuEngine3DReg.ZetaVert);

            GalImageFormat format = ImageUtils.ConvertZeta((GalZetaFormat)zetaFormat);

            GalImage image = new GalImage(width, height, 1, gobBlockHeight, layout, format);

            _gpu.ResourceManager.SendZetaBuffer(vmm, key, image);
        }

        private long[] UploadShaders(NvGpuVmm vmm)
        {
            long[] keys = new long[5];

            long basePosition = MakeInt64From2XInt32(NvGpuEngine3DReg.ShaderAddress);

            int index = 1;

            int vpAControl = ReadRegister(NvGpuEngine3DReg.ShaderNControl);

            bool vpAEnable = (vpAControl & 1) != 0;

            if (vpAEnable)
            {
                //Note: The maxwell supports 2 vertex programs, usually
                //only VP B is used, but in some cases VP A is also used.
                //In this case, it seems to function as an extra vertex
                //shader stage.
                //The graphics abstraction layer has a special overload for this
                //case, which should merge the two shaders into one vertex shader.
                int vpAOffset = ReadRegister(NvGpuEngine3DReg.ShaderNOffset);
                int vpBOffset = ReadRegister(NvGpuEngine3DReg.ShaderNOffset + 0x10);

                long vpAPos = basePosition + (uint)vpAOffset;
                long vpBPos = basePosition + (uint)vpBOffset;

                keys[(int)GalShaderType.Vertex] = vpBPos;

                _gpu.Renderer.Shader.Create(vmm, vpAPos, vpBPos, GalShaderType.Vertex);
                _gpu.Renderer.Shader.Bind(vpBPos);

                index = 2;
            }

            for (; index < 6; index++)
            {
                GalShaderType type = GetTypeFromProgram(index);

                int control = ReadRegister(NvGpuEngine3DReg.ShaderNControl + index * 0x10);
                int offset  = ReadRegister(NvGpuEngine3DReg.ShaderNOffset  + index * 0x10);

                //Note: Vertex Program (B) is always enabled.
                bool enable = (control & 1) != 0 || index == 1;

                if (!enable)
                {
                    _gpu.Renderer.Shader.Unbind(type);

                    continue;
                }

                long key = basePosition + (uint)offset;

                keys[(int)type] = key;

                _gpu.Renderer.Shader.Create(vmm, key, type);
                _gpu.Renderer.Shader.Bind(key);
            }

            return keys;
        }

        private static GalShaderType GetTypeFromProgram(int program)
        {
            switch (program)
            {
                case 0:
                case 1: return GalShaderType.Vertex;
                case 2: return GalShaderType.TessControl;
                case 3: return GalShaderType.TessEvaluation;
                case 4: return GalShaderType.Geometry;
                case 5: return GalShaderType.Fragment;
            }

            throw new ArgumentOutOfRangeException(nameof(program));
        }

        private void SetFrontFace(GalPipelineState state)
        {
            float signX = GetFlipSign(NvGpuEngine3DReg.ViewportNScaleX);
            float signY = GetFlipSign(NvGpuEngine3DReg.ViewportNScaleY);

            GalFrontFace frontFace = (GalFrontFace)ReadRegister(NvGpuEngine3DReg.FrontFace);

            //Flipping breaks facing. Flipping front facing too fixes it
            if (signX != signY)
            {
                switch (frontFace)
                {
                    case GalFrontFace.Cw:  frontFace = GalFrontFace.Ccw; break;
                    case GalFrontFace.Ccw: frontFace = GalFrontFace.Cw;  break;
                }
            }

            state.FrontFace = frontFace;
        }

        private void SetCullFace(GalPipelineState state)
        {
            state.CullFaceEnabled = ReadRegisterBool(NvGpuEngine3DReg.CullFaceEnable);

            if (state.CullFaceEnabled)
            {
                state.CullFace = (GalCullFace)ReadRegister(NvGpuEngine3DReg.CullFace);
            }
        }

        private void SetDepth(GalPipelineState state)
        {
            state.DepthTestEnabled = ReadRegisterBool(NvGpuEngine3DReg.DepthTestEnable);

            state.DepthWriteEnabled = ReadRegisterBool(NvGpuEngine3DReg.DepthWriteEnable);

            if (state.DepthTestEnabled)
            {
                state.DepthFunc = (GalComparisonOp)ReadRegister(NvGpuEngine3DReg.DepthTestFunction);
            }

            state.DepthRangeNear = ReadRegisterFloat(NvGpuEngine3DReg.DepthRangeNNear);
            state.DepthRangeFar  = ReadRegisterFloat(NvGpuEngine3DReg.DepthRangeNFar);
        }

        private void SetStencil(GalPipelineState state)
        {
            state.StencilTestEnabled = ReadRegisterBool(NvGpuEngine3DReg.StencilEnable);

            if (state.StencilTestEnabled)
            {
                state.StencilBackFuncFunc = (GalComparisonOp)ReadRegister(NvGpuEngine3DReg.StencilBackFuncFunc);
                state.StencilBackFuncRef  =                  ReadRegister(NvGpuEngine3DReg.StencilBackFuncRef);
                state.StencilBackFuncMask =            (uint)ReadRegister(NvGpuEngine3DReg.StencilBackFuncMask);
                state.StencilBackOpFail   =    (GalStencilOp)ReadRegister(NvGpuEngine3DReg.StencilBackOpFail);
                state.StencilBackOpZFail  =    (GalStencilOp)ReadRegister(NvGpuEngine3DReg.StencilBackOpZFail);
                state.StencilBackOpZPass  =    (GalStencilOp)ReadRegister(NvGpuEngine3DReg.StencilBackOpZPass);
                state.StencilBackMask     =            (uint)ReadRegister(NvGpuEngine3DReg.StencilBackMask);

                state.StencilFrontFuncFunc = (GalComparisonOp)ReadRegister(NvGpuEngine3DReg.StencilFrontFuncFunc);
                state.StencilFrontFuncRef  =                  ReadRegister(NvGpuEngine3DReg.StencilFrontFuncRef);
                state.StencilFrontFuncMask =            (uint)ReadRegister(NvGpuEngine3DReg.StencilFrontFuncMask);
                state.StencilFrontOpFail   =    (GalStencilOp)ReadRegister(NvGpuEngine3DReg.StencilFrontOpFail);
                state.StencilFrontOpZFail  =    (GalStencilOp)ReadRegister(NvGpuEngine3DReg.StencilFrontOpZFail);
                state.StencilFrontOpZPass  =    (GalStencilOp)ReadRegister(NvGpuEngine3DReg.StencilFrontOpZPass);
                state.StencilFrontMask     =            (uint)ReadRegister(NvGpuEngine3DReg.StencilFrontMask);
            }
        }

        private void SetBlending(GalPipelineState state)
        {
            //TODO: Support independent blend properly.
            state.BlendEnabled = ReadRegisterBool(NvGpuEngine3DReg.BlendNEnable);

            if (state.BlendEnabled)
            {
                state.BlendSeparateAlpha = ReadRegisterBool(NvGpuEngine3DReg.BlendNSeparateAlpha);

                state.BlendEquationRgb   = (GalBlendEquation)ReadRegister(NvGpuEngine3DReg.BlendNEquationRgb);
                state.BlendFuncSrcRgb    =   (GalBlendFactor)ReadRegister(NvGpuEngine3DReg.BlendNFuncSrcRgb);
                state.BlendFuncDstRgb    =   (GalBlendFactor)ReadRegister(NvGpuEngine3DReg.BlendNFuncDstRgb);
                state.BlendEquationAlpha = (GalBlendEquation)ReadRegister(NvGpuEngine3DReg.BlendNEquationAlpha);
                state.BlendFuncSrcAlpha  =   (GalBlendFactor)ReadRegister(NvGpuEngine3DReg.BlendNFuncSrcAlpha);
                state.BlendFuncDstAlpha  =   (GalBlendFactor)ReadRegister(NvGpuEngine3DReg.BlendNFuncDstAlpha);
            }
        }

        private void SetColorMask(GalPipelineState state)
        {
            int colorMask = ReadRegister(NvGpuEngine3DReg.ColorMask);

            state.ColorMask.Red   = ((colorMask >> 0)  & 0xf) != 0;
            state.ColorMask.Green = ((colorMask >> 4)  & 0xf) != 0;
            state.ColorMask.Blue  = ((colorMask >> 8)  & 0xf) != 0;
            state.ColorMask.Alpha = ((colorMask >> 12) & 0xf) != 0;

            for (int index = 0; index < GalPipelineState.RenderTargetsCount; index++)
            {
                colorMask = ReadRegister(NvGpuEngine3DReg.ColorMaskN + index);

                state.ColorMasks[index].Red   = ((colorMask >> 0)  & 0xf) != 0;
                state.ColorMasks[index].Green = ((colorMask >> 4)  & 0xf) != 0;
                state.ColorMasks[index].Blue  = ((colorMask >> 8)  & 0xf) != 0;
                state.ColorMasks[index].Alpha = ((colorMask >> 12) & 0xf) != 0;
            }
        }

        private void SetPrimitiveRestart(GalPipelineState state)
        {
            state.PrimitiveRestartEnabled = ReadRegisterBool(NvGpuEngine3DReg.PrimRestartEnable);

            if (state.PrimitiveRestartEnabled)
            {
                state.PrimitiveRestartIndex = (uint)ReadRegister(NvGpuEngine3DReg.PrimRestartIndex);
            }
        }

        private void SetRenderTargets()
        {
            //Commercial games do not seem to
            //bool SeparateFragData = ReadRegisterBool(NvGpuEngine3dReg.RTSeparateFragData);

            uint control = (uint)(ReadRegister(NvGpuEngine3DReg.RtControl));

            uint count = control & 0xf;

            if (count > 0)
            {
                int[] map = new int[count];

                for (int index = 0; index < count; index++)
                {
                    int shift = 4 + index * 3;

                    map[index] = (int)((control >> shift) & 7);
                }

                _gpu.Renderer.RenderTarget.SetMap(map);
            }
            else
            {
                _gpu.Renderer.RenderTarget.SetMap(null);
            }
        }

        private void UploadTextures(NvGpuVmm vmm, GalPipelineState state, long[] keys)
        {
            long baseShPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.ShaderAddress);

            int textureCbIndex = ReadRegister(NvGpuEngine3DReg.TextureCbIndex);

            int texIndex = 0;

            for (int index = 0; index < keys.Length; index++)
            {
                foreach (ShaderDeclInfo declInfo in _gpu.Renderer.Shader.GetTextureUsage(keys[index]))
                {
                    long position;

                    if (declInfo.IsCb)
                    {
                        position = _constBuffers[index][declInfo.Cbuf].Position;
                    }
                    else
                    {
                        position = _constBuffers[index][textureCbIndex].Position;
                    }

                    int textureHandle = vmm.ReadInt32(position + declInfo.Index * 4);

                    UploadTexture(vmm, texIndex, textureHandle);

                    texIndex++;
                }
            }
        }

        private void UploadTexture(NvGpuVmm vmm, int texIndex, int textureHandle)
        {
            if (textureHandle == 0)
            {
                //TODO: Is this correct?
                //Some games like puyo puyo will have 0 handles.
                //It may be just normal behaviour or a bug caused by sync issues.
                //The game does initialize the value properly after through.
                return;
            }

            int ticIndex = (textureHandle >>  0) & 0xfffff;
            int tscIndex = (textureHandle >> 20) & 0xfff;

            long ticPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.TexHeaderPoolOffset);
            long tscPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.TexSamplerPoolOffset);

            ticPosition += ticIndex * 0x20;
            tscPosition += tscIndex * 0x20;

            GalImage image = TextureFactory.MakeTexture(vmm, ticPosition);

            GalTextureSampler sampler = TextureFactory.MakeSampler(_gpu, vmm, tscPosition);

            long key = vmm.ReadInt64(ticPosition + 4) & 0xffffffffffff;

            if (image.Layout == GalMemoryLayout.BlockLinear)
            {
                key &= ~0x1ffL;
            }
            else if (image.Layout == GalMemoryLayout.Pitch)
            {
                key &= ~0x1fL;
            }

            key = vmm.GetPhysicalAddress(key);

            if (key == -1)
            {
                //FIXME: Shouldn't ignore invalid addresses.
                return;
            }

            _gpu.ResourceManager.SendTexture(vmm, key, image, texIndex);

            _gpu.Renderer.Texture.SetSampler(sampler);
        }

        private void UploadConstBuffers(NvGpuVmm vmm, GalPipelineState state, long[] keys)
        {
            for (int stage = 0; stage < keys.Length; stage++)
            {
                foreach (ShaderDeclInfo declInfo in _gpu.Renderer.Shader.GetConstBufferUsage(keys[stage]))
                {
                    ConstBuffer cb = _constBuffers[stage][declInfo.Cbuf];

                    if (!cb.Enabled)
                    {
                        continue;
                    }

                    long key = vmm.GetPhysicalAddress(cb.Position);

                    if (QueryKeyUpload(vmm, key, cb.Size, NvGpuBufferType.ConstBuffer))
                    {
                        IntPtr source = vmm.GetHostAddress(cb.Position, cb.Size);

                        _gpu.Renderer.Buffer.SetData(key, cb.Size, source);
                    }

                    state.ConstBufferKeys[stage][declInfo.Cbuf] = key;
                }
            }
        }

        private void UploadVertexArrays(NvGpuVmm vmm, GalPipelineState state)
        {
            long ibPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.IndexArrayAddress);

            long iboKey = vmm.GetPhysicalAddress(ibPosition);

            int indexEntryFmt = ReadRegister(NvGpuEngine3DReg.IndexArrayFormat);
            int indexCount    = ReadRegister(NvGpuEngine3DReg.IndexBatchCount);
            int primCtrl      = ReadRegister(NvGpuEngine3DReg.VertexBeginGl);

            GalPrimitiveType primType = (GalPrimitiveType)(primCtrl & 0xffff);

            GalIndexFormat indexFormat = (GalIndexFormat)indexEntryFmt;

            int indexEntrySize = 1 << indexEntryFmt;

            if (indexEntrySize > 4)
            {
                throw new InvalidOperationException();
            }

            if (indexCount != 0)
            {
                int ibSize = indexCount * indexEntrySize;

                bool iboCached = _gpu.Renderer.Rasterizer.IsIboCached(iboKey, (uint)ibSize);

                bool usesLegacyQuads =
                    primType == GalPrimitiveType.Quads ||
                    primType == GalPrimitiveType.QuadStrip;

                if (!iboCached || QueryKeyUpload(vmm, iboKey, (uint)ibSize, NvGpuBufferType.Index))
                {
                    if (!usesLegacyQuads)
                    {
                        IntPtr dataAddress = vmm.GetHostAddress(ibPosition, ibSize);

                        _gpu.Renderer.Rasterizer.CreateIbo(iboKey, ibSize, dataAddress);
                    }
                    else
                    {
                        byte[] buffer = vmm.ReadBytes(ibPosition, ibSize);

                        if (primType == GalPrimitiveType.Quads)
                        {
                            buffer = QuadHelper.ConvertIbQuadsToTris(buffer, indexEntrySize, indexCount);
                        }
                        else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                        {
                            buffer = QuadHelper.ConvertIbQuadStripToTris(buffer, indexEntrySize, indexCount);
                        }

                        _gpu.Renderer.Rasterizer.CreateIbo(iboKey, ibSize, buffer);
                    }
                }

                if (!usesLegacyQuads)
                {
                    _gpu.Renderer.Rasterizer.SetIndexArray(ibSize, indexFormat);
                }
                else
                {
                    if (primType == GalPrimitiveType.Quads)
                    {
                        _gpu.Renderer.Rasterizer.SetIndexArray(QuadHelper.ConvertIbSizeQuadsToTris(ibSize), indexFormat);
                    }
                    else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                    {
                        _gpu.Renderer.Rasterizer.SetIndexArray(QuadHelper.ConvertIbSizeQuadStripToTris(ibSize), indexFormat);
                    }
                }
            }

            List<GalVertexAttrib>[] attribs = new List<GalVertexAttrib>[32];

            for (int attr = 0; attr < 16; attr++)
            {
                int packed = ReadRegister(NvGpuEngine3DReg.VertexAttribNFormat + attr);

                int arrayIndex = packed & 0x1f;

                if (attribs[arrayIndex] == null)
                {
                    attribs[arrayIndex] = new List<GalVertexAttrib>();
                }

                long vertexPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.VertexArrayNAddress + arrayIndex * 4);

                int offset = (packed >> 7) & 0x3fff;

                //Note: 16 is the maximum size of an attribute,
                //having a component size of 32-bits with 4 elements (a vec4).
                IntPtr pointer = vmm.GetHostAddress(vertexPosition + offset, 16);

                attribs[arrayIndex].Add(new GalVertexAttrib(
                                           attr,
                                         ((packed >>  6) & 0x1) != 0,
                                           offset,
                                           pointer,
                    (GalVertexAttribSize)((packed >> 21) & 0x3f),
                    (GalVertexAttribType)((packed >> 27) & 0x7),
                                         ((packed >> 31) & 0x1) != 0));
            }

            state.VertexBindings = new GalVertexBinding[32];

            for (int index = 0; index < 32; index++)
            {
                if (attribs[index] == null)
                {
                    continue;
                }

                int control = ReadRegister(NvGpuEngine3DReg.VertexArrayNControl + index * 4);

                bool enable = (control & 0x1000) != 0;

                if (!enable)
                {
                    continue;
                }

                long vertexPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.VertexArrayNAddress + index * 4);
                long vertexEndPos   = MakeInt64From2XInt32(NvGpuEngine3DReg.VertexArrayNEndAddr + index * 2);

                int vertexDivisor = ReadRegister(NvGpuEngine3DReg.VertexArrayNDivisor + index * 4);

                bool instanced = ReadRegisterBool(NvGpuEngine3DReg.VertexArrayNInstance + index);

                int stride = control & 0xfff;

                if (instanced && vertexDivisor != 0)
                {
                    vertexPosition += stride * (_currentInstance / vertexDivisor);
                }

                if (vertexPosition > vertexEndPos)
                {
                    //Instance is invalid, ignore the draw call
                    continue;
                }

                long vboKey = vmm.GetPhysicalAddress(vertexPosition);

                long vbSize = (vertexEndPos - vertexPosition) + 1;

                bool vboCached = _gpu.Renderer.Rasterizer.IsVboCached(vboKey, vbSize);

                if (!vboCached || QueryKeyUpload(vmm, vboKey, vbSize, NvGpuBufferType.Vertex))
                {
                    IntPtr dataAddress = vmm.GetHostAddress(vertexPosition, vbSize);

                    _gpu.Renderer.Rasterizer.CreateVbo(vboKey, (int)vbSize, dataAddress);
                }

                state.VertexBindings[index].Enabled   = true;
                state.VertexBindings[index].Stride    = stride;
                state.VertexBindings[index].VboKey    = vboKey;
                state.VertexBindings[index].Instanced = instanced;
                state.VertexBindings[index].Divisor   = vertexDivisor;
                state.VertexBindings[index].Attribs   = attribs[index].ToArray();
            }
        }

        private void DispatchRender(NvGpuVmm vmm, GalPipelineState state)
        {
            int indexCount = ReadRegister(NvGpuEngine3DReg.IndexBatchCount);
            int primCtrl   = ReadRegister(NvGpuEngine3DReg.VertexBeginGl);

            GalPrimitiveType primType = (GalPrimitiveType)(primCtrl & 0xffff);

            bool instanceNext = ((primCtrl >> 26) & 1) != 0;
            bool instanceCont = ((primCtrl >> 27) & 1) != 0;

            if (instanceNext && instanceCont)
            {
                throw new InvalidOperationException("GPU tried to increase and reset instance count at the same time");
            }

            if (instanceNext)
            {
                _currentInstance++;
            }
            else if (!instanceCont)
            {
                _currentInstance = 0;
            }

            state.Instance = _currentInstance;

            _gpu.Renderer.Pipeline.Bind(state);

            _gpu.Renderer.RenderTarget.Bind();

            if (indexCount != 0)
            {
                int indexEntryFmt = ReadRegister(NvGpuEngine3DReg.IndexArrayFormat);
                int indexFirst    = ReadRegister(NvGpuEngine3DReg.IndexBatchFirst);
                int vertexBase    = ReadRegister(NvGpuEngine3DReg.VertexArrayElemBase);

                long indexPosition = MakeInt64From2XInt32(NvGpuEngine3DReg.IndexArrayAddress);

                long iboKey = vmm.GetPhysicalAddress(indexPosition);

                //Quad primitive types were deprecated on OpenGL 3.x,
                //they are converted to a triangles index buffer on IB creation,
                //so we should use the triangles type here too.
                if (primType == GalPrimitiveType.Quads ||
                    primType == GalPrimitiveType.QuadStrip)
                {
                    primType = GalPrimitiveType.Triangles;

                    //Note: We assume that index first points to the first
                    //vertex of a quad, if it points to the middle of a
                    //quad (First % 4 != 0 for Quads) then it will not work properly.
                    if (primType == GalPrimitiveType.Quads)
                    {
                        indexFirst = QuadHelper.ConvertIbSizeQuadsToTris(indexFirst);
                    }
                    else /* if (PrimType == GalPrimitiveType.QuadStrip) */
                    {
                        indexFirst = QuadHelper.ConvertIbSizeQuadStripToTris(indexFirst);
                    }
                }

                _gpu.Renderer.Rasterizer.DrawElements(iboKey, indexFirst, vertexBase, primType);
            }
            else
            {
                int vertexFirst = ReadRegister(NvGpuEngine3DReg.VertexArrayFirst);
                int vertexCount = ReadRegister(NvGpuEngine3DReg.VertexArrayCount);

                _gpu.Renderer.Rasterizer.DrawArrays(vertexFirst, vertexCount, primType);
            }

            //Is the GPU really clearing those registers after draw?
            WriteRegister(NvGpuEngine3DReg.IndexBatchFirst, 0);
            WriteRegister(NvGpuEngine3DReg.IndexBatchCount, 0);
        }

        private enum QueryMode
        {
            WriteSeq,
            Sync,
            WriteCounterAndTimestamp
        }

        private void QueryControl(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            WriteRegister(pbEntry);

            long position = MakeInt64From2XInt32(NvGpuEngine3DReg.QueryAddress);

            int seq  = Registers[(int)NvGpuEngine3DReg.QuerySequence];
            int ctrl = Registers[(int)NvGpuEngine3DReg.QueryControl];

            QueryMode mode = (QueryMode)(ctrl & 3);

            switch (mode)
            {
                case QueryMode.WriteSeq: vmm.WriteInt32(position, seq); break;

                case QueryMode.WriteCounterAndTimestamp:
                {
                    //TODO: Implement counters.
                    long counter = 1;

                    long timestamp = (uint)Environment.TickCount;

                    timestamp = (long)(timestamp * 615384.615385);

                    vmm.WriteInt64(position + 0, counter);
                    vmm.WriteInt64(position + 8, timestamp);

                    break;
                }
            }
        }

        private void CbData(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            long position = MakeInt64From2XInt32(NvGpuEngine3DReg.ConstBufferAddress);

            int offset = ReadRegister(NvGpuEngine3DReg.ConstBufferOffset);

            foreach (int arg in pbEntry.Arguments)
            {
                vmm.WriteInt32(position + offset, arg);

                offset += 4;
            }

            WriteRegister(NvGpuEngine3DReg.ConstBufferOffset, offset);

            _uploadedKeys[(int)NvGpuBufferType.ConstBuffer].Clear();
        }

        private void CbBind(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            int stage = (pbEntry.Method - 0x904) >> 3;

            int index = pbEntry.Arguments[0];

            bool enabled = (index & 1) != 0;

            index = (index >> 4) & 0x1f;

            long position = MakeInt64From2XInt32(NvGpuEngine3DReg.ConstBufferAddress);

            long cbKey = vmm.GetPhysicalAddress(position);

            int size = ReadRegister(NvGpuEngine3DReg.ConstBufferSize);

            if (!_gpu.Renderer.Buffer.IsCached(cbKey, size))
            {
                _gpu.Renderer.Buffer.Create(cbKey, size);
            }

            ConstBuffer cb = _constBuffers[stage][index];

            if (cb.Position != position || cb.Enabled != enabled || cb.Size != size)
            {
                _constBuffers[stage][index].Position = position;
                _constBuffers[stage][index].Enabled = enabled;
                _constBuffers[stage][index].Size = size;
            }
        }

        private float GetFlipSign(NvGpuEngine3DReg reg)
        {
            return MathF.Sign(ReadRegisterFloat(reg));
        }

        private long MakeInt64From2XInt32(NvGpuEngine3DReg reg)
        {
            return
                (long)Registers[(int)reg + 0] << 32 |
                (uint)Registers[(int)reg + 1];
        }

        private void WriteRegister(NvGpuPbEntry pbEntry)
        {
            int argsCount = pbEntry.Arguments.Count;

            if (argsCount > 0)
            {
                Registers[pbEntry.Method] = pbEntry.Arguments[argsCount - 1];
            }
        }

        private int ReadRegister(NvGpuEngine3DReg reg)
        {
            return Registers[(int)reg];
        }

        private float ReadRegisterFloat(NvGpuEngine3DReg reg)
        {
            return BitConverter.Int32BitsToSingle(ReadRegister(reg));
        }

        private bool ReadRegisterBool(NvGpuEngine3DReg reg)
        {
            return (ReadRegister(reg) & 1) != 0;
        }

        private void WriteRegister(NvGpuEngine3DReg reg, int value)
        {
            Registers[(int)reg] = value;
        }

        private bool QueryKeyUpload(NvGpuVmm vmm, long key, long size, NvGpuBufferType type)
        {
            List<long> uploaded = _uploadedKeys[(int)type];

            if (uploaded.Contains(key))
            {
                return false;
            }

            uploaded.Add(key);

            return vmm.IsRegionModified(key, size, type);
        }
    }
}
