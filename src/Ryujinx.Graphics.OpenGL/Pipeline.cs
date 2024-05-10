using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using Ryujinx.Graphics.OpenGL.Queries;
using Ryujinx.Graphics.Shader;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using System;
using Sampler = Ryujinx.Graphics.OpenGL.Image.Sampler;

namespace Ryujinx.Graphics.OpenGL
{
    class Pipeline : IPipeline, IDisposable
    {
        private const int SavedImages = 2;

        private readonly DrawTextureEmulation _drawTexture;

        internal ulong DrawCount { get; private set; }

        private Program _program;

        private bool _rasterizerDiscard;

        private VertexArray _vertexArray;
        private Framebuffer _framebuffer;

        private IntPtr _indexBaseOffset;

        private DrawElementsType _elementsType;

        private PrimitiveType _primitiveType;

        private int _stencilFrontMask;
        private bool _depthMask;
        private bool _depthTestEnable;
        private bool _stencilTestEnable;
        private bool _cullEnable;

        private float[] _viewportArray = [];
        private double[] _depthRangeArray = [];

        private uint _boundDrawFramebuffer;
        private uint _boundReadFramebuffer;

        private CounterQueueEvent _activeConditionalRender;

        private readonly Vector4<int>[] _fpIsBgra = new Vector4<int>[SupportBuffer.FragmentIsBgraCount];

        private readonly (TextureBase, Format)[] _images;
        private TextureBase _unit0Texture;
        private Sampler _unit0Sampler;

        private FrontFaceDirection _frontFace;
        private ClipControlOrigin _clipOrigin;
        private ClipControlDepth _clipDepthMode;

        private uint _fragmentOutputMap;
        private uint _componentMasks;
        private uint _currentComponentMasks;
        private bool _advancedBlendEnable;

        private uint _scissorEnables;

        private bool _tfEnabled;
        private PrimitiveType _tfTopology;

        private readonly BufferHandle[] _tfbs;
        private readonly BufferRange[] _tfbTargets;

        private ColorF _blendConstant;

        private readonly OpenGLRenderer _gd;

        internal Pipeline(OpenGLRenderer gd)
        {
            _gd = gd;

            _drawTexture = new DrawTextureEmulation();
            _rasterizerDiscard = false;
            _clipOrigin = ClipControlOrigin.LowerLeft;
            _clipDepthMode = ClipControlDepth.NegativeOneToOne;

            _fragmentOutputMap = uint.MaxValue;
            _componentMasks = uint.MaxValue;

            _images = new (TextureBase, Format)[SavedImages];

            _tfbs = new BufferHandle[Constants.MaxTransformFeedbackBuffers];
            _tfbTargets = new BufferRange[Constants.MaxTransformFeedbackBuffers];
        }

        public void Barrier()
        {
            _gd.Api.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
        }

        public void BeginTransformFeedback(PrimitiveTopology topology)
        {
            _gd.Api.BeginTransformFeedback(_tfTopology = topology.ConvertToTfType());
            _tfEnabled = true;
        }

        public void ClearBuffer(BufferHandle destination, int offset, int size, uint value)
        {
            Buffer.Clear(_gd.Api, destination, offset, (uint)size, value);
        }

        public void ClearRenderTargetColor(int index, int layer, int layerCount, uint componentMask, ColorF color)
        {
            EnsureFramebuffer();

            _gd.Api.ColorMask(
                (uint)index,
                (componentMask & 1) != 0,
                (componentMask & 2) != 0,
                (componentMask & 4) != 0,
                (componentMask & 8) != 0);

            float[] colors = [color.Red, color.Green, color.Blue, color.Alpha];

            if (layer != 0 || layerCount != _framebuffer.GetColorLayerCount(index))
            {
                for (int l = layer; l < layer + layerCount; l++)
                {
                    _framebuffer.AttachColorLayerForClear(index, l);

                    _gd.Api.ClearBuffer(BufferKind.Color, index, colors);
                }

                _framebuffer.DetachColorLayerForClear(index);
            }
            else
            {
                _gd.Api.ClearBuffer(BufferKind.Color, index, colors);
            }

            RestoreComponentMask(index);
        }

        public void ClearRenderTargetDepthStencil(int layer, int layerCount, float depthValue, bool depthMask, int stencilValue, int stencilMask)
        {
            EnsureFramebuffer();

            bool stencilMaskChanged =
                stencilMask != 0 &&
                stencilMask != _stencilFrontMask;

            bool depthMaskChanged = depthMask && depthMask != _depthMask;

            if (stencilMaskChanged)
            {
                _gd.Api.StencilMaskSeparate(TriangleFace.Front, (uint)stencilMask);
            }

            if (depthMaskChanged)
            {
                _gd.Api.DepthMask(depthMask);
            }

            if (layer != 0 || layerCount != _framebuffer.GetDepthStencilLayerCount())
            {
                for (int l = layer; l < layer + layerCount; l++)
                {
                    _framebuffer.AttachDepthStencilLayerForClear(l);

                    ClearDepthStencil(depthValue, depthMask, stencilValue, stencilMask);
                }

                _framebuffer.DetachDepthStencilLayerForClear();
            }
            else
            {
                ClearDepthStencil(depthValue, depthMask, stencilValue, stencilMask);
            }

            if (stencilMaskChanged)
            {
                _gd.Api.StencilMaskSeparate(TriangleFace.Front, (uint)_stencilFrontMask);
            }

            if (depthMaskChanged)
            {
                _gd.Api.DepthMask(_depthMask);
            }
        }

        private void ClearDepthStencil(float depthValue, bool depthMask, int stencilValue, int stencilMask)
        {
            if (depthMask && stencilMask != 0)
            {
                _gd.Api.ClearBuffer(GLEnum.DepthStencil, 0, depthValue, stencilValue);
            }
            else if (depthMask)
            {
                _gd.Api.ClearBuffer(BufferKind.Depth, 0, ref depthValue);
            }
            else if (stencilMask != 0)
            {
                _gd.Api.ClearBuffer(BufferKind.Stencil, 0, ref stencilValue);
            }
        }

        public void CommandBufferBarrier()
        {
            _gd.Api.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit);
        }

        public void CopyBuffer(BufferHandle source, BufferHandle destination, int srcOffset, int dstOffset, int size)
        {
            Buffer.Copy(_gd.Api, source, destination, srcOffset, dstOffset, size);
        }

        public void DispatchCompute(int groupsX, int groupsY, int groupsZ)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Dispatch error, shader not linked.");
                return;
            }

            PrepareForDispatch();

            _gd.Api.DispatchCompute((uint)groupsX, (uint)groupsY, (uint)groupsZ);
        }

        public void Draw(int vertexCount, int instanceCount, int firstVertex, int firstInstance)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Draw error, shader not linked.");
                return;
            }

            PreDraw(vertexCount);

            if (_primitiveType == PrimitiveType.Quads && !_gd.Capabilities.SupportsQuads)
            {
                DrawQuadsImpl(vertexCount, instanceCount, firstVertex, firstInstance);
            }
            else if (_primitiveType == PrimitiveType.QuadStrip && !_gd.Capabilities.SupportsQuads)
            {
                DrawQuadStripImpl(vertexCount, instanceCount, firstVertex, firstInstance);
            }
            else
            {
                DrawImpl(vertexCount, instanceCount, firstVertex, firstInstance);
            }

            PostDraw();
        }

        private void DrawQuadsImpl(
            int vertexCount,
            int instanceCount,
            int firstVertex,
            int firstInstance)
        {
            // TODO: Instanced rendering.
            uint quadsCount = (uint)(vertexCount / 4);

            int[] firsts = new int[quadsCount];
            uint[] counts = new uint[quadsCount];

            for (int quadIndex = 0; quadIndex < quadsCount; quadIndex++)
            {
                firsts[quadIndex] = firstVertex + quadIndex * 4;
                counts[quadIndex] = 4;
            }

            _gd.Api.MultiDrawArrays(
                PrimitiveType.TriangleFan,
                firsts,
                counts,
                quadsCount);
        }

        private void DrawQuadStripImpl(
            int vertexCount,
            int instanceCount,
            int firstVertex,
            int firstInstance)
        {
            uint quadsCount = (uint)((vertexCount - 2) / 2);

            if (firstInstance != 0 || instanceCount != 1)
            {
                for (int quadIndex = 0; quadIndex < quadsCount; quadIndex++)
                {
                    _gd.Api.DrawArraysInstancedBaseInstance(PrimitiveType.TriangleFan, firstVertex + quadIndex * 2, 4, (uint)instanceCount, (uint)firstInstance);
                }
            }
            else
            {
                int[] firsts = new int[quadsCount];
                uint[] counts = new uint[quadsCount];

                firsts[0] = firstVertex;
                counts[0] = 4;

                for (int quadIndex = 1; quadIndex < quadsCount; quadIndex++)
                {
                    firsts[quadIndex] = firstVertex + quadIndex * 2;
                    counts[quadIndex] = 4;
                }

                _gd.Api.MultiDrawArrays(
                    PrimitiveType.TriangleFan,
                    firsts,
                    counts,
                    quadsCount);
            }
        }

        private void DrawImpl(
            int vertexCount,
            int instanceCount,
            int firstVertex,
            int firstInstance)
        {
            if (firstInstance == 0 && instanceCount == 1)
            {
                _gd.Api.DrawArrays(_primitiveType, firstVertex, (uint)vertexCount);
            }
            else if (firstInstance == 0)
            {
                _gd.Api.DrawArraysInstanced(_primitiveType, firstVertex, (uint)vertexCount, (uint)instanceCount);
            }
            else
            {
                _gd.Api.DrawArraysInstancedBaseInstance(
                    _primitiveType,
                    firstVertex,
                    (uint)vertexCount,
                    (uint)instanceCount,
                    (uint)firstInstance);
            }
        }

        public void DrawIndexed(
            int indexCount,
            int instanceCount,
            int firstIndex,
            int firstVertex,
            int firstInstance)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Draw error, shader not linked.");
                return;
            }

            PreDrawVbUnbounded();

            int indexElemSize = 1;

            switch (_elementsType)
            {
                case DrawElementsType.UnsignedShort:
                    indexElemSize = 2;
                    break;
                case DrawElementsType.UnsignedInt:
                    indexElemSize = 4;
                    break;
            }

            IntPtr indexBaseOffset = _indexBaseOffset + firstIndex * indexElemSize;

            if (_primitiveType == PrimitiveType.Quads && !_gd.Capabilities.SupportsQuads)
            {
                DrawQuadsIndexedImpl(
                    indexCount,
                    instanceCount,
                    indexBaseOffset,
                    indexElemSize,
                    firstVertex,
                    firstInstance);
            }
            else if (_primitiveType == PrimitiveType.QuadStrip && !_gd.Capabilities.SupportsQuads)
            {
                DrawQuadStripIndexedImpl(
                    indexCount,
                    instanceCount,
                    indexBaseOffset,
                    indexElemSize,
                    firstVertex,
                    firstInstance);
            }
            else
            {
                DrawIndexedImpl(
                    indexCount,
                    instanceCount,
                    indexBaseOffset,
                    firstVertex,
                    firstInstance);
            }

            PostDraw();
        }

        private unsafe void DrawQuadsIndexedImpl(
            int indexCount,
            int instanceCount,
            IntPtr indexBaseOffset,
            int indexElemSize,
            int firstVertex,
            int firstInstance)
        {
            int quadsCount = indexCount / 4;

            if (firstInstance != 0 || instanceCount != 1)
            {
                if (firstVertex != 0 && firstInstance != 0)
                {
                    for (int quadIndex = 0; quadIndex < quadsCount; quadIndex++)
                    {
                        _gd.Api.DrawElementsInstancedBaseVertexBaseInstance(
                            PrimitiveType.TriangleFan,
                            4,
                            _elementsType,
                            indexBaseOffset + quadIndex * 4 * indexElemSize,
                            (uint)instanceCount,
                            firstVertex,
                            (uint)firstInstance);
                    }
                }
                else if (firstInstance != 0)
                {
                    for (int quadIndex = 0; quadIndex < quadsCount; quadIndex++)
                    {
                        _gd.Api.DrawElementsInstancedBaseInstance(
                            PrimitiveType.TriangleFan,
                            4,
                            _elementsType,
                            indexBaseOffset + quadIndex * 4 * indexElemSize,
                            (uint)instanceCount,
                            (uint)firstInstance);
                    }
                }
                else
                {
                    for (int quadIndex = 0; quadIndex < quadsCount; quadIndex++)
                    {
                        _gd.Api.DrawElementsInstanced(
                            PrimitiveType.TriangleFan,
                            4,
                            _elementsType,
                            indexBaseOffset + quadIndex * 4 * indexElemSize,
                            (uint)instanceCount);
                    }
                }
            }
            else
            {
                void*[] indices = new void*[quadsCount];

                uint[] counts = new uint[quadsCount];

                int[] baseVertices = new int[quadsCount];

                for (int quadIndex = 0; quadIndex < quadsCount; quadIndex++)
                {
                    indices[quadIndex] = (void*)(indexBaseOffset + quadIndex * 4 * indexElemSize);

                    counts[quadIndex] = 4;

                    baseVertices[quadIndex] = firstVertex;
                }

                fixed (uint* countsPtr = counts)
                fixed (void* indicesPtr = indices)
                fixed (int* baseVerticesPtr = baseVertices)
                {
                    _gd.Api.MultiDrawElementsBaseVertex(
                        PrimitiveType.TriangleFan,
                        countsPtr,
                        _elementsType,
                        indicesPtr,
                        (uint)quadsCount,
                        baseVerticesPtr);
                }
            }
        }

        private unsafe void DrawQuadStripIndexedImpl(
            int indexCount,
            int instanceCount,
            IntPtr indexBaseOffset,
            int indexElemSize,
            int firstVertex,
            int firstInstance)
        {
            // TODO: Instanced rendering.
            int quadsCount = (indexCount - 2) / 2;

            void*[] indices = new void*[quadsCount];

            uint[] counts = new uint[quadsCount];

            int[] baseVertices = new int[quadsCount];

            indices[0] = (void*)indexBaseOffset;

            counts[0] = 4;

            baseVertices[0] = firstVertex;

            for (int quadIndex = 1; quadIndex < quadsCount; quadIndex++)
            {
                indices[quadIndex] = (void*)(indexBaseOffset + quadIndex * 2 * indexElemSize);

                counts[quadIndex] = 4;

                baseVertices[quadIndex] = firstVertex;
            }

            fixed (uint* countsPtr = counts)
            fixed (void* indicesPtr = indices)
            fixed (int* baseVerticesPtr = baseVertices)
            {
                _gd.Api.MultiDrawElementsBaseVertex(
                    PrimitiveType.TriangleFan,
                    countsPtr,
                    _elementsType,
                    indicesPtr,
                    (uint)quadsCount,
                    baseVerticesPtr);
            }
        }

        private void DrawIndexedImpl(
            int indexCount,
            int instanceCount,
            IntPtr indexBaseOffset,
            int firstVertex,
            int firstInstance)
        {
            if (firstInstance == 0 && firstVertex == 0 && instanceCount == 1)
            {
                _gd.Api.DrawElements(_primitiveType, (uint)indexCount, _elementsType, indexBaseOffset);
            }
            else if (firstInstance == 0 && instanceCount == 1)
            {
                _gd.Api.DrawElementsBaseVertex(
                    _primitiveType,
                    (uint)indexCount,
                    _elementsType,
                    indexBaseOffset,
                    firstVertex);
            }
            else if (firstInstance == 0 && firstVertex == 0)
            {
                _gd.Api.DrawElementsInstanced(
                    _primitiveType,
                    (uint)indexCount,
                    _elementsType,
                    indexBaseOffset,
                    (uint)instanceCount);
            }
            else if (firstInstance == 0)
            {
                _gd.Api.DrawElementsInstancedBaseVertex(
                    _primitiveType,
                    (uint)indexCount,
                    _elementsType,
                    indexBaseOffset,
                    (uint)instanceCount,
                    firstVertex);
            }
            else if (firstVertex == 0)
            {
                _gd.Api.DrawElementsInstancedBaseInstance(
                    _primitiveType,
                    (uint)indexCount,
                    _elementsType,
                    indexBaseOffset,
                    (uint)instanceCount,
                    (uint)firstInstance);
            }
            else
            {
                _gd.Api.DrawElementsInstancedBaseVertexBaseInstance(
                    _primitiveType,
                    (uint)indexCount,
                    _elementsType,
                    indexBaseOffset,
                    (uint)instanceCount,
                    firstVertex,
                    (uint)firstInstance);
            }
        }

        public void DrawIndexedIndirect(BufferRange indirectBuffer)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Draw error, shader not linked.");
                return;
            }

            PreDrawVbUnbounded();

            _vertexArray.SetRangeOfIndexBuffer();

            _gd.Api.BindBuffer(BufferTargetARB.DrawIndirectBuffer, indirectBuffer.Handle.ToUInt32());

            _gd.Api.DrawElementsIndirect(_primitiveType, _elementsType, (IntPtr)indirectBuffer.Offset);

            _vertexArray.RestoreIndexBuffer();

            PostDraw();
        }

        public void DrawIndexedIndirectCount(BufferRange indirectBuffer, BufferRange parameterBuffer, int maxDrawCount, int stride)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Draw error, shader not linked.");
                return;
            }

            PreDrawVbUnbounded();

            _vertexArray.SetRangeOfIndexBuffer();

            _gd.Api.BindBuffer(BufferTargetARB.DrawIndirectBuffer, indirectBuffer.Handle.ToUInt32());
            _gd.Api.BindBuffer(BufferTargetARB.ParameterBuffer, parameterBuffer.Handle.ToUInt32());

            _gd.Api.MultiDrawElementsIndirectCount(
                _primitiveType,
                _elementsType,
                (IntPtr)indirectBuffer.Offset,
                parameterBuffer.Offset,
                (uint)maxDrawCount,
                (uint)stride);

            _vertexArray.RestoreIndexBuffer();

            PostDraw();
        }

        public void DrawIndirect(BufferRange indirectBuffer)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Draw error, shader not linked.");
                return;
            }

            PreDrawVbUnbounded();

            _gd.Api.BindBuffer(BufferTargetARB.DrawIndirectBuffer, indirectBuffer.Handle.ToUInt32());

            _gd.Api.DrawArraysIndirect(_primitiveType, (IntPtr)indirectBuffer.Offset);

            PostDraw();
        }

        public void DrawIndirectCount(BufferRange indirectBuffer, BufferRange parameterBuffer, int maxDrawCount, int stride)
        {
            if (!_program.IsLinked)
            {
                Logger.Debug?.Print(LogClass.Gpu, "Draw error, shader not linked.");
                return;
            }

            PreDrawVbUnbounded();

            _gd.Api.BindBuffer(BufferTargetARB.DrawIndirectBuffer, indirectBuffer.Handle.ToUInt32());
            _gd.Api.BindBuffer(BufferTargetARB.ParameterBuffer, parameterBuffer.Handle.ToUInt32());

            _gd.Api.MultiDrawArraysIndirectCount(
                _primitiveType,
                (IntPtr)indirectBuffer.Offset,
                parameterBuffer.Offset,
                (uint)maxDrawCount,
                (uint)stride);

            PostDraw();
        }

        public void DrawTexture(ITexture texture, ISampler sampler, Extents2DF srcRegion, Extents2DF dstRegion)
        {
            if (texture is TextureView view && sampler is Sampler samp)
            {
                if (_gd.Capabilities.SupportsDrawTexture)
                {
                    _gd.Api.TryGetExtension(out NVDrawTexture drawTexture);

                    drawTexture.DrawTexture(
                        view.Handle,
                        samp.Handle,
                        dstRegion.X1,
                        dstRegion.Y1,
                        dstRegion.X2,
                        dstRegion.Y2,
                        0,
                        srcRegion.X1 / view.Width,
                        srcRegion.Y1 / view.Height,
                        srcRegion.X2 / view.Width,
                        srcRegion.Y2 / view.Height);
                }
                else
                {
                    static void Disable(GL api, EnableCap cap, bool enabled)
                    {
                        if (enabled)
                        {
                            api.Disable(cap);
                        }
                    }

                    static void Enable(GL api, EnableCap cap, bool enabled)
                    {
                        if (enabled)
                        {
                            api.Enable(cap);
                        }
                    }

                    Disable(_gd.Api, EnableCap.CullFace, _cullEnable);
                    Disable(_gd.Api, EnableCap.StencilTest, _stencilTestEnable);
                    Disable(_gd.Api, EnableCap.DepthTest, _depthTestEnable);

                    if (_depthMask)
                    {
                        _gd.Api.DepthMask(false);
                    }

                    if (_tfEnabled)
                    {
                        _gd.Api.EndTransformFeedback();
                    }

                    _gd.Api.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.NegativeOneToOne);

                    _drawTexture.Draw(
                        _gd.Api,
                        view,
                        samp,
                        dstRegion.X1,
                        dstRegion.Y1,
                        dstRegion.X2,
                        dstRegion.Y2,
                        srcRegion.X1 / view.Width,
                        srcRegion.Y1 / view.Height,
                        srcRegion.X2 / view.Width,
                        srcRegion.Y2 / view.Height);

                    _program?.Bind();
                    _unit0Sampler?.Bind(0);

                    RestoreViewport0();

                    Enable(_gd.Api, EnableCap.CullFace, _cullEnable);
                    Enable(_gd.Api, EnableCap.StencilTest, _stencilTestEnable);
                    Enable(_gd.Api, EnableCap.DepthTest, _depthTestEnable);

                    if (_depthMask)
                    {
                        _gd.Api.DepthMask(true);
                    }

                    if (_tfEnabled)
                    {
                        _gd.Api.BeginTransformFeedback(_tfTopology);
                    }

                    RestoreClipControl();
                }
            }
        }

        public void EndTransformFeedback()
        {
            _gd.Api.EndTransformFeedback();
            _tfEnabled = false;
        }

        public void SetAlphaTest(bool enable, float reference, CompareOp op)
        {
            if (!enable)
            {
                _gd.Api.Disable(EnableCap.AlphaTest);
                return;
            }

            _gd.Api.AlphaFunc((AlphaFunction)op.Convert(), reference);
            _gd.Api.Enable(EnableCap.AlphaTest);
        }

        public void SetBlendState(AdvancedBlendDescriptor blend)
        {
            if (_gd.Capabilities.SupportsBlendEquationAdvanced)
            {
                _gd.Api.BlendEquation((GLEnum)blend.Op.Convert());

                _gd.Api.TryGetExtension(out NVBlendEquationAdvanced nvBlendEquationAdvanced);
                nvBlendEquationAdvanced.BlendParameter(NV.BlendOverlapNV, (int)blend.Overlap.Convert());
                nvBlendEquationAdvanced.BlendParameter(NV.BlendPremultipliedSrcNV, blend.SrcPreMultiplied ? 1 : 0);
                _gd.Api.Enable(EnableCap.Blend);
                _advancedBlendEnable = true;
            }
        }

        public void SetBlendState(int index, BlendDescriptor blend)
        {
            if (_advancedBlendEnable)
            {
                _gd.Api.Disable(EnableCap.Blend);
                _advancedBlendEnable = false;
            }

            if (!blend.Enable)
            {
                _gd.Api.Disable(EnableCap.Blend, (uint)index);
                return;
            }

            _gd.Api.BlendEquationSeparate(
                (uint)index,
                blend.ColorOp.Convert(),
                blend.AlphaOp.Convert());

            _gd.Api.BlendFuncSeparate(
                (uint)index,
                (BlendingFactor)blend.ColorSrcFactor.Convert(),
                (BlendingFactor)blend.ColorDstFactor.Convert(),
                (BlendingFactor)blend.AlphaSrcFactor.Convert(),
                (BlendingFactor)blend.AlphaDstFactor.Convert());

            EnsureFramebuffer();

            _framebuffer.SetDualSourceBlend(
                blend.ColorSrcFactor.IsDualSource() ||
                blend.ColorDstFactor.IsDualSource() ||
                blend.AlphaSrcFactor.IsDualSource() ||
                blend.AlphaDstFactor.IsDualSource());

            if (_blendConstant != blend.BlendConstant)
            {
                _blendConstant = blend.BlendConstant;

                _gd.Api.BlendColor(
                    blend.BlendConstant.Red,
                    blend.BlendConstant.Green,
                    blend.BlendConstant.Blue,
                    blend.BlendConstant.Alpha);
            }

            _gd.Api.Enable(EnableCap.Blend, (uint)index);
        }

        public void SetDepthBias(PolygonModeMask enables, float factor, float units, float clamp)
        {
            if ((enables & PolygonModeMask.Point) != 0)
            {
                _gd.Api.Enable(EnableCap.PolygonOffsetPoint);
            }
            else
            {
                _gd.Api.Disable(EnableCap.PolygonOffsetPoint);
            }

            if ((enables & PolygonModeMask.Line) != 0)
            {
                _gd.Api.Enable(EnableCap.PolygonOffsetLine);
            }
            else
            {
                _gd.Api.Disable(EnableCap.PolygonOffsetLine);
            }

            if ((enables & PolygonModeMask.Fill) != 0)
            {
                _gd.Api.Enable(EnableCap.PolygonOffsetFill);
            }
            else
            {
                _gd.Api.Disable(EnableCap.PolygonOffsetFill);
            }

            if (enables == 0)
            {
                return;
            }

            if (_gd.Capabilities.SupportsPolygonOffsetClamp)
            {
                _gd.Api.PolygonOffsetClamp(factor, units, clamp);
            }
            else
            {
                _gd.Api.PolygonOffset(factor, units);
            }
        }

        public void SetDepthClamp(bool clamp)
        {
            if (!clamp)
            {
                _gd.Api.Disable(EnableCap.DepthClamp);
                return;
            }

            _gd.Api.Enable(EnableCap.DepthClamp);
        }

        public void SetDepthMode(DepthMode mode)
        {
            ClipControlDepth depthMode = (ClipControlDepth)mode.Convert();

            if (_clipDepthMode != depthMode)
            {
                _clipDepthMode = depthMode;

                _gd.Api.ClipControl(_clipOrigin, depthMode);
            }
        }

        public void SetDepthTest(DepthTestDescriptor depthTest)
        {
            if (depthTest.TestEnable)
            {
                _gd.Api.Enable(EnableCap.DepthTest);
                _gd.Api.DepthFunc((DepthFunction)depthTest.Func.Convert());
            }
            else
            {
                _gd.Api.Disable(EnableCap.DepthTest);
            }

            _gd.Api.DepthMask(depthTest.WriteEnable);
            _depthMask = depthTest.WriteEnable;
            _depthTestEnable = depthTest.TestEnable;
        }

        public void SetFaceCulling(bool enable, Face face)
        {
            _cullEnable = enable;

            if (!enable)
            {
                _gd.Api.Disable(EnableCap.CullFace);
                return;
            }

            _gd.Api.CullFace(face.Convert());

            _gd.Api.Enable(EnableCap.CullFace);
        }

        public void SetFrontFace(FrontFace frontFace)
        {
            SetFrontFace(_frontFace = frontFace.Convert());
        }

        public void SetImage(ShaderStage stage, int binding, ITexture texture, Format imageFormat)
        {
            if ((uint)binding < SavedImages)
            {
                _images[binding] = (texture as TextureBase, imageFormat);
            }

            if (texture == null)
            {
                _gd.Api.BindImageTexture((uint)binding, 0, 0, true, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
                return;
            }

            TextureBase texBase = (TextureBase)texture;

            InternalFormat format = (InternalFormat)FormatTable.GetImageFormat(imageFormat);

            if (format != 0)
            {
                _gd.Api.BindImageTexture((uint)binding, texBase.Handle, 0, true, 0, BufferAccessARB.ReadWrite, format);
            }
        }

        public void SetImageArray(ShaderStage stage, int binding, IImageArray array)
        {
            (array as ImageArray).Bind((uint)binding);
        }

        public void SetIndexBuffer(BufferRange buffer, IndexType type)
        {
            _elementsType = type.Convert();

            _indexBaseOffset = buffer.Offset;

            EnsureVertexArray();

            _vertexArray.SetIndexBuffer(buffer);
        }

        public void SetLogicOpState(bool enable, LogicalOp op)
        {
            if (enable)
            {
                _gd.Api.Enable(EnableCap.ColorLogicOp);

                _gd.Api.LogicOp((LogicOp)op.Convert());
            }
            else
            {
                _gd.Api.Disable(EnableCap.ColorLogicOp);
            }
        }

        public void SetMultisampleState(MultisampleDescriptor multisample)
        {
            if (multisample.AlphaToCoverageEnable)
            {
                _gd.Api.Enable(EnableCap.SampleAlphaToCoverage);

                if (multisample.AlphaToOneEnable)
                {
                    _gd.Api.Enable(EnableCap.SampleAlphaToOne);
                }
                else
                {
                    _gd.Api.Disable(EnableCap.SampleAlphaToOne);
                }

                if (_gd.Capabilities.SupportsAlphaToCoverageDitherControl)
                {
                    _gd.Api.TryGetExtension(out NVAlphaToCoverageDitherControl nvAlphaToCoverageDitherControl);

                    nvAlphaToCoverageDitherControl.AlphaToCoverageDitherControl(multisample.AlphaToCoverageDitherEnable
                        ? NV.AlphaToCoverageDitherEnableNV
                        : NV.AlphaToCoverageDitherDisableNV);
                }
            }
            else
            {
                _gd.Api.Disable(EnableCap.SampleAlphaToCoverage);
            }
        }

        public void SetLineParameters(float width, bool smooth)
        {
            if (smooth)
            {
                _gd.Api.Enable(EnableCap.LineSmooth);
            }
            else
            {
                _gd.Api.Disable(EnableCap.LineSmooth);
            }

            _gd.Api.LineWidth(width);
        }

        public unsafe void SetPatchParameters(int vertices, ReadOnlySpan<float> defaultOuterLevel, ReadOnlySpan<float> defaultInnerLevel)
        {
            _gd.Api.PatchParameter(PatchParameterName.Vertices, vertices);

            fixed (float* pOuterLevel = defaultOuterLevel)
            {
                _gd.Api.PatchParameter(PatchParameterName.DefaultOuterLevel, pOuterLevel);
            }

            fixed (float* pInnerLevel = defaultInnerLevel)
            {
                _gd.Api.PatchParameter(PatchParameterName.DefaultInnerLevel, pInnerLevel);
            }
        }

        public void SetPointParameters(float size, bool isProgramPointSize, bool enablePointSprite, Origin origin)
        {
            // GL_POINT_SPRITE was deprecated in core profile 3.2+ and causes GL_INVALID_ENUM when set.
            // As we don't know if the current context is core or compat, it's safer to keep this code.
            if (enablePointSprite)
            {
                _gd.Api.Enable(GLEnum.PointSprite);
            }
            else
            {
                _gd.Api.Disable(GLEnum.PointSprite);
            }

            if (isProgramPointSize)
            {
                _gd.Api.Enable(EnableCap.ProgramPointSize);
            }
            else
            {
                _gd.Api.Disable(EnableCap.ProgramPointSize);
            }

            _gd.Api.PointParameter(GLEnum.PointSpriteCoordOrigin, (int)(origin == Origin.LowerLeft
                ? GLEnum.LowerLeft
                : GLEnum.UpperLeft));

            // Games seem to set point size to 0 which generates a GL_INVALID_VALUE
            // From the spec, GL_INVALID_VALUE is generated if size is less than or equal to 0.
            _gd.Api.PointSize(Math.Max(float.Epsilon, size));
        }

        public void SetPolygonMode(GAL.PolygonMode frontMode, GAL.PolygonMode backMode)
        {
            if (frontMode == backMode)
            {
                _gd.Api.PolygonMode(TriangleFace.FrontAndBack, frontMode.Convert());
            }
            else
            {
                _gd.Api.PolygonMode(TriangleFace.Front, frontMode.Convert());
                _gd.Api.PolygonMode(TriangleFace.Back, backMode.Convert());
            }
        }

        public void SetPrimitiveRestart(bool enable, int index)
        {
            if (!enable)
            {
                _gd.Api.Disable(EnableCap.PrimitiveRestart);
                return;
            }

            _gd.Api.PrimitiveRestartIndex((uint)index);

            _gd.Api.Enable(EnableCap.PrimitiveRestart);
        }

        public void SetPrimitiveTopology(PrimitiveTopology topology)
        {
            _primitiveType = topology.Convert();
        }

        public void SetProgram(IProgram program)
        {
            Program prg = (Program)program;

            if (_tfEnabled)
            {
                _gd.Api.EndTransformFeedback();
                prg.Bind();
                _gd.Api.BeginTransformFeedback(_tfTopology);
            }
            else
            {
                prg.Bind();
            }

            if (_fragmentOutputMap != (uint)prg.FragmentOutputMap)
            {
                _fragmentOutputMap = (uint)prg.FragmentOutputMap;

                for (int index = 0; index < Constants.MaxRenderTargets; index++)
                {
                    RestoreComponentMask(index, force: false);
                }
            }

            _program = prg;
        }

        public void SetRasterizerDiscard(bool discard)
        {
            if (discard)
            {
                _gd.Api.Enable(EnableCap.RasterizerDiscard);
            }
            else
            {
                _gd.Api.Disable(EnableCap.RasterizerDiscard);
            }

            _rasterizerDiscard = discard;
        }

        public void SetRenderTargetColorMasks(ReadOnlySpan<uint> componentMasks)
        {
            _componentMasks = 0;

            for (int index = 0; index < componentMasks.Length; index++)
            {
                _componentMasks |= componentMasks[index] << (index * 4);

                RestoreComponentMask(index, force: false);
            }
        }

        public void SetRenderTargets(ITexture[] colors, ITexture depthStencil)
        {
            EnsureFramebuffer();

            for (int index = 0; index < colors.Length; index++)
            {
                TextureView color = (TextureView)colors[index];

                _framebuffer.AttachColor(index, color);

                if (color != null)
                {
                    int isBgra = color.Format.IsBgr() ? 1 : 0;

                    if (_fpIsBgra[index].X != isBgra)
                    {
                        _fpIsBgra[index].X = isBgra;

                        RestoreComponentMask(index);
                    }
                }
            }

            TextureView depthStencilView = (TextureView)depthStencil;

            _framebuffer.AttachDepthStencil(depthStencilView);
            _framebuffer.SetDrawBuffers(colors.Length);
        }

        public void SetScissors(ReadOnlySpan<Rectangle<int>> regions)
        {
            int count = Math.Min(regions.Length, Constants.MaxViewports);

            Span<int> v = stackalloc int[count * 4];

            for (int index = 0; index < count; index++)
            {
                int vIndex = index * 4;

                var region = regions[index];

                bool enabled = (region.X | region.Y) != 0 || region.Width != 0xffff || region.Height != 0xffff;
                uint mask = 1u << index;

                if (enabled)
                {
                    v[vIndex] = region.X;
                    v[vIndex + 1] = region.Y;
                    v[vIndex + 2] = region.Width;
                    v[vIndex + 3] = region.Height;

                    if ((_scissorEnables & mask) == 0)
                    {
                        _scissorEnables |= mask;
                        _gd.Api.Enable(EnableCap.ScissorTest, (uint)index);
                    }
                }
                else
                {
                    if ((_scissorEnables & mask) != 0)
                    {
                        _scissorEnables &= ~mask;
                        _gd.Api.Disable(EnableCap.ScissorTest, (uint)index);
                    }
                }
            }

            _gd.Api.ScissorArray(0, (uint)count, ref v[0]);
        }

        public void SetStencilTest(StencilTestDescriptor stencilTest)
        {
            _stencilTestEnable = stencilTest.TestEnable;

            if (!stencilTest.TestEnable)
            {
                _gd.Api.Disable(EnableCap.StencilTest);
                return;
            }

            _gd.Api.StencilOpSeparate(
                TriangleFace.Front,
                stencilTest.FrontSFail.Convert(),
                stencilTest.FrontDpFail.Convert(),
                stencilTest.FrontDpPass.Convert());

            _gd.Api.StencilFuncSeparate(
                TriangleFace.Front,
                (StencilFunction)stencilTest.FrontFunc.Convert(),
                stencilTest.FrontFuncRef,
                (uint)stencilTest.FrontFuncMask);

            _gd.Api.StencilMaskSeparate(TriangleFace.Front, (uint)stencilTest.FrontMask);

            _gd.Api.StencilOpSeparate(
                TriangleFace.Back,
                stencilTest.BackSFail.Convert(),
                stencilTest.BackDpFail.Convert(),
                stencilTest.BackDpPass.Convert());

            _gd.Api.StencilFuncSeparate(
                TriangleFace.Back,
                (StencilFunction)stencilTest.BackFunc.Convert(),
                stencilTest.BackFuncRef,
                (uint)stencilTest.BackFuncMask);

            _gd.Api.StencilMaskSeparate(TriangleFace.Back, (uint)stencilTest.BackMask);

            _gd.Api.Enable(EnableCap.StencilTest);

            _stencilFrontMask = stencilTest.FrontMask;
        }

        public void SetStorageBuffers(ReadOnlySpan<BufferAssignment> buffers)
        {
            SetBuffers(buffers, isStorage: true);
        }

        public void SetTextureAndSampler(ShaderStage stage, int binding, ITexture texture, ISampler sampler)
        {
            if (texture != null)
            {
                if (binding == 0)
                {
                    _unit0Texture = (TextureBase)texture;
                }
                else
                {
                    ((TextureBase)texture).Bind((uint)binding);
                }
            }
            else
            {
                TextureBase.ClearBinding(_gd.Api, (uint)binding);
            }

            Sampler glSampler = (Sampler)sampler;

            glSampler?.Bind((uint)binding);

            if (binding == 0)
            {
                _unit0Sampler = glSampler;
            }
        }

        public void SetTextureArray(ShaderStage stage, int binding, ITextureArray array)
        {
            (array as TextureArray).Bind((uint)binding);
        }

        public void SetTransformFeedbackBuffers(ReadOnlySpan<BufferRange> buffers)
        {
            if (_tfEnabled)
            {
                _gd.Api.EndTransformFeedback();
            }

            int count = Math.Min(buffers.Length, Constants.MaxTransformFeedbackBuffers);

            for (int i = 0; i < count; i++)
            {
                BufferRange buffer = buffers[i];
                _tfbTargets[i] = buffer;

                if (buffer.Handle == BufferHandle.Null)
                {
                    _gd.Api.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, (uint)i, 0);
                    continue;
                }

                if (_tfbs[i] == BufferHandle.Null)
                {
                    _tfbs[i] = Buffer.Create(_gd.Api);
                }

                Buffer.Resize(_gd.Api, _tfbs[i], buffer.Size);
                Buffer.Copy(_gd.Api, buffer.Handle, _tfbs[i], buffer.Offset, 0, buffer.Size);
                _gd.Api.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, (uint)i, _tfbs[i].ToUInt32());
            }

            if (_tfEnabled)
            {
                _gd.Api.BeginTransformFeedback(_tfTopology);
            }
        }

        public void SetUniformBuffers(ReadOnlySpan<BufferAssignment> buffers)
        {
            SetBuffers(buffers, isStorage: false);
        }

        public void SetUserClipDistance(int index, bool enableClip)
        {
            if (!enableClip)
            {
                _gd.Api.Disable(EnableCap.ClipDistance0 + index);
                return;
            }

            _gd.Api.Enable(EnableCap.ClipDistance0 + index);
        }

        public void SetVertexAttribs(ReadOnlySpan<VertexAttribDescriptor> vertexAttribs)
        {
            EnsureVertexArray();

            _vertexArray.SetVertexAttributes(vertexAttribs);
        }

        public void SetVertexBuffers(ReadOnlySpan<VertexBufferDescriptor> vertexBuffers)
        {
            EnsureVertexArray();

            _vertexArray.SetVertexBuffers(vertexBuffers);
        }

        public void SetViewports(ReadOnlySpan<Viewport> viewports)
        {
            Array.Resize(ref _viewportArray, viewports.Length * 4);
            Array.Resize(ref _depthRangeArray, viewports.Length * 2);

            float[] viewportArray = _viewportArray;
            double[] depthRangeArray = _depthRangeArray;

            for (uint index = 0; index < viewports.Length; index++)
            {
                uint viewportElemIndex = index * 4;

                Viewport viewport = viewports[(int)index];

                viewportArray[viewportElemIndex + 0] = viewport.Region.X;
                viewportArray[viewportElemIndex + 1] = viewport.Region.Y + (viewport.Region.Height < 0 ? viewport.Region.Height : 0);
                viewportArray[viewportElemIndex + 2] = viewport.Region.Width;
                viewportArray[viewportElemIndex + 3] = MathF.Abs(viewport.Region.Height);

                if (_gd.Capabilities.SupportsViewportSwizzle)
                {
                    _gd.Api.TryGetExtension(out NVViewportSwizzle nvViewportSwizzle);

                    nvViewportSwizzle.ViewportSwizzle(
                        index,
                        viewport.SwizzleX.Convert(),
                        viewport.SwizzleY.Convert(),
                        viewport.SwizzleZ.Convert(),
                        viewport.SwizzleW.Convert());
                }

                depthRangeArray[index * 2 + 0] = viewport.DepthNear;
                depthRangeArray[index * 2 + 1] = viewport.DepthFar;
            }

            bool flipY = viewports.Length != 0 && viewports[0].Region.Height < 0;

            SetOrigin(flipY ? ClipControlOrigin.UpperLeft : ClipControlOrigin.LowerLeft);

            _gd.Api.ViewportArray(0, (uint)viewports.Length, viewportArray);
            _gd.Api.DepthRangeArray(0, (uint)viewports.Length, depthRangeArray);
        }

        public void TextureBarrier()
        {
            _gd.Api.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);
        }

        public void TextureBarrierTiled()
        {
            _gd.Api.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);
        }

        private void SetBuffers(ReadOnlySpan<BufferAssignment> buffers, bool isStorage)
        {
            BufferTargetARB target = isStorage ? BufferTargetARB.ShaderStorageBuffer : BufferTargetARB.UniformBuffer;

            for (int index = 0; index < buffers.Length; index++)
            {
                BufferAssignment assignment = buffers[index];
                BufferRange buffer = assignment.Range;

                if (buffer.Handle == BufferHandle.Null)
                {
                    _gd.Api.BindBufferRange(target, (uint)assignment.Binding, 0, IntPtr.Zero, 0);
                    continue;
                }

                _gd.Api.BindBufferRange(target, (uint)assignment.Binding, buffer.Handle.ToUInt32(), buffer.Offset, (UIntPtr)buffer.Size);
            }
        }

        private void SetOrigin(ClipControlOrigin origin)
        {
            if (_clipOrigin != origin)
            {
                _clipOrigin = origin;

                _gd.Api.ClipControl(origin, _clipDepthMode);

                SetFrontFace(_frontFace);
            }
        }

        private void SetFrontFace(FrontFaceDirection frontFace)
        {
            // Changing clip origin will also change the front face to compensate
            // for the flipped viewport, we flip it again here to compensate as
            // this effect is undesirable for us.
            if (_clipOrigin == ClipControlOrigin.UpperLeft)
            {
                frontFace = frontFace == FrontFaceDirection.Ccw ? FrontFaceDirection.CW : FrontFaceDirection.Ccw;
            }

            _gd.Api.FrontFace(frontFace);
        }

        private void EnsureVertexArray()
        {
            if (_vertexArray == null)
            {
                _vertexArray = new VertexArray(_gd.Api);

                _vertexArray.Bind();
            }
        }

        private void EnsureFramebuffer()
        {
            if (_framebuffer == null)
            {
                _framebuffer = new Framebuffer(_gd.Api);

                uint boundHandle = _framebuffer.Bind();
                _boundDrawFramebuffer = _boundReadFramebuffer = boundHandle;

                _gd.Api.Enable(EnableCap.FramebufferSrgb);
            }
        }

        internal (uint drawHandle, uint readHandle) GetBoundFramebuffers()
        {
            if (BackgroundContextWorker.InBackground)
            {
                return (0, 0);
            }

            return (_boundDrawFramebuffer, _boundReadFramebuffer);
        }

        private void PrepareForDispatch()
        {
            _unit0Texture?.Bind(0);
        }

        private void PreDraw(int vertexCount)
        {
            _vertexArray.PreDraw(vertexCount);
            PreDraw();
        }

        private void PreDrawVbUnbounded()
        {
            _vertexArray.PreDrawVbUnbounded();
            PreDraw();
        }

        private void PreDraw()
        {
            DrawCount++;

            _unit0Texture?.Bind(0);
        }

        private void PostDraw()
        {
            if (_tfEnabled)
            {
                for (int i = 0; i < Constants.MaxTransformFeedbackBuffers; i++)
                {
                    if (_tfbTargets[i].Handle != BufferHandle.Null)
                    {
                        Buffer.Copy(_gd.Api, _tfbs[i], _tfbTargets[i].Handle, 0, _tfbTargets[i].Offset, _tfbTargets[i].Size);
                    }
                }
            }
        }

        public void RestoreComponentMask(int index, bool force = true)
        {
            // If the bound render target is bgra, swap the red and blue masks.
            uint redMask = _fpIsBgra[index].X == 0 ? 1u : 4u;
            uint blueMask = _fpIsBgra[index].X == 0 ? 4u : 1u;

            int shift = index * 4;
            uint componentMask = _componentMasks & _fragmentOutputMap;
            uint checkMask = 0xfu << shift;
            uint componentMaskAtIndex = componentMask & checkMask;

            if (!force && componentMaskAtIndex == (_currentComponentMasks & checkMask))
            {
                return;
            }

            componentMask >>= shift;
            componentMask &= 0xfu;

            _gd.Api.ColorMask(
                (uint)index,
                (componentMask & redMask) != 0,
                (componentMask & 2u) != 0,
                (componentMask & blueMask) != 0,
                (componentMask & 8u) != 0);

            _currentComponentMasks &= ~checkMask;
            _currentComponentMasks |= componentMaskAtIndex;
        }

        public void RestoreClipControl()
        {
            _gd.Api.ClipControl(_clipOrigin, _clipDepthMode);
        }

        public void RestoreScissor0Enable()
        {
            if ((_scissorEnables & 1u) != 0)
            {
                _gd.Api.Enable(EnableCap.ScissorTest, 0);
            }
        }

        public void RestoreRasterizerDiscard()
        {
            if (_rasterizerDiscard)
            {
                _gd.Api.Enable(EnableCap.RasterizerDiscard);
            }
        }

        public void RestoreViewport0()
        {
            if (_viewportArray.Length > 0)
            {
                _gd.Api.ViewportArray(0, 1, _viewportArray);
            }
        }

        public void RestoreProgram()
        {
            _program?.Bind();
        }

        public void RestoreImages1And2()
        {
            for (int i = 0; i < SavedImages; i++)
            {
                (TextureBase texBase, Format imageFormat) = _images[i];

                if (texBase != null)
                {
                    InternalFormat format = (InternalFormat)FormatTable.GetImageFormat(imageFormat);

                    if (format != 0)
                    {
                        _gd.Api.BindImageTexture((uint)i, texBase.Handle, 0, true, 0, BufferAccessARB.ReadWrite, format);
                        continue;
                    }
                }

                _gd.Api.BindImageTexture((uint)i, 0, 0, true, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
            }
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

                if (compare == 0 && evt.Type == QueryTarget.SamplesPassed && evt.ClearCounter)
                {
                    if (!value.ReserveForHostAccess())
                    {
                        // If the event has been flushed, then just use the values on the CPU.
                        // The query object may already be repurposed for another draw (eg. begin + end).
                        return false;
                    }

                    _gd.Api.BeginConditionalRender(evt.Query, isEqual ? ConditionalRenderMode.NoWaitInverted : ConditionalRenderMode.NoWait);
                    _activeConditionalRender = evt;

                    return true;
                }
            }

            // The GPU will flush the queries to CPU and evaluate the condition there instead.

            _gd.Api.Flush(); // The thread will be stalled manually flushing the counter, so flush GL commands now.
            return false;
        }

        public bool TryHostConditionalRendering(ICounterEvent value, ICounterEvent compare, bool isEqual)
        {
            _gd.Api.Flush(); // The GPU thread will be stalled manually flushing the counter, so flush GL commands now.
            return false; // We don't currently have a way to compare two counters for conditional rendering.
        }

        public void EndHostConditionalRendering()
        {
            _gd.Api.EndConditionalRender();

            _activeConditionalRender?.ReleaseHostAccess();
            _activeConditionalRender = null;
        }

        public void Dispose()
        {
            for (int i = 0; i < Constants.MaxTransformFeedbackBuffers; i++)
            {
                if (_tfbs[i] != BufferHandle.Null)
                {
                    Buffer.Delete(_gd.Api, _tfbs[i]);
                    _tfbs[i] = BufferHandle.Null;
                }
            }

            _activeConditionalRender?.ReleaseHostAccess();
            _framebuffer?.Dispose();
            _vertexArray?.Dispose();
            _drawTexture.Dispose(_gd.Api);
        }
    }
}
